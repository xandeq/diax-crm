# -*- coding: utf-8 -*-
"""Leitor unico do estado operacional real do canal WhatsApp (Fase 13, WHAT-01).

Junta tres fontes independentes numa verdade so, sem NUNCA escrever em nenhuma delas (so
grava o proprio arquivo de snapshot, previews/whatsapp-estado.json):

  1. D:\\claude-code\\n8n-local\\logs\\outreach-audit.log + outreach-sent.json +
     outreach-blacklist.json + outreach-watch-estado.json — o que o caminho Python
     (outreach-send.py / outreach-watch.py, tasks do Windows outreach-wa/outreach-watch)
     REALMENTE enviou e recebeu hoje. Este e o emissor ao vivo (ver 13-CONTEXT.md).
  2. ~/.n8n/database.sqlite (workflow_entity/execution_entity) — o estado AO VIVO do n8n,
     nao o JSON exportado em n8n-local/workflows/local/ (que fica defasado: o exportado de
     waOutreach1a1AQ diz MAX_PRIO=3/CAP=1, o banco ao vivo mediu MAX_PRIO=4/CAP=2 em
     2026-09-20). Aberto SOMENTE-LEITURA (`mode=ro`).
  3. customers / task_items (SQL Server prod, via skills/financas/scripts/db.py) — o que o
     CRM sabe. outreach-send.py NAO chama POST /api/v1/integrations/whatsapp-event, entao
     o CRM subestima quem foi contatado — o campo `divergencia` do snapshot() mede isso a
     cada chamada, nunca com um numero fixo.

Zero rede/DB em tempo de import: toda chamada externa (arquivo, sqlite, SQL Server, socket,
schtasks) so acontece dentro de funcao. `import wa_estado` funciona offline.

Desvios do formato assumido no plano, ajustados ao real e documentados aqui (em vez de forcar
o formato quando o real divergiu):
  - outreach-watch-estado.json e uma LISTA de strings "<chat_id>|<timestamp_unix>" (nao um
    dict) — o timestamp vem do campo `timestamp` da mensagem no WAHA (epoch em segundos).
  - As linhas "WATCH ALERTA enviado ao dono sobre N lead(s)" do audit NAO carregam chat_id
    (so uma contagem agregada por varredura) — por isso ler_respostas() usa APENAS
    outreach-watch-estado.json como fonte de {ts, chat_id} por lead; o total de linhas
    ALERTA vira o diagnostico 'alertas_total' do snapshot(), fora do contrato de
    ler_respostas().

Uso:
  python wa_estado.py                 -> --status (default)
  python wa_estado.py --status        -> resumo legivel do estado real, so leitura
  python wa_estado.py --json          -> grava previews/whatsapp-estado.json
  python wa_estado.py --selftest      -> autoteste offline, sem rede/DB/sqlite, imprime OK n/n
"""
import os, re, sys, json, socket, sqlite3, datetime, subprocess

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
LOGS = r'D:\claude-code\n8n-local\logs'
N8N_DB = os.path.expanduser('~/.n8n/database.sqlite')
ES_DDD = {'27', '28'}  # mesmo conjunto de export_whatsapp_leads.py

AUDIT = os.path.join(LOGS, 'outreach-audit.log')
SENT = os.path.join(LOGS, 'outreach-sent.json')
BLACKLIST = os.path.join(LOGS, 'outreach-blacklist.json')
WATCH_ESTADO = os.path.join(LOGS, 'outreach-watch-estado.json')

OUT = DIR + 'previews/whatsapp-estado.json'

RE_ENVIADO = re.compile(r'^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) ENVIADO (\S+@c\.us) \| (.*)$')
RE_ALERTA = re.compile(r'^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} WATCH ALERTA enviado ao dono sobre (\d+) lead')
RE_ENVIADOS_HOJE = re.compile(r'enviados hoje: (\d+)/(\d+)')
RE_CAP = re.compile(r'MAX_PRIO\s*=\s*(\d+)\s*,\s*CAP\s*=\s*(\d+)')

WAOUTREACH_ID = 'waOutreach1a1AQ'
BOT_ID = '8nKTlX4Y7Hy1EUEn'


# ---------- helpers puros (sem I/O) ----------

def _so_digitos(chat_id):
    return ''.join(c for c in str(chat_id or '') if c.isdigit())


def seg(chat_id):
    """'wa_es' | 'wa_br' | 'desconhecido' — mesma regra ES_DDD de export_whatsapp_leads.py."""
    d = _so_digitos(chat_id)
    if d.startswith('55') and len(d) >= 4:
        return 'wa_es' if d[2:4] in ES_DDD else 'wa_br'
    return 'desconhecido'


def _norm(chat_id):
    """So digitos + @c.us — mesmo numero nunca vira duas chaves diferentes (copia de
    outreach-send.py:norm(), duplicada aqui em vez de importada porque outreach-send.py faz
    os.chdir(AQUI) em tempo de import, o que quebraria 'zero efeito colateral no import')."""
    d = _so_digitos(chat_id)
    return d + '@c.us' if d else ''


def _parse_envios_lines(lines):
    """Funcao pura: linhas cruas do audit -> so as validas ' ENVIADO ', nunca 'WATCH'."""
    out = []
    for l in lines:
        m = RE_ENVIADO.match(l.rstrip('\n'))
        if not m:
            continue
        ts = datetime.datetime.strptime(m.group(1), '%Y-%m-%d %H:%M:%S')
        chat_id = m.group(2)
        out.append({'ts': ts, 'chat_id': chat_id, 'nome': m.group(3), 'segmento': seg(chat_id)})
    return out


def _parse_sent(raw):
    """Funcao pura: lista crua -> chat_ids normalizados e deduplicados, ordem preservada."""
    vistos, out = set(), []
    for x in (raw or []):
        n = _norm(x)
        if n and n not in vistos:
            vistos.add(n)
            out.append(n)
    return out


def _parse_blacklist(raw):
    return _parse_sent((raw or {}).get('recusaram', []))


def _parse_respostas(raw):
    """Funcao pura: lista crua de outreach-watch-estado.json ('chat_id|timestamp_unix') ->
    [{'ts','chat_id'}] ordenado por ts. Linha sem '|' ou com timestamp invalido e ignorada."""
    out = []
    for item in (raw or []):
        partes = str(item).split('|', 1)
        if len(partes) < 2:
            continue
        try:
            ts = datetime.datetime.fromtimestamp(int(partes[1]))
        except (ValueError, OSError, OverflowError):
            continue
        out.append({'ts': ts, 'chat_id': partes[0]})
    out.sort(key=lambda r: r['ts'])
    return out


def _extract_cap_maxprio(js_code):
    """Funcao pura: extrai (max_prio, cap) do jsCode do no 'Filtrar P1-P3 + cap' ao vivo.
    (None, None) se o padrao nao casar."""
    m = RE_CAP.search(js_code or '')
    return (int(m.group(1)), int(m.group(2))) if m else (None, None)


# ---------- leitores (I/O isolado por funcao) ----------

def ler_envios(dias=None):
    """[{'ts': datetime, 'chat_id': str, 'nome': str, 'segmento': str}] — linhas ' ENVIADO '
    do audit, ordem cronologica. dias=N filtra pelo timestamp LOCAL do log (BRT, o log e
    escrito com time.strftime local — nunca converter para UTC)."""
    if not os.path.exists(AUDIT):
        return []
    with open(AUDIT, encoding='utf-8') as f:
        entradas = _parse_envios_lines(f.readlines())
    if dias:
        corte = datetime.datetime.now() - datetime.timedelta(days=dias)
        entradas = [e for e in entradas if e['ts'] >= corte]
    return entradas


def ler_sent():
    """chat_ids normalizados e deduplicados de logs/outreach-sent.json (all-time, nao
    janelado — e a base pra medir a divergencia logs-vs-CRM)."""
    if not os.path.exists(SENT):
        return []
    return _parse_sent(json.load(open(SENT, encoding='utf-8')))


def ler_blacklist():
    """chat_ids normalizados de logs/outreach-blacklist.json['recusaram']. Lista vazia e
    mais provavel ser bug de leitura que realidade (mesma regra de outreach-send.py:escolhe())
    — o snapshot() marca 'blacklist_suspeita': True nesse caso, sem levantar excecao aqui."""
    if not os.path.exists(BLACKLIST):
        return []
    return _parse_blacklist(json.load(open(BLACKLIST, encoding='utf-8')))


def ler_respostas(dias=None):
    """[{'ts': datetime, 'chat_id': str}] — de logs/outreach-watch-estado.json. Ver docstring
    do modulo: as linhas 'WATCH ALERTA' do audit nao tem chat_id, entao nao entram aqui."""
    if not os.path.exists(WATCH_ESTADO):
        return []
    entradas = _parse_respostas(json.load(open(WATCH_ESTADO, encoding='utf-8')))
    if dias:
        corte = datetime.datetime.now() - datetime.timedelta(days=dias)
        entradas = [e for e in entradas if e['ts'] >= corte]
    return entradas


def _alertas_total():
    """Diagnostico agregado (nao faz parte do contrato de ler_respostas): quantas varreduras
    do outreach-watch.py dispararam alerta ao dono, all-time."""
    if not os.path.exists(AUDIT):
        return 0
    n = 0
    with open(AUDIT, encoding='utf-8') as f:
        for l in f:
            if RE_ALERTA.match(l.rstrip('\n')):
                n += 1
    return n


def _outreach_hoje():
    """Ultima linha 'enviados hoje: N/CAP' que o proprio outreach-send.py ja loga a cada
    execucao — leitura pura do log, nao reimplementa a logica de CAP_DIA/janela."""
    if not os.path.exists(AUDIT):
        return {'enviados': None, 'cap': None}
    ultimo = None
    with open(AUDIT, encoding='utf-8') as f:
        for l in f:
            m = RE_ENVIADOS_HOJE.search(l)
            if m:
                ultimo = m
    if not ultimo:
        return {'enviados': None, 'cap': None}
    return {'enviados': int(ultimo.group(1)), 'cap': int(ultimo.group(2))}


def _porta_aberta(host, port, timeout=1.5):
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(timeout)
    try:
        s.connect((host, port))
        return True
    except OSError:
        return False
    finally:
        s.close()


def _task_status(nome):
    try:
        r = subprocess.run(['schtasks', '/query', '/tn', nome, '/fo', 'LIST'],
                            capture_output=True, text=True, timeout=10)
        if r.returncode != 0:
            return 'inexistente_ou_erro'
        m = re.search(r'Status:\s*(\S+)', r.stdout)
        return m.group(1) if m else 'desconhecida'
    except Exception as e:
        return 'erro(%s)' % type(e).__name__


def ler_n8n():
    """Estado AO VIVO do n8n — porta 5678, sqlite SOMENTE-LEITURA, task do Windows.
    {'porta_5678_aberta': bool, 'task_n8n_local': str, 'erro': str|None,
     'workflows': {'waOutreach1a1AQ': {'active','ultima_execucao','cap','max_prio'},
                   '8nKTlX4Y7Hy1EUEn': {'active','ultima_execucao','crm_leads'}}}"""
    resultado = {
        'porta_5678_aberta': _porta_aberta('127.0.0.1', 5678),
        'task_n8n_local': _task_status('n8n-local'),
        'workflows': {},
        'erro': None,
    }
    con = None
    try:
        uri = 'file:' + N8N_DB.replace(os.sep, '/') + '?mode=ro'
        con = sqlite3.connect(uri, uri=True)
        cur = con.cursor()

        cur.execute('select active, nodes from workflow_entity where id=?', (WAOUTREACH_ID,))
        row = cur.fetchone()
        active_out = max_prio = cap = ultima = None
        if row:
            active_out, nodes_raw = row
            try:
                for n in json.loads(nodes_raw):
                    if 'Filtrar' in (n.get('name') or ''):
                        max_prio, cap = _extract_cap_maxprio(
                            (n.get('parameters') or {}).get('jsCode', ''))
            except Exception:
                pass
            cur.execute('select max(startedAt) from execution_entity where workflowId=?',
                        (WAOUTREACH_ID,))
            r2 = cur.fetchone()
            ultima = r2[0] if r2 else None
        resultado['workflows'][WAOUTREACH_ID] = {
            'active': active_out, 'ultima_execucao': ultima, 'cap': cap, 'max_prio': max_prio,
        }

        cur.execute('select active, staticData from workflow_entity where id=?', (BOT_ID,))
        row = cur.fetchone()
        active_bot = crm_leads = ultima_bot = None
        if row:
            active_bot, sd_raw = row
            try:
                sd = json.loads(sd_raw) if sd_raw else {}
                crm_leads = len((sd.get('global') or {}).get('crmLeads') or {})
            except Exception:
                crm_leads = 0
            cur.execute('select max(startedAt) from execution_entity where workflowId=?',
                        (BOT_ID,))
            r2 = cur.fetchone()
            ultima_bot = r2[0] if r2 else None
        resultado['workflows'][BOT_ID] = {
            'active': active_bot, 'ultima_execucao': ultima_bot, 'crm_leads': crm_leads,
        }
    except Exception as e:
        resultado['erro'] = type(e).__name__
    finally:
        if con is not None:
            con.close()
    return resultado


def ler_crm(dias=7):
    """{'enviados_total','enviados_periodo','respostas_task_items','respostas_periodo',
    'optout'} — via crmdb.connect() (mesmo padrao de export_whatsapp_leads.py). Se a conexao
    falhar, devolve {'erro': tipo} em vez de estourar; o snapshot ainda vale pelos logs."""
    sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
    try:
        import db as crmdb
        cx = crmdb.connect()
        cur = cx.cursor()
        cur.execute('SELECT COUNT(*) FROM customers WHERE whats_app_sent_count>0')
        total = cur.fetchone()[0]
        cur.execute(
            'SELECT COUNT(*) FROM customers WHERE whats_app_sent_count>0 '
            'AND last_whats_app_sent_at >= DATEADD(day,-?,SYSUTCDATETIME())', dias)
        periodo = cur.fetchone()[0]
        cur.execute("SELECT COUNT(*) FROM task_items WHERE title LIKE N'%WhatsApp respondeu%'")
        resp_total = cur.fetchone()[0]
        cur.execute(
            "SELECT COUNT(*) FROM task_items WHERE title LIKE N'%WhatsApp respondeu%' "
            'AND created_at >= DATEADD(day,-?,SYSUTCDATETIME())', dias)
        resp_periodo = cur.fetchone()[0]
        cur.execute('SELECT COUNT(*) FROM customers WHERE whats_app_opt_out=1')
        optout = cur.fetchone()[0]
        cx.close()
        return {
            'enviados_total': total, 'enviados_periodo': periodo,
            'respostas_task_items': resp_total, 'respostas_periodo': resp_periodo,
            'optout': optout,
        }
    except Exception as e:
        return {'erro': type(e).__name__}


def _por_segmento(lista, campo='chat_id'):
    c = {'wa_es': 0, 'wa_br': 0, 'desconhecido': 0}
    for item in lista:
        c[seg(item[campo])] += 1
    return c


def snapshot():
    """Junta tudo + grava previews/whatsapp-estado.json. Unico ponto de escrita deste
    arquivo — nenhuma fonte operacional (log, sqlite do n8n, CRM) e alterada."""
    dias = 7
    envios = ler_envios(dias=dias)
    respostas = ler_respostas(dias=dias)
    blacklist = ler_blacklist()
    sent_total = ler_sent()
    n8n = ler_n8n()
    crm = ler_crm(dias=dias)

    enviados_logs = len(sent_total)
    enviados_crm = crm.get('enviados_total') if isinstance(crm, dict) and 'erro' not in crm else None
    delta = (enviados_logs - enviados_crm) if enviados_crm is not None else None

    dados = {
        'gerado': datetime.datetime.now().isoformat(timespec='seconds'),
        'janela_dias': dias,
        'envios': envios,
        'envios_por_segmento': _por_segmento(envios),
        'respostas': respostas,
        'respostas_por_segmento': _por_segmento(respostas),
        'blacklist': blacklist,
        'blacklist_por_segmento': _por_segmento([{'chat_id': c} for c in blacklist]),
        'blacklist_suspeita': len(blacklist) == 0,
        'enviados_sent_json_total': enviados_logs,
        'alertas_total': _alertas_total(),
        'outreach_hoje': _outreach_hoje(),
        'n8n': n8n,
        'crm': crm,
        'divergencia': {
            'enviados_logs': enviados_logs,
            'enviados_crm': enviados_crm,
            'delta': delta,
            'causa': 'outreach-send.py nao chama POST /api/v1/integrations/whatsapp-event',
        },
    }

    def _default(o):
        if isinstance(o, datetime.datetime):
            return o.isoformat(timespec='seconds')
        raise TypeError(str(o))

    os.makedirs(DIR + 'previews', exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(dados, f, ensure_ascii=False, indent=1, default=_default)
    return dados


# ---------- CLI ----------

def _fmt(x):
    return x.strftime('%d/%m %H:%M:%S') if isinstance(x, datetime.datetime) else str(x)


def status():
    d = snapshot()
    n8n = d['n8n']
    wa = n8n['workflows'].get(WAOUTREACH_ID, {})
    bot = n8n['workflows'].get(BOT_ID, {})
    oh = d['outreach_hoje']
    print('=== Estado real do canal WhatsApp (%s) ===' % d['gerado'])
    print('Caminho Python (outreach-send.py, task outreach-wa) — emissor ao vivo hoje:')
    print('  enviados hoje: %s/%s' % (oh['enviados'], oh['cap']))
    if d['envios']:
        ult = d['envios'][-1]
        print('  ultimo ENVIADO: %s %s | %s' % (_fmt(ult['ts']), ult['chat_id'], ult['nome']))
    else:
        print('  nenhum ENVIADO nos ultimos %d dias' % d['janela_dias'])
    print('  outreach-sent.json (all-time): %d chat_ids' % d['enviados_sent_json_total'])
    print('n8n: %s | task n8n-local: %s' % (
        'porta 5678 ABERTA' if n8n['porta_5678_aberta'] else 'porta 5678 FECHADA',
        n8n['task_n8n_local']))
    print('  waOutreach1a1AQ: active=%s cap=%s max_prio=%s ultima_execucao=%s' % (
        wa.get('active'), wa.get('cap'), wa.get('max_prio'), wa.get('ultima_execucao')))
    print('  8nKTlX4Y7Hy1EUEn (bot atendente): active=%s crm_leads=%s ultima_execucao=%s' % (
        bot.get('active'), bot.get('crm_leads'), bot.get('ultima_execucao')))
    print('Blacklist: %d numeros%s' % (
        len(d['blacklist']), ' (SUSPEITA: vazia)' if d['blacklist_suspeita'] else ''))
    print('Respostas (%dd): %d %s' % (
        d['janela_dias'], len(d['respostas']), d['respostas_por_segmento']))
    print('CRM: %s' % d['crm'])
    print('Divergencia logs-vs-CRM: %s' % d['divergencia'])
    return 0


# ---------- autoteste offline (sem rede/DB/sqlite) ----------

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
    ok &= _assert(seg('5527999016777@c.us') == 'wa_es', "seg() classifica DDD 27 como wa_es")
    ok &= _assert(seg('5511996257748@c.us') == 'wa_br', "seg() classifica DDD 11 como wa_br")
    ok &= _assert(seg('abc') == 'desconhecido', "seg() devolve desconhecido p/ chat_id invalido")

    linhas = [
        '2026-09-20 17:00:13 ENVIADO 5527999016777@c.us | Imoveis A Venda Em Guarapari/ES\n',
        '2026-09-20 17:02:44 WATCH varredura: 80 de 184 contatados, 36 aguardando resposta, 0 novos\n',
    ]
    parsed = _parse_envios_lines(linhas)
    ok &= _assert(len(parsed) == 1 and parsed[0]['chat_id'] == '5527999016777@c.us',
                  "_parse_envios_lines pega so a linha ENVIADO, ignora WATCH")

    sent = _parse_sent(['5511996257748@c.us', '+55 11 99625-7748', '5511996257748@c.us'])
    ok &= _assert(sent == ['5511996257748@c.us'],
                  "_parse_sent normaliza e deduplica chat_ids equivalentes")

    bl = _parse_blacklist({'recusaram': ['5527936187331@c.us']})
    ok &= _assert(bl == ['5527936187331@c.us'], "_parse_blacklist le a chave 'recusaram'")

    resp = _parse_respostas(['5519988441001@c.us|1789045981', 'sem-pipe', '5519988441001@c.us|nao-e-numero'])
    ok &= _assert(len(resp) == 1 and resp[0]['chat_id'] == '5519988441001@c.us',
                  "_parse_respostas corta chat_id|timestamp no 1o '|' e ignora linhas invalidas")

    mp, cap = _extract_cap_maxprio('const MAX_PRIO = 4, CAP = 2;')
    ok &= _assert((mp, cap) == (4, 2), "_extract_cap_maxprio le MAX_PRIO/CAP do jsCode ao vivo")

    ok &= _assert(_extract_cap_maxprio('sem padrao aqui') == (None, None),
                  "_extract_cap_maxprio devolve (None,None) quando o jsCode nao casa")

    print('OK %d/%d' % (_CHECKS['ok'], _CHECKS['total']))
    return ok


def main():
    argv = sys.argv[1:]
    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)
    if '--json' in argv:
        d = snapshot()
        print('snapshot gravado em %s' % OUT)
        print('divergencia: %s' % d['divergencia'])
        return 0
    return status()


if __name__ == '__main__':
    sys.exit(main())
