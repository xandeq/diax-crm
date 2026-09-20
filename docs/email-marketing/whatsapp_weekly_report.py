# -*- coding: utf-8 -*-
"""Relatorio semanal do canal WhatsApp (WHAT-03, Fase 13 plano 03).

Unico requisito desta fase que e codigo genuinamente novo: nem `daily_waves.py:weekly_report()`
(so email) nem `funnel_analysis.py` tocam em WhatsApp. Junta enviados/respondidos/opt-out da
semana e entrega no Telegram, honrando as decisoes travadas em 13-CONTEXT.md:

  - D-01: entrega via Telegram, mesmo canal/estilo do watchdog e do DMARC (Fase 10/12) — o
    `tg_send()` abaixo e copia modulo-local do padrao ja usado em `dmarc_escalation.py`
    (linhas 339-348), NAO um import de `D:/claude-code/n8n-local/alerta_telegram.py` (outro
    repositorio, escolhas fechadas waha/n8n — decisao ja tomada na Fase 12).
  - D-02: sai sexta a TARDE (task Windows propria `DIAX-WA-Weekly-Report`, 17:00 BRT) — NUNCA
    pendurado no ramo de sexta as 08:40 BRT (manha) de `daily_waves.py` (Pitfall 3 da pesquisa).

Fonte de dados: `wa_estado.py` (criado no 13-01) e o UNICO leitor de log/sqlite/CRM — este
script so importa as funcoes prontas (`ler_envios/ler_respostas/ler_blacklist/ler_crm/seg`),
nunca reimplementa parsing de audit log ou consulta SQL.

Este script e SOMENTE LEITURA em relacao a WhatsApp/CRM/n8n: nenhum `sendText`, nenhum
POST `/whatsapp-event`, nenhuma escrita em sqlite. O unico arquivo que ele grava e o proprio
`previews/whatsapp-weekly.json`.

Achado que o relatorio precisa expor (medido no 13-01, `whatsapp-estado.json`): a operacao real
registrou ~186 chat_ids contatados (all-time) enquanto o CRM conhecia ~17, porque
`outreach-send.py` nao chama `POST /api/v1/integrations/whatsapp-event`. Um relatorio construido
so sobre o CRM reportaria quase zero e seria mentira — por isso o bloco `crm` e a linha de
`divergencia` aparecem sempre, nunca escondidos.

Uso:
  python whatsapp_weekly_report.py           # calcula, grava JSON e ENVIA no Telegram
  python whatsapp_weekly_report.py --dry     # calcula e grava JSON, NAO envia Telegram
  python whatsapp_weekly_report.py --selftest # autoteste offline, sem rede/sqlite/CRM, OK n/n
"""
import os
import sys
import json
import datetime
import requests

if hasattr(sys.stdout, 'reconfigure'):
    # console Windows (cp1252) quebra no emoji do formatar() — mesma regra ja documentada
    # no projeto (ab_report.py linha 33-37): reconfigura em vez de exigir env var externa.
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
if DIR not in sys.path:
    sys.path.insert(0, DIR)
import wa_estado  # noqa: E402 — leitor unico do estado real do canal (criado no 13-01)

OUT = DIR + 'previews/whatsapp-weekly.json'
LOG_DIR = DIR + 'logs/'
LOG_FILE = LOG_DIR + 'whatsapp-weekly.log'


# ---------- infra (copiado do padrao dmarc_escalation.py / waves_core.py) ----------

def load_env():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ[k] = v


def log_line(msg):
    os.makedirs(LOG_DIR, exist_ok=True)
    stamp = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    line = f'[{stamp}] {msg}'
    try: print(line)
    except UnicodeEncodeError: print(line.encode('ascii', 'ignore').decode())
    with open(LOG_FILE, 'a', encoding='utf-8') as f:
        f.write(line + '\n')


def tg_send(text):
    """Copia modulo-local do padrao de daily_waves.py/dmarc_escalation.py — NAO importa
    D:/claude-code/n8n-local/alerta_telegram.py (outro repo, escolhas fechadas waha/n8n,
    rejeitado pelo mesmo motivo na Fase 12). Honra D-01 pelo canal/estilo, nao pelo import."""
    try:
        tok = os.environ.get('TELEGRAM_BOT_TOKEN'); chat = os.environ.get('TELEGRAM_CHAT_ID')
        if not tok or not chat:
            log_line('telegram nao enviado: TELEGRAM_BOT_TOKEN/TELEGRAM_CHAT_ID ausentes')
            return False
        requests.post(f'https://api.telegram.org/bot{tok}/sendMessage',
                      json={'chat_id': chat, 'text': text, 'parse_mode': 'HTML'}, timeout=20)
        return True
    except Exception as e:
        log_line(f'telegram falhou: {e}')
        return False


# ---------- janela (funcao pura, testavel offline com 'agora' injetado) ----------

def _filtra_janela(entradas, dias, agora=None):
    """Re-filtra defensivamente por 'ts' >= agora-dias (wa_estado.ler_envios/ler_respostas ja
    janelam por dias= na leitura, mas usam datetime.now() real internamente — esta camada
    permite testar o corte de janela offline, com 'agora' fixo, sem tocar arquivo nenhum)."""
    agora = agora or datetime.datetime.now()
    corte = agora - datetime.timedelta(days=dias)
    return [e for e in entradas if e['ts'] >= corte]


# ---------- calculo (funcao pura — nao le nada, so recebe dados ja carregados) ----------

def _compute_metricas(envios, respostas, blacklist_atual, blacklist_anterior, crm, dias):
    """dict com enviados/respondidos/optout/taxa_resposta/crm/divergencia. Nunca consulta
    arquivo/sqlite/SQL Server — toda leitura acontece em metricas(), antes de chamar isto."""
    enviados_es = [e for e in envios if e['segmento'] == 'wa_es']
    enviados_br = [e for e in envios if e['segmento'] == 'wa_br']
    enviados_desc = [e for e in envios if e['segmento'] not in ('wa_es', 'wa_br')]
    uniq_all = {e['chat_id'] for e in envios}
    uniq_es = {e['chat_id'] for e in enviados_es}
    uniq_br = {e['chat_id'] for e in enviados_br}

    enviados = {
        'total': len(envios), 'wa_es': len(enviados_es), 'wa_br': len(enviados_br),
        'desconhecido': len(enviados_desc),
        'unicos': {'total': len(uniq_all), 'wa_es': len(uniq_es), 'wa_br': len(uniq_br)},
    }

    # respondidos_unicos: um mesmo chat_id que respondeu 2x na semana conta 1 no numerador —
    # dict por chat_id (nao lista) ja deduplica por construcao.
    seg_por_chat = {}
    for r in respostas:
        seg_por_chat[r['chat_id']] = wa_estado.seg(r['chat_id'])
    respondidos = {
        'total': len(seg_por_chat),
        'wa_es': sum(1 for s in seg_por_chat.values() if s == 'wa_es'),
        'wa_br': sum(1 for s in seg_por_chat.values() if s == 'wa_br'),
    }

    def _taxa(num, den):
        return (num / den) if den else None

    taxa_resposta = {
        'geral': _taxa(respondidos['total'], enviados['unicos']['total']),
        'wa_es': _taxa(respondidos['wa_es'], enviados['unicos']['wa_es']),
        'wa_br': _taxa(respondidos['wa_br'], enviados['unicos']['wa_br']),
    }

    tamanho_atual = len(blacklist_atual)
    novos_periodo = (
        (tamanho_atual - blacklist_anterior) if blacklist_anterior is not None else None
    )
    optout = {'total': tamanho_atual, 'novos_periodo': novos_periodo}

    crm_valido = isinstance(crm, dict) and 'erro' not in crm
    enviados_crm = crm.get('enviados_periodo') if crm_valido else None
    delta = (enviados['total'] - enviados_crm) if enviados_crm is not None else None
    divergencia = {
        'enviados_logs': enviados['total'],
        'enviados_crm': enviados_crm,
        'delta': delta,
        'causa': 'outreach-send.py nao chama POST /api/v1/integrations/whatsapp-event',
    }

    return {
        'enviados': enviados, 'respondidos': respondidos, 'optout': optout,
        'taxa_resposta': taxa_resposta, 'crm': crm, 'divergencia': divergencia,
    }


# ---------- leitura + montagem final (I/O isolado aqui, nao em _compute_metricas) ----------

def _load_previous():
    if not os.path.exists(OUT):
        return None
    try:
        with open(OUT, encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        log_line(f'previews/whatsapp-weekly.json anterior ilegivel ({e}) — tratando como 1a execucao')
        return None


def metricas(dias=7):
    """Ponto unico de leitura: chama wa_estado (audit log + sqlite n8n mode=ro + CRM), nunca
    reimplementa parsing. Janela: 7 dias corridos terminando agora, horario LOCAL (logs em BRT);
    o recorte do CRM usa DATEADD(day,-N,SYSUTCDATETIME()) dentro de wa_estado.ler_crm (UTC) —
    por isso periodo.fuso_logs='BRT' e periodo.fuso_crm='UTC' ficam explicitos no JSON."""
    agora = datetime.datetime.now()
    envios = _filtra_janela(wa_estado.ler_envios(dias=dias), dias, agora)
    respostas = _filtra_janela(wa_estado.ler_respostas(dias=dias), dias, agora)
    blacklist_atual = wa_estado.ler_blacklist()
    crm = wa_estado.ler_crm(dias=dias)

    anterior = _load_previous()
    blacklist_anterior = anterior.get('blacklist_tamanho') if anterior else None

    m = _compute_metricas(envios, respostas, blacklist_atual, blacklist_anterior, crm, dias)
    m['periodo'] = {
        'de': (agora - datetime.timedelta(days=dias)).isoformat(timespec='seconds'),
        'ate': agora.isoformat(timespec='seconds'),
        'dias': dias,
        'fuso_logs': 'BRT',
        'fuso_crm': 'UTC',
    }
    m['blacklist_tamanho'] = len(blacklist_atual)
    m['gerado'] = agora.isoformat(timespec='seconds')
    return m


def _grava(m):
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(m, f, ensure_ascii=False, indent=1)


# ---------- formatacao (estilo weekly_report() do email, daily_waves.py linhas 807-830) ----------

def formatar(m):
    """Mensagem HTML curta (<=12 linhas), com a linha de divergencia SEMPRE que
    crm.enviados_periodo != enviados.total — nunca esconde que o CRM esta defasado."""
    e, r, o, t = m['enviados'], m['respondidos'], m['optout'], m['taxa_resposta']

    def pct(x):
        return f'{x * 100:.1f}%' if x is not None else '—'

    linhas = [
        '📱 <b>Semana de WhatsApp</b>',
        f"Enviados: {e['total']} (ES {e['wa_es']} · BR {e['wa_br']})",
        f"Respondidos: {r['total']} (ES {r['wa_es']} · BR {r['wa_br']}) — taxa {pct(t['geral'])}",
        f"  taxa ES: {pct(t['wa_es'])} · taxa BR: {pct(t['wa_br'])}",
        f"Opt-out: {o['total']} (novos: {o['novos_periodo'] if o['novos_periodo'] is not None else '(primeira medicao — sem base de comparacao)'})",
    ]

    crm = m.get('crm') or {}
    crm_enviados = crm.get('enviados_periodo') if isinstance(crm, dict) else None
    delta = m['divergencia'].get('delta')
    if crm_enviados is not None:
        linha_crm = f'CRM registra: {crm_enviados} enviados'
        if delta:
            linha_crm += f' — divergencia de {delta} (outreach-send.py nao escreve no CRM)'
        linhas.append(linha_crm)
    else:
        linhas.append('CRM: dado indisponivel nesta execucao (ver logs)')

    if e.get('desconhecido'):
        linhas.append(f"Segmento desconhecido: {e['desconhecido']} envios (fora de wa_es/wa_br)")

    return '\n'.join(linhas)


# ---------- autoteste offline (sem rede, sem sqlite, sem CRM) ----------

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

    # 1. taxa com denominador zero devolve None (nunca 0.0% / ZeroDivisionError)
    m1 = _compute_metricas([], [], [], None, {'enviados_periodo': 0}, dias=7)
    ok &= _assert(
        m1['taxa_resposta']['geral'] is None
        and m1['taxa_resposta']['wa_es'] is None
        and m1['taxa_resposta']['wa_br'] is None,
        'taxa_resposta devolve None em todos os campos quando nao ha enviados unicos',
    )

    # 2. chat_id que respondeu 2x na semana conta 1 no numerador
    ag = datetime.datetime(2026, 9, 20, 12, 0, 0)
    envios2 = [{'ts': ag, 'chat_id': '5527999016777@c.us', 'nome': 'X', 'segmento': 'wa_es'}]
    respostas2 = [
        {'ts': ag, 'chat_id': '5527999016777@c.us'},
        {'ts': ag, 'chat_id': '5527999016777@c.us'},
    ]
    m2 = _compute_metricas(envios2, respostas2, [], None, {}, dias=7)
    ok &= _assert(
        m2['respondidos']['total'] == 1 and m2['respondidos']['wa_es'] == 1,
        'chat_id que respondeu 2x na semana conta 1 no numerador (dedupe por chat_id)',
    )

    # 3. split wa_es/wa_br pelo DDD (segmento ja vem calculado por wa_estado.seg em cada envio)
    envios3 = [
        {'ts': ag, 'chat_id': '5527999016777@c.us', 'nome': 'ES', 'segmento': 'wa_es'},
        {'ts': ag, 'chat_id': '5511996257748@c.us', 'nome': 'BR', 'segmento': 'wa_br'},
    ]
    m3 = _compute_metricas(envios3, [], [], None, {}, dias=7)
    ok &= _assert(
        m3['enviados']['wa_es'] == 1 and m3['enviados']['wa_br'] == 1
        and m3['enviados']['total'] == 2 and m3['enviados']['desconhecido'] == 0,
        'split wa_es/wa_br pelo DDD soma exatamente enviados.total (nenhum envio perdido)',
    )

    # 4. janela de 7 dias exclui evento de 8 dias atras
    janela_fixture = [
        {'ts': ag - datetime.timedelta(days=1), 'chat_id': 'a', 'nome': 'dentro', 'segmento': 'wa_es'},
        {'ts': ag - datetime.timedelta(days=8), 'chat_id': 'b', 'nome': 'fora', 'segmento': 'wa_es'},
    ]
    filtrado = _filtra_janela(janela_fixture, dias=7, agora=ag)
    ok &= _assert(
        len(filtrado) == 1 and filtrado[0]['chat_id'] == 'a',
        'janela de 7 dias exclui evento de 8 dias atras (mantem so o de 1 dia atras)',
    )

    # 5. primeira medicao de opt-out (sem base de comparacao) devolve None
    m5 = _compute_metricas([], [], ['5527900000000@c.us'], None, {}, dias=7)
    ok &= _assert(
        m5['optout']['total'] == 1 and m5['optout']['novos_periodo'] is None,
        'primeira medicao de opt-out (blacklist_anterior=None) devolve novos_periodo=None',
    )
    m5b = _compute_metricas([], [], ['a@c.us', 'b@c.us', 'c@c.us'], 1, {}, dias=7)
    ok &= _assert(
        m5b['optout']['novos_periodo'] == 2,
        'medicao seguinte calcula novos_periodo = tamanho_agora - tamanho_anterior (3-1=2)',
    )

    # 6. divergencia calculada como logs menos CRM
    envios6 = [{'ts': ag, 'chat_id': 'a', 'nome': 'X', 'segmento': 'wa_es'}] * 10
    m6 = _compute_metricas(envios6, [], [], None, {'enviados_periodo': 7}, dias=7)
    ok &= _assert(
        m6['divergencia']['enviados_logs'] == 10
        and m6['divergencia']['enviados_crm'] == 7
        and m6['divergencia']['delta'] == 3,
        'divergencia.delta = enviados_logs (10) - enviados_crm (7) = 3',
    )
    m6b = _compute_metricas(envios6, [], [], None, {'erro': 'ConnError'}, dias=7)
    ok &= _assert(
        m6b['divergencia']['enviados_crm'] is None and m6b['divergencia']['delta'] is None,
        'divergencia devolve None (nunca 0 nem excecao) quando ler_crm() falha',
    )

    # 7. formatar() produz linha de divergencia quando delta != 0
    m6['periodo'] = {'de': '', 'ate': '', 'dias': 7, 'fuso_logs': 'BRT', 'fuso_crm': 'UTC'}
    msg = formatar(m6)
    ok &= _assert(
        'divergencia de 3' in msg,
        'formatar() inclui a linha de divergencia quando crm.enviados_periodo != enviados.total',
    )

    print('OK %d/%d' % (_CHECKS['ok'], _CHECKS['total']))
    return ok


# ---------- CLI ----------

def main():
    argv = sys.argv[1:]
    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)

    load_env()
    m = metricas(dias=7)
    _grava(m)
    msg = formatar(m)
    print(msg)
    print('gravado em %s' % OUT)

    if '--dry' in argv:
        log_line('modo --dry: relatorio calculado e gravado, Telegram NAO enviado')
    else:
        if tg_send(msg):
            log_line('relatorio semanal enviado ao Telegram')
    return 0


if __name__ == '__main__':
    sys.exit(main())
