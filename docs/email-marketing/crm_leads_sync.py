# -*- coding: utf-8 -*-
"""Sync incremental e continuo do conjunto de deflexao (staticData.global.crmLeads) do bot
WAHA Bot - Atendente IA [local] (8nKTlX4Y7Hy1EUEn), Fase 13 (WHAT-02).

Ate 2026-09-20 este arquivo (22 linhas) so lia previews/whatsapp-leads.json e mandava tudo em
lote, sem verificar nada, sem tratar n8n fora do ar e sem rodar de hora em hora. Este e o
rewrite: sync incremental (so o delta), verificado por releitura do sqlite do n8n, com fila
pendente durave quando o n8n esta fora do ar (nunca perde o delta, nunca finge sucesso).

Fontes do conjunto de deflexao (uniao normalizada, sem duplicata):
  1. wa_estado.ler_sent()      -> quem RECEBEU mensagem de prospeccao de verdade (outreach-send.py)
  2. wa_estado.ler_blacklist() -> quem RECUSOU (recusa nunca pode receber resposta de IA)
  3. previews/whatsapp-leads.json (chave 'leads') -> base completa de leads exportada do CRM

Contrato do webhook do bot (n8n-local/workflows/local/waha-bot-atendente.json, no 'Guard',
handler do evento 'crmleads' -- lido ao vivo em 2026-09-20, nao apenas do JSON exportado):
  POST http://localhost:5678/webhook/waha-bot
  {"event":"crmleads","payload":{"chatIds":[...], "reset": true|ausente}}
Sem 'reset', o handler faz MERGE (s.crmLeads[cid]=1 para cada id) -- e o modo incremental normal
usado pelo sync automatico. Com 'reset:true' ele zera o conjunto antes de aplicar -- uso manual,
nunca na task agendada.

O conjunto vive em staticData.global.crmLeads (mapa {chatId: 1}) do workflow 8nKTlX4Y7Hy1EUEn,
lido aqui SOMENTE-LEITURA via sqlite3 mode=ro -- este script NUNCA escreve no sqlite do n8n; a
unica escrita no staticData e feita pelo proprio n8n, via webhook.

Zero I/O em tempo de import: toda chamada externa (arquivo, sqlite, rede, sleep) so acontece
dentro de funcao -- 'import crm_leads_sync' funciona offline, sem CRM, sem WhatsApp.

Uso:
  python crm_leads_sync.py            -> sync incremental real (delta + pendentes -> POST -> verifica)
  python crm_leads_sync.py --status   -> mostra fonte/atual/delta/pendentes, nao escreve nada
  python crm_leads_sync.py --dry      -> calcula tudo e mostra o que POSTaria, sem POST
  python crm_leads_sync.py --reset    -> manda a fonte INTEIRA com reset:true no 1o lote (manual)
  python crm_leads_sync.py --selftest -> autoteste offline (fixtures embutidas), imprime OK n/n
"""
import json, os, re, sys, time, socket, sqlite3, datetime, unicodedata, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import wa_estado  # noqa: E402  (mesmo diretorio -- ver sys.path acima; le-only, ja e offline no import)

WEBHOOK = 'http://localhost:5678/webhook/waha-bot'
N8N_DB = os.path.expanduser('~/.n8n/database.sqlite')
BOT_ID = '8nKTlX4Y7Hy1EUEn'

WHATSAPP_LEADS = os.path.join(HERE, 'previews', 'whatsapp-leads.json')
PENDING = os.path.join(HERE, 'previews', 'crmleads-pending.json')
LOG_PATH = os.path.join(HERE, 'logs', 'crm-leads-sync.log')

BATCH = 800
SLEEP_LOTE = 20      # espera o staticData commitar antes do proximo lote (mesmo valor de restaurar-tudo.py)
SLEEP_VERIFICA = 5   # espera antes de reler o sqlite pra verificar

RE_VALIDO = re.compile(r'^\d{12,13}@c\.us$')


# ---------- log ----------

def log(msg):
    os.makedirs(os.path.dirname(LOG_PATH), exist_ok=True)
    linha = '[%s] %s' % (time.strftime('%Y-%m-%d %H:%M:%S'), msg)
    try:
        print(linha)
    except UnicodeEncodeError:
        print(linha.encode('ascii', 'ignore').decode())
    with open(LOG_PATH, 'a', encoding='utf-8') as f:
        f.write(linha + '\n')


# ---------- funcoes puras (sem I/O, testaveis offline) ----------

def norm(chat_id):
    """So digitos + @c.us -- mesma semantica de outreach-send.py:norm() (copiada, nao
    importada: outreach-send.py faz os.chdir() em tempo de import, efeito colateral que este
    script nao pode ter)."""
    s = unicodedata.normalize('NFKC', str(chat_id or '')).strip().lower()
    s = s.split('@')[0]
    s = ''.join(c for c in s if c.isdigit())
    return s + '@c.us' if s else ''


def _valido(chat_id):
    return bool(RE_VALIDO.match(chat_id or ''))


def _normalizar_uniao(brutos):
    """Lista bruta -> (ids validos normalizados sem duplicata em ordem estavel, contagem de
    descartes). Id que nao casa ^\\d{12,13}@c.us$ apos normalizar e descartado, nao estoura."""
    vistos, out, descartados = set(), [], 0
    for x in brutos:
        n = norm(x)
        if not n or not _valido(n):
            descartados += 1
            continue
        if n not in vistos:
            vistos.add(n)
            out.append(n)
    return out, descartados


def delta(atuais_set, fonte):
    """So o que existe em `fonte` e NAO existe em `atuais_set`, em ordem estavel. Delta vazio
    (fonte inteira ja dentro de atuais_set) -> lista vazia, nenhum POST necessario."""
    return [x for x in fonte if x not in atuais_set]


def _a_enviar(atual_set, pendentes, fonte, reset):
    """Funcao pura de decisao: o que efetivamente sai neste ciclo.
    reset=True   -> a fonte INTEIRA (reset e substituicao completa, nao delta).
    reset=False  -> pendentes fundidos ao delta atual, dedup, e filtrados de novo contra
                    atual_set (cobre pendente antigo que por acaso ja entrou por outro caminho)."""
    if reset:
        return list(fonte)
    d = delta(atual_set, fonte)
    fundidos = list(dict.fromkeys(list(pendentes) + d))
    return delta(atual_set, fundidos)


def _lotes(ids, tamanho=BATCH):
    """Fatia `ids` em lotes de `tamanho`, ordem preservada."""
    return [ids[i:i + tamanho] for i in range(0, len(ids), tamanho)]


def _payload(lote, reset_neste_lote):
    """Monta o corpo do POST. 'reset' so aparece quando reset_neste_lote e True -- nunca em
    lotes seguintes ao primeiro, mesmo com --reset ligado."""
    p = {'chatIds': list(lote)}
    if reset_neste_lote:
        p['reset'] = True
    return {'event': 'crmleads', 'payload': p}


# ---------- leitores/escritores (I/O isolado por funcao) ----------

def fontes():
    """Uniao normalizada de wa_estado.ler_sent() + wa_estado.ler_blacklist() + chat_ids de
    previews/whatsapp-leads.json. Devolve (lista, descartados) -- ver _normalizar_uniao."""
    brutos = list(wa_estado.ler_sent()) + list(wa_estado.ler_blacklist())
    if os.path.exists(WHATSAPP_LEADS):
        d = json.load(open(WHATSAPP_LEADS, encoding='utf-8'))
        leads = d if isinstance(d, list) else d.get('leads', [])
        for l in leads:
            cid = l.get('chat_id') or l.get('chatId')
            if cid:
                brutos.append(cid)
    return _normalizar_uniao(brutos)


def atuais():
    """set() de staticData.global.crmLeads do workflow BOT_ID, lido SOMENTE-LEITURA
    (sqlite3 mode=ro) de ~/.n8n/database.sqlite. Nunca escreve; set() vazio se o arquivo, a
    linha ou a chave nao existirem (nao e erro -- e o estado inicial legitimo)."""
    if not os.path.exists(N8N_DB):
        return set()
    con = None
    try:
        uri = 'file:' + N8N_DB.replace(os.sep, '/') + '?mode=ro'
        con = sqlite3.connect(uri, uri=True)
        cur = con.cursor()
        cur.execute('select staticData from workflow_entity where id=?', (BOT_ID,))
        row = cur.fetchone()
        if not row or not row[0]:
            return set()
        sd = json.loads(row[0])
        crm = (sd.get('global') or {}).get('crmLeads') or {}
        return set(crm.keys())
    except Exception as e:
        log('ERRO ao ler staticData do n8n (%s) -- tratando como conjunto vazio' % type(e).__name__)
        return set()
    finally:
        if con is not None:
            con.close()


def _porta_aberta():
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(1.5)
    try:
        s.connect(('127.0.0.1', 5678))
        return True
    except OSError:
        return False
    finally:
        s.close()


def _pendentes_ler():
    if not os.path.exists(PENDING):
        return []
    try:
        d = json.load(open(PENDING, encoding='utf-8'))
        return list(d.get('chat_ids') or [])
    except Exception as e:
        log('ERRO ao ler %s (%s) -- tratando como fila vazia' % (PENDING, type(e).__name__))
        return []


def _pendentes_gravar(ids):
    os.makedirs(os.path.dirname(PENDING), exist_ok=True)
    dados = {'gerado': datetime.datetime.now().isoformat(timespec='seconds'), 'chat_ids': list(ids)}
    tmp = PENDING + '.tmp'
    with open(tmp, 'w', encoding='utf-8') as f:
        json.dump(dados, f, ensure_ascii=False, indent=1)
        f.flush()
        os.fsync(f.fileno())
    os.replace(tmp, PENDING)


def empurrar(ids, reset=False):
    """POST em lotes de BATCH para o webhook do bot. Falha de rede em qualquer lote: para
    imediatamente (nao estoura), devolve (empurrados_ate_agora, restante_nao_tentado).
    time.sleep(SLEEP_LOTE) entre lotes (nao apos o ultimo)."""
    lotes = _lotes(ids)
    empurrados = []
    for i, lote in enumerate(lotes):
        payload = _payload(lote, reset and i == 0)
        try:
            body = json.dumps(payload).encode('utf-8')
            req = urllib.request.Request(WEBHOOK, data=body,
                                          headers={'Content-Type': 'application/json'})
            urllib.request.urlopen(req, timeout=30).read()
            empurrados.extend(lote)
            log('lote %d/%d empurrado: %d ids (reset=%s)' % (
                i + 1, len(lotes), len(lote), payload['payload'].get('reset', False)))
        except Exception as e:
            log('FALHA no lote %d/%d (%s) -- parando, restante vira pendente' % (
                i + 1, len(lotes), type(e).__name__))
            restante = []
            for l2 in lotes[i:]:
                restante.extend(l2)
            return empurrados, restante
        if i + 1 < len(lotes):
            time.sleep(SLEEP_LOTE)
    return empurrados, []


def verificar(ids):
    """Rele atuais() (com sleep de SLEEP_VERIFICA antes) e devolve os `ids` que NAO estao no
    conjunto ao vivo do bot. Lista vazia se `ids` vazio (nada empurrado, nada a verificar)."""
    if not ids:
        return []
    time.sleep(SLEEP_VERIFICA)
    atual = atuais()
    return [i for i in ids if i not in atual]


# ---------- nucleo testavel (I/O injetada -- main() usa as reais, selftest() usa fakes) ----------

def _executar(argv, fn_fontes, fn_atuais, fn_porta_aberta, fn_empurrar, fn_verificar,
              fn_pend_ler, fn_pend_gravar, fn_log):
    reset = '--reset' in argv
    dry = '--dry' in argv
    status_only = '--status' in argv

    pendentes = fn_pend_ler()
    f, descartados = fn_fontes()
    atual = fn_atuais()
    d = delta(atual, f)

    if status_only:
        print('fonte=%d atual=%d delta=%d pendentes=%d descartados=%d' % (
            len(f), len(atual), len(d), len(pendentes), descartados))
        return 0

    combinados = _a_enviar(atual, pendentes, f, reset)

    if dry:
        print('DRY: fonte=%d atual=%d delta=%d pendentes=%d -> enviaria %d ids (reset=%s)' % (
            len(f), len(atual), len(d), len(pendentes), len(combinados), reset))
        return 0

    if not combinados:
        print('nada a sincronizar')
        fn_log('fonte=%d atual=%d delta=%d empurrados=0 pendentes=0 faltantes=0' % (
            len(f), len(atual), len(d)))
        return 0

    if not fn_porta_aberta():
        fn_pend_gravar(combinados)
        msg = 'N8N FORA DO AR: %d pendentes' % len(combinados)
        print(msg)
        fn_log(msg)
        fn_log('fonte=%d atual=%d delta=%d empurrados=0 pendentes=%d faltantes=0' % (
            len(f), len(atual), len(d), len(combinados)))
        return 0

    empurrados, restante = fn_empurrar(combinados, reset)
    faltantes = fn_verificar(empurrados)
    novo_pendente = list(dict.fromkeys(restante + faltantes))
    fn_pend_gravar(novo_pendente)

    resumo = 'fonte=%d atual=%d delta=%d empurrados=%d pendentes=%d faltantes=%d' % (
        len(f), len(atual), len(d), len(empurrados), len(novo_pendente), len(faltantes))
    fn_log(resumo)
    print(resumo)

    if faltantes:
        print('VERIFICACAO FALHOU: %d de %d nao entraram' % (len(faltantes), len(empurrados)))
        return 3
    return 0


# ---------- autoteste offline (sem rede/sqlite/CRM) ----------

_CHECKS = {'ok': 0, 'total': 0}


def _assert(cond, label):
    _CHECKS['total'] += 1
    if cond:
        _CHECKS['ok'] += 1
        print('OK: %s' % label)
    else:
        print('FAIL: %s' % label)
    return bool(cond)


def selftest():
    ok = True

    ok &= _assert(norm('+55 (27) 99901-6777') == '5527999016777@c.us',
                  'norm() normaliza id torto (espacos/parenteses/traco/DDI) para so-digitos@c.us')
    ok &= _assert(norm('abc') == '', 'norm() devolve vazio para chat_id sem digito')

    ok &= _assert(_valido('5527999016777@c.us') and not _valido('123@c.us') and not _valido(''),
                  '_valido() aceita so 12-13 digitos + @c.us')

    ids, desc = _normalizar_uniao(['5527999016777@c.us', '+55 27 99901-6777', 'abc', '123'])
    ok &= _assert(ids == ['5527999016777@c.us'] and desc == 2,
                  '_normalizar_uniao dedup equivalentes e descarta invalidos (contagem correta)')

    ok &= _assert(delta({'a@c.us'}, ['a@c.us', 'b@c.us']) == ['b@c.us'],
                  'delta() so devolve o que falta no atual')
    ok &= _assert(delta({'a@c.us'}, ['a@c.us']) == [],
                  'delta() vazio quando a fonte inteira ja esta no atual')

    ok &= _assert(_a_enviar(set(), ['p@c.us'], ['a@c.us'], False) == ['p@c.us', 'a@c.us'],
                  '_a_enviar funde pendentes ao delta antes de empurrar (pendente primeiro)')
    ok &= _assert(_a_enviar({'a@c.us'}, ['a@c.us'], ['a@c.us', 'b@c.us'], False) == ['b@c.us'],
                  '_a_enviar filtra pendente que ja entrou por outro caminho')
    ok &= _assert(_a_enviar({'a@c.us'}, [], ['a@c.us', 'b@c.us'], True) == ['a@c.us', 'b@c.us'],
                  '_a_enviar com reset=True manda a fonte inteira, ignorando atual/pendentes')

    lotes = _lotes(['1', '2', '3'], tamanho=2)
    ok &= _assert(lotes == [['1', '2'], ['3']], '_lotes fatia preservando ordem')
    ok &= _assert('reset' not in _payload(['x'], False)['payload'],
                  '_payload sem reset nao inclui a chave reset')
    ok &= _assert(_payload(['x'], True)['payload'].get('reset') is True and
                  'reset' not in _payload(['y'], False)['payload'],
                  "--reset so manda reset:true no lote marcado (nunca nos seguintes)")

    _log_fake = lambda m: None  # noqa: E731

    # cenario: nada a sincronizar (delta vazio + pendentes vazio) -> nao chama empurrar
    chamadas = []
    cod = _executar([], lambda: (['a@c.us'], 0), lambda: {'a@c.us'}, lambda: True,
                     lambda ids, reset: chamadas.append(ids) or (ids, []),
                     lambda ids: [], lambda: [], lambda ids: None, _log_fake)
    ok &= _assert(cod == 0 and chamadas == [],
                  '_executar: delta=0/pendentes=0 sai 0 sem chamar empurrar (nada a sincronizar)')

    # cenario: delta + pendente fundem e sao empurrados com sucesso, sem faltantes
    chamadas = []
    gravados = []
    cod = _executar([], lambda: (['a@c.us'], 0), lambda: set(), lambda: True,
                     lambda ids, reset: chamadas.append(sorted(ids)) or (ids, []),
                     lambda ids: [], lambda: ['p@c.us'],
                     lambda ids: gravados.append(list(ids)), _log_fake)
    ok &= _assert(cod == 0 and chamadas == [['a@c.us', 'p@c.us']] and gravados == [[]],
                  '_executar: delta com pendente funde antes de empurrar e limpa a fila ao confirmar')

    # cenario: n8n fora do ar -- grava pendente, sai 0, nunca chama empurrar
    chamadas = []
    gravados = []
    cod = _executar([], lambda: (['a@c.us'], 0), lambda: set(), lambda: False,
                     lambda ids, reset: chamadas.append(ids) or (ids, []),
                     lambda ids: [], lambda: [],
                     lambda ids: gravados.append(list(ids)), _log_fake)
    ok &= _assert(cod == 0 and chamadas == [] and gravados == [['a@c.us']],
                  '_executar: n8n fora do ar grava o delta em pendente e sai 0 sem tentar POST')

    # cenario: verificacao acusa faltante -> sai com codigo != 0 e faltante volta pra pendente
    gravados = []
    cod = _executar([], lambda: (['a@c.us'], 0), lambda: set(), lambda: True,
                     lambda ids, reset: (ids, []), lambda ids: ['a@c.us'],
                     lambda: [], lambda ids: gravados.append(list(ids)), _log_fake)
    ok &= _assert(cod == 3 and gravados == [['a@c.us']],
                  '_executar: verificacao com faltante sai com codigo != 0 e mantem o faltante pendente')

    print('OK %d/%d' % (_CHECKS['ok'], _CHECKS['total']))
    return ok


# ---------- CLI ----------

def main():
    argv = sys.argv[1:]
    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)
    return _executar(argv, fontes, atuais, _porta_aberta, empurrar, verificar,
                      _pendentes_ler, _pendentes_gravar, log)


if __name__ == '__main__':
    sys.exit(main())
