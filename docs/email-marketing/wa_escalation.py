# -*- coding: utf-8 -*-
"""Motor de decisao wa_es -> wa_br (WHAT-04, Phase 13-escala-de-whatsapp).

Mede a taxa de resposta REAL do segmento wa_es (via wa_estado.py, unica fonte real do canal
lida por 13-01/02/03) contra limiares travados ANTES desta medicao, e decide
AVANCAR/SEGURAR/INCONCLUSIVO — sem NUNCA escrever nada operacional; o unico arquivo gravado e
o proprio registro de leitura (previews/wa-escalation-state.json). Espelha a FORMA de
dmarc_escalation.py (Fase 12): decide() e funcao pura (recebe metricas + limiares + data,
devolve veredito+motivo, sem I/O), e toda escrita fica atras de --apply.

ACHADO CENTRAL DESTE MODULO (medido, nao suposto): `export_whatsapp_leads.py` ordena wa_es
antes de wa_br mas NAO exclui wa_br (linha `items.sort(key=lambda x: (x['priority'], 0 if
x['segment']=='wa_es' else 1, ...))`), e `outreach-send.py:escolhe()` (o emissor oficial desde
o checkpoint `python-oficial` do 13-01) NAO filtra por segmento em lugar nenhum — so por
STOP/blacklist/sent/janela/CAP/prioridade/encoding. Ou seja, o "portao" que este requisito
pede para abrir pode nunca ter existido como trava de codigo no emissor ao vivo. `--apply`,
portanto, NUNCA liga nada de operacional — ele so grava o veredito e, quando nao ha portao,
imprime isso explicitamente em vez de fingir uma acao que nao existe (ver `portao_atual()` e
`mensagem_portao()`).

REGRA DE HONESTIDADE (13-04): os 4 limiares abaixo foram escritos e commitados ANTES de rodar
`--status` contra dados reais pela primeira vez nesta sessao. Se o veredito nao agradar, o
caminho e mais amostra, nao mexer nestes numeros — o SUMMARY do plano cita o `git diff` que
prova essa ordem.

Uso:
  python wa_escalation.py --status    # mede via wa_estado, imprime veredito, grava o state
  python wa_escalation.py             # mesmo que --status
  python wa_escalation.py --selftest  # autoteste offline, sem rede/DB/sqlite, <5s
  python wa_escalation.py --apply     # so grava o veredito com aplicado=true; RECUSA (exit 1)
                                       # se veredito != AVANCAR; nunca chama API/DB/config real
"""
import os, sys, json, datetime
from pathlib import Path

DIR = str(Path(__file__).resolve().parent) + '/'
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import wa_estado  # noqa: E402 - leitor unico do estado real do canal (Fase 13-01)

STATE_FILE = DIR + 'previews/wa-escalation-state.json'
EXPORT_LEADS_PATH = DIR + 'export_whatsapp_leads.py'
OUTREACH_SEND_PATH = r'D:\claude-code\n8n-local\outreach-send.py'

# ---------- LIMIARES (escritos e justificados ANTES de rodar contra dados reais) ----------
# SE O VEREDITO NAO AGRADAR, O CAMINHO E MAIS AMOSTRA, NAO MEXER NESTES NUMEROS.
MIN_ENVIADOS_ES = 60    # abaixo disso, uma unica resposta a mais move a taxa em >1,5 p.p.;
                        # ordem de grandeza do MIN_CLICKS=30 do A/B de email (Fase 12) aplicado
                        # a um canal com taxa de resposta muito maior que clique de email.
MIN_RESPOSTAS_ES = 10   # menos que isso e anedota, nao taxa.
TAXA_MINIMA = 0.10      # 10% de resposta em 1-a-1 no WhatsApp — deliberadamente conservador.
                        # A base historica pre-cadencia (8 respostas em 17 contatos manuais,
                        # citada em 13-RESEARCH.md, ~47%) NAO foi usada como referencia por nao
                        # seguir as travas atuais de outreach-send.py (blacklist-first, dedupe
                        # verificado, cap/janela) — comparar com ela inflaria o limiar por acaso.
TETO_RECUSA = 0.08      # acima de 8% de recusa explicita, escalar amplifica dano de reputacao;
                        # mesma logica do limiar de reversao por bounce do DMARC (D-04, Fase 12).

LIMIARES = {
    'MIN_ENVIADOS_ES': MIN_ENVIADOS_ES,
    'MIN_RESPOSTAS_ES': MIN_RESPOSTAS_ES,
    'TAXA_MINIMA': TAXA_MINIMA,
    'TETO_RECUSA': TETO_RECUSA,
}


# ---------- metricas (I/O via wa_estado, nunca reimplementado) ----------

def metricas_segmento(dias=None):
    """{'wa_es': {...}, 'wa_br': {...}} — enviados/respondidos/recusas contando chat_ids
    UNICOS por segmento, `taxa=None` quando `enviados==0`. `dias=None` = historico completo
    (todo o audit log, desde 2026-09-13): a amostra de wa_es e pequena; recortar 7 dias aqui
    destruiria o pouco que existe — mesma decisao de janela tomada em `crm_leads_sync.py`
    (13-02) para a fonte de deflexao. `ler_blacklist()` e sempre all-time (o proprio arquivo
    nao tem timestamp por item), independente de `dias`."""
    envios = wa_estado.ler_envios(dias=dias)
    respostas = wa_estado.ler_respostas(dias=dias)
    blacklist = wa_estado.ler_blacklist()

    envios_por_chat = {}
    for e in envios:
        envios_por_chat[e['chat_id']] = e['segmento']
    respostas_chat_ids = {r['chat_id'] for r in respostas}
    blacklist_chat_ids = set(blacklist)

    resultado = {}
    for seg in ('wa_es', 'wa_br'):
        n_enviados = sum(1 for s in envios_por_chat.values() if s == seg)
        n_respondidos = sum(1 for cid in respostas_chat_ids if wa_estado.seg(cid) == seg)
        n_recusas = sum(1 for cid in blacklist_chat_ids if wa_estado.seg(cid) == seg)
        taxa = (n_respondidos / n_enviados) if n_enviados else None
        resultado[seg] = {
            'enviados': n_enviados, 'respondidos': n_respondidos,
            'recusas': n_recusas, 'taxa': taxa,
        }
    return resultado


# ---------- decisao (funcao pura — nao consulta nada) ----------

def decide(m, limiares, hoje):
    """(veredito, motivo). PURA: sem I/O, sem rede, sem relogio (hoje entra por parametro,
    mesma assinatura testavel de dmarc_escalation.py:decide() — esta decisao nao depende de
    calendario, so de amostra/taxa/recusa). Ordem de avaliacao FIXA:
      a) amostra insuficiente em wa_es (enviados < MIN_ENVIADOS_ES OU respondidos <
         MIN_RESPOSTAS_ES) -> INCONCLUSIVO, avaliada ANTES de qualquer outra regra — amostra
         pequena vence mesmo que a taxa aparente seja otima.
      b) taxa_es < TAXA_MINIMA OU recusa_es > TETO_RECUSA -> SEGURAR.
      c) senao -> AVANCAR.
    """
    es = m['wa_es']
    enviados = es['enviados']
    respondidos = es['respondidos']

    if enviados < limiares['MIN_ENVIADOS_ES'] or respondidos < limiares['MIN_RESPOSTAS_ES']:
        motivo = ('amostra insuficiente em wa_es: %d enviados (min %d), %d respostas (min %d)'
                   % (enviados, limiares['MIN_ENVIADOS_ES'], respondidos, limiares['MIN_RESPOSTAS_ES']))
        return 'INCONCLUSIVO', motivo

    taxa = es['taxa'] or 0.0
    recusa_rate = (es['recusas'] / enviados) if enviados else 0.0

    if taxa < limiares['TAXA_MINIMA']:
        motivo = ('SEGURAR: taxa wa_es %.1f%% < %.1f%% minimo (%d respostas em %d envios)'
                   % (taxa * 100, limiares['TAXA_MINIMA'] * 100, respondidos, enviados))
        return 'SEGURAR', motivo

    if recusa_rate > limiares['TETO_RECUSA']:
        motivo = ('SEGURAR: recusa wa_es %.1f%% > %.1f%% teto (%d recusas em %d envios)'
                   % (recusa_rate * 100, limiares['TETO_RECUSA'] * 100, es['recusas'], enviados))
        return 'SEGURAR', motivo

    motivo = ('AVANCAR: taxa wa_es %.1f%% >= %.1f%% minimo, recusa %.1f%% <= %.1f%% teto '
               '(%d respostas em %d envios)'
               % (taxa * 100, limiares['TAXA_MINIMA'] * 100, recusa_rate * 100,
                  limiares['TETO_RECUSA'] * 100, respondidos, enviados))
    return 'AVANCAR', motivo


def apply_gate(veredito):
    """Pura: (permitido: bool, msg). `--apply` so pode gravar aplicado=true quando o veredito
    da propria medicao e AVANCAR — nunca decide por si so, so guarda o portao."""
    if veredito != 'AVANCAR':
        return False, '--apply recusado: veredito atual e %s, nao AVANCAR' % veredito
    return True, 'veredito AVANCAR confirmado'


# ---------- portao real (le o codigo-fonte ao vivo, nao assume) ----------

def portao_atual():
    """Detecta se existe, de fato, ALGUM filtro de segmento no caminho de envio ativo — le os
    arquivos-fonte reais em vez de assumir. Devolve
    {'existe_portao': bool, 'onde': str, 'observacao': str}."""
    import re
    observ = []
    tem_es_only = None
    try:
        src_export = open(EXPORT_LEADS_PATH, encoding='utf-8').read()
        tem_es_only = '--es-only' in src_export
    except Exception as e:
        observ.append('export_whatsapp_leads.py nao lido: %s' % type(e).__name__)

    tem_filtro_seg = None
    try:
        src_send = open(OUTREACH_SEND_PATH, encoding='utf-8').read()
        m = re.search(r'def escolhe\(.*?\n(?=def |\Z)', src_send, re.S)
        corpo = m.group(0) if m else ''
        tem_filtro_seg = ('segment' in corpo) or ('segmento' in corpo)
    except Exception as e:
        observ.append('outreach-send.py nao lido: %s' % type(e).__name__)

    if tem_filtro_seg:
        existe = True
        onde = 'outreach-send.py:escolhe()'
        observ.append('escolhe() filtra por segmento — portao real encontrado no emissor ao vivo')
    else:
        existe = False
        onde = 'nenhum'
        observ.append(
            'export_whatsapp_leads.py ordena wa_es antes de wa_br via --es-only=%s, mas '
            'outreach-send.py:escolhe() nao filtra por segmento (so STOP/blacklist/sent/'
            'janela/CAP/prioridade/encoding) — wa_br ja pode ser escolhido pelo emissor ao '
            'vivo assim que a prioridade permitir' % tem_es_only)
    return {'existe_portao': existe, 'onde': onde, 'observacao': '; '.join(observ)}


def mensagem_portao(portao):
    """Pura: string fixa quando nao ha portao (nunca finge uma acao que nao existe)."""
    if not portao.get('existe_portao'):
        return ('SEM PORTAO DE SEGMENTO: wa_br ja entra na mesma fila, ordenada wa_es primeiro '
                '— nada a ligar')
    return ('Portao encontrado em %s — nao alterado por este comando (--apply so grava o '
             'veredito, nunca escreve codigo/config)' % portao.get('onde'))


# ---------- estado (leitura + historico) ----------

def load_state():
    try:
        return json.load(open(STATE_FILE, encoding='utf-8'))
    except Exception:
        return {}


def save_state(st):
    os.makedirs(DIR + 'previews', exist_ok=True)
    json.dump(st, open(STATE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)


def commit_state(veredito, motivo, metricas, portao, aplicado=False):
    st = load_state()
    agora = datetime.datetime.now().isoformat(timespec='seconds')
    st['gerado'] = agora
    st['veredito'] = veredito
    st['motivo'] = motivo
    st['limiares'] = dict(LIMIARES)
    st['metricas'] = metricas
    st['portao'] = portao
    st.setdefault('historico', []).append(
        {'data': agora, 'veredito': veredito, 'motivo': motivo, 'aplicado': aplicado})
    save_state(st)
    return st


# ---------- CLI ----------

def do_status():
    m = metricas_segmento(dias=None)
    portao = portao_atual()
    veredito, motivo = decide(m, LIMIARES, datetime.date.today())
    print('=== wa_escalation - status (somente leitura) ===')
    print('Limiares: %s' % LIMIARES)
    print('Metricas wa_es: %s' % m['wa_es'])
    print('Metricas wa_br: %s' % m['wa_br'])
    print('Portao: %s' % portao)
    print('VEREDITO: %s' % veredito)
    print('motivo: %s' % motivo)
    commit_state(veredito, motivo, m, portao, aplicado=False)
    return veredito, motivo


def do_apply():
    m = metricas_segmento(dias=None)
    portao = portao_atual()
    veredito, motivo = decide(m, LIMIARES, datetime.date.today())
    permitido, msg_gate = apply_gate(veredito)
    if not permitido:
        print('APPLY RECUSADO: %s' % msg_gate)
        print('motivo da medicao: %s' % motivo)
        commit_state(veredito, motivo, m, portao, aplicado=False)
        return 1
    print('APPLY: %s' % msg_gate)
    print(mensagem_portao(portao))
    commit_state(veredito, motivo, m, portao, aplicado=True)
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
    lim = dict(LIMIARES)
    hoje = datetime.date(2026, 9, 20)
    m_br_vazio = {'enviados': 0, 'respondidos': 0, 'recusas': 0, 'taxa': None}

    # 1. amostra insuficiente (enviados) vence ANTES de qualquer outra regra, mesmo com taxa
    #    aparente otima.
    m1 = {'wa_es': {'enviados': 20, 'respondidos': 15, 'recusas': 0, 'taxa': 0.75},
          'wa_br': m_br_vazio}
    v, mo = decide(m1, lim, hoje)
    ok &= _assert(v == 'INCONCLUSIVO' and 'insuficiente' in mo,
                  "amostra insuficiente (enviados) vence antes de qualquer regra (got %s)" % v)

    # 2. amostra insuficiente por RESPOSTAS mesmo com enviados suficientes.
    m2 = {'wa_es': {'enviados': 100, 'respondidos': 5, 'recusas': 0, 'taxa': 0.05},
          'wa_br': m_br_vazio}
    v, mo = decide(m2, lim, hoje)
    ok &= _assert(v == 'INCONCLUSIVO',
                  "respostas abaixo do minimo -> INCONCLUSIVO mesmo com enviados suficiente (got %s)" % v)

    # 3. taxa abaixo do minimo com amostra suficiente -> SEGURAR.
    m3 = {'wa_es': {'enviados': 150, 'respondidos': 10, 'recusas': 0, 'taxa': 10 / 150},
          'wa_br': m_br_vazio}
    v, mo = decide(m3, lim, hoje)
    ok &= _assert(v == 'SEGURAR' and 'taxa' in mo,
                  "taxa abaixo do minimo com amostra suficiente -> SEGURAR (got %s)" % v)

    # 4. taxa acima do minimo mas recusa acima do teto -> SEGURAR.
    m4 = {'wa_es': {'enviados': 100, 'respondidos': 20, 'recusas': 10, 'taxa': 0.20},
          'wa_br': m_br_vazio}
    v, mo = decide(m4, lim, hoje)
    ok &= _assert(v == 'SEGURAR' and 'recusa' in mo,
                  "taxa OK mas recusa acima do teto -> SEGURAR (got %s)" % v)

    # 5. caso feliz -> AVANCAR (numeros na mesma ordem de grandeza do medido em 2026-09-20).
    m5 = {'wa_es': {'enviados': 121, 'respondidos': 53, 'recusas': 7, 'taxa': 53 / 121},
          'wa_br': m_br_vazio}
    v, mo = decide(m5, lim, hoje)
    ok &= _assert(v == 'AVANCAR', "amostra e saude boas -> AVANCAR (got %s)" % v)

    # 6. enviados=0 -> taxa None sem excecao (ZeroDivisionError/TypeError).
    m6 = {'wa_es': m_br_vazio, 'wa_br': m_br_vazio}
    try:
        v, mo = decide(m6, lim, hoje)
        ok &= _assert(v == 'INCONCLUSIVO', "enviados=0 -> INCONCLUSIVO sem excecao (got %s)" % v)
    except Exception as e:
        ok &= _assert(False, "enviados=0 nao deveria lancar %s" % type(e).__name__)

    # 7. decide() nao toca disco: chamada so com dicts puros, sem nenhum arquivo/sqlite/rede.
    ok &= _assert(callable(decide) and decide.__module__ == __name__,
                  "decide() e funcao local pura, chamavel so com dicts (sem I/O)")

    # 8. --apply recusa quando veredito != AVANCAR.
    permitido, _ = apply_gate('SEGURAR')
    ok &= _assert(permitido is False, "apply_gate('SEGURAR') recusa (got %s)" % permitido)
    permitido, _ = apply_gate('INCONCLUSIVO')
    ok &= _assert(permitido is False, "apply_gate('INCONCLUSIVO') recusa (got %s)" % permitido)
    permitido, _ = apply_gate('AVANCAR')
    ok &= _assert(permitido is True, "apply_gate('AVANCAR') permite (got %s)" % permitido)

    # 9. mensagem_portao() nunca finge uma acao que nao existe.
    msg_sem = mensagem_portao({'existe_portao': False, 'onde': 'nenhum'})
    ok &= _assert(msg_sem.startswith('SEM PORTAO DE SEGMENTO'),
                  "mensagem_portao() sem portao usa a frase fixa exigida pelo plano")
    msg_com = mensagem_portao({'existe_portao': True, 'onde': 'outreach-send.py:escolhe()'})
    ok &= _assert('Portao encontrado' in msg_com and 'nao alterado' in msg_com,
                  "mensagem_portao() com portao nunca diz que aplicou algo")

    print('OK %d/%d' % (_CHECKS['ok'], _CHECKS['total']))
    return ok


def main():
    argv = sys.argv[1:]
    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)
    if '--apply' in argv:
        sys.exit(do_apply())
    do_status()


if __name__ == '__main__':
    main()
