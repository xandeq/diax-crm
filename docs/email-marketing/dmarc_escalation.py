# -*- coding: utf-8 -*-
"""Motor de escalonamento DMARC (EMAL-01, Phase 12).

Le o estado real do DMARC (DNS via WHM + saude de bounce/spam via email_events), decide se o
degrau atual pode avancar (25->50->75->100), e monta a escrita no WHM — mas NUNCA escreve de
verdade sem --apply explicito.

FRONTEIRA DURA (12-01): nenhuma chamada a editzonerecord e executada de verdade neste plano.
--apply existe no codigo (e o produto), mas nao e acionado por nenhuma task do 12-01 — nem
contra _dmarc, nem contra registro de teste. A primeira escrita real acontece no 12-05, atras
de checkpoint humano.

Uso:
  python dmarc_escalation.py --status               # le DNS + state + saude, so leitura
  python dmarc_escalation.py --selftest              # autoteste offline, sem rede/DB, <5s
  python dmarc_escalation.py --check                 # roda decide() e imprime a decisao (leitura)
  python dmarc_escalation.py --advance                # dry-run do proximo degrau (nao escreve)
  python dmarc_escalation.py --revert                 # dry-run do degrau anterior (nao escreve)
  python dmarc_escalation.py --set-pct 50              # dry-run de um pct especifico (nao escreve)
  python dmarc_escalation.py --advance --apply         # ESCREVE de verdade — fora de escopo do 12-01
  python dmarc_escalation.py --simulate-bad-window --check   # forca health ruim p/ testar revert
"""
import os, sys, re, json, time, datetime, subprocess
from pathlib import Path
import requests
import urllib3

# InsecureRequestWarning silenciado explicitamente (nao e rede/DB — so um filtro de warnings —
# seguro em tempo de import; ver waves_smoke.py check imports_locais_resolvem).
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

DIR = str(Path(__file__).resolve().parent) + '/'

DOMAIN   = 'alexandrequeiroz.com.br'
RECORD   = '_dmarc.alexandrequeiroz.com.br.'    # com ponto final, como a zona devolve
TTL      = 14400
NS_AUTH  = 'ns232.prodns.com.br'
STEPS    = [25, 50, 75, 100]                     # D-01: um degrau por semana
TPL      = 'v=DMARC1; p=quarantine; pct={pct}; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r'

STEP_MIN_DAYS    = 7      # D-01: 1 degrau por semana
BOUNCE_REVERT    = 0.05   # D-04 (decisao travada do usuario): bounce > 5% reverte
SPAM_REVERT      = 0.001  # D-04 (decisao travada do usuario): spam complaint > 0.1% reverte
# ADVANCE_MIN_SENT e discricionario e vale SO PARA AVANCAR — a reversao dispara pelo limiar
# puro acima, sem piso de amostra (D-02/D-04: saude ruim reverte mesmo com poucos envios).
ADVANCE_MIN_SENT = 200

STATE_FILE = DIR + 'previews/daily-state.json'
LOG_DIR    = DIR + 'logs/'

# ---------- infra (copiado do padrao waves_core.py / daily_waves.py) ----------

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
    with open(LOG_DIR + 'dmarc-escalation.log', 'a', encoding='utf-8') as f:
        f.write(line + '\n')


def whm_get(path, params):
    """GET em https://{WHM_HOST}:{WHM_PORT}/json-api/{path} — equivalente ao `curl -sk` do
    run_gates_20260904.sh (linhas 15-34). O WHM do HostGator serve cert que nao valida
    (verify=False de proposito). Chamada apenas dentro de funcoes, nunca em tempo de import."""
    host = os.environ['WHM_HOST']; port = os.environ['WHM_PORT']
    user = os.environ['WHM_USER']; token = os.environ['WHM_API_TOKEN']
    url = f'https://{host}:{port}/json-api/{path}'
    headers = {'Authorization': f'whm {user}:{token}'}
    r = requests.get(url, headers=headers, params=params, verify=False, timeout=30)
    r.raise_for_status()
    return r.json()


def parse_pct(txtdata):
    m = re.search(r'pct=(\d+)', txtdata or '')
    return int(m.group(1)) if m else None


def read_zone_dmarc():
    """dumpzone -> localiza o TXT _dmarc por type+name. A Line e lida A CADA CHAMADA e NUNCA
    persistida no state (Pitfall 1: a zona tem 164 linhas e 5 outros dominios; qualquer edicao
    desloca as linhas subsequentes). Se achar 0 ou mais de 1 registro, levanta RuntimeError —
    nunca "escolhe o primeiro" em silencio."""
    resp = whm_get('dumpzone', {'api.version': 1, 'domain': DOMAIN})
    records = resp['data']['zone'][0]['record']
    found = [r for r in records if r.get('type') == 'TXT' and r.get('name') == RECORD]
    if len(found) != 1:
        raise RuntimeError(f'esperava exatamente 1 registro TXT {RECORD!r} na zona, achei {len(found)}: {found}')
    rec = found[0]
    txtdata = rec.get('txtdata', '')
    return {'line': int(rec['Line']), 'txtdata': txtdata, 'ttl': int(rec.get('ttl', TTL)), 'pct': parse_pct(txtdata)}


def dns_verify():
    """Consulta o NS autoritativo de proposito: fura cache de resolver (mesma verificacao do
    run_gates_20260904.sh, linhas 32-34)."""
    r = subprocess.run(['nslookup', '-type=TXT', RECORD.rstrip('.'), NS_AUTH],
                        capture_output=True, text=True, timeout=30)
    return (r.stdout or '') + (r.stderr or '')


def step_health(since_dt):
    """Unica fonte de verdade de bounce/spam (Pitfall 3): email_events.event_type 4 (Bounced) e
    5 (Spam), contados por queue_item_id DISTINTO. NUNCA EmailCampaign.UnsubscribeCount — mistura
    spam com opt-out voluntario (achado da pesquisa, BrevoWebhookController.HandleOptOutAsync)."""
    sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
    import db as crmdb
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT
        (SELECT COUNT(*) FROM email_queue_items WHERE status=2 AND sent_at >= ?) sent,
        (SELECT COUNT(DISTINCT e.queue_item_id) FROM email_events e
          WHERE e.event_type=4 AND e.occurred_at >= ?) bounced,
        (SELECT COUNT(DISTINCT e.queue_item_id) FROM email_events e
          WHERE e.event_type=5 AND e.occurred_at >= ?) spam
    """, since_dt, since_dt, since_dt)
    row = cur.fetchone(); cx.close()
    sent, bounced, spam = (row.sent or 0), (row.bounced or 0), (row.spam or 0)
    result = {'sent': sent, 'bounced': bounced, 'spam': spam, 'since': since_dt.isoformat()}
    if sent == 0:
        result['bounce_rate'] = 0.0; result['spam_rate'] = 0.0; result['no_data'] = True
    else:
        result['bounce_rate'] = bounced / sent; result['spam_rate'] = spam / sent
    return result


def load_state():
    try: return json.load(open(STATE_FILE, encoding='utf-8'))
    except Exception: return {}


def save_state(st):
    json.dump(st, open(STATE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


def get_pct(st):
    return st.get('dmarc_pct', 25)


def get_changed_at(st):
    return st.get('dmarc_changed_at')


def _since_datetime_from_state(st):
    """Janela do degrau atual: desde dmarc_changed_at (date ISO), ou 7 dias se o state ainda
    nao tem essa chave. dmarc_changed_at e sempre gravado como data (nao datetime) por
    commit_state — a hora exata de troca de degrau nao importa para a janela de saude."""
    changed_at = get_changed_at(st)
    if changed_at:
        d = datetime.date.fromisoformat(changed_at)
        return datetime.datetime.combine(d, datetime.time.min), f'desde {changed_at} (degrau atual)'
    dt = datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None) - datetime.timedelta(days=STEP_MIN_DAYS)
    return dt, f'ultimos {STEP_MIN_DAYS} dias (state sem dmarc_changed_at ainda)'


# ---------- CLI: --status (leitura pura) ----------

def do_status():
    print('=== DMARC - status (somente leitura) ===')
    load_env()
    zone = read_zone_dmarc()
    print(f"Zona WHM: pct={zone['pct']} (Line {zone['line']})")
    print(f"  txtdata: {zone['txtdata']}")

    st = load_state()
    state_pct = get_pct(st)
    changed_at = get_changed_at(st)
    print(f"State file: dmarc_pct={state_pct} dmarc_changed_at={changed_at}")
    if zone['pct'] != state_pct:
        print(f"  DIVERGENCIA: zona diz pct={zone['pct']}, state diz pct={state_pct}")
    else:
        print('  Divergencia zona vs state: nenhuma')

    since_dt, janela_desc = _since_datetime_from_state(st)
    health = step_health(since_dt)
    print(f"Saude do degrau atual ({janela_desc}):")
    print(f"  sent={health['sent']} bounced={health['bounced']} spam={health['spam']} "
          f"bounce_rate={health['bounce_rate']*100:.2f}% spam_rate={health['spam_rate']*100:.2f}%")

    print(f"DNS (NS autoritativo {NS_AUTH}):")
    print(dns_verify())


# ---------- autoteste offline ----------

def _assert(cond, label):
    if cond:
        print(f'OK: {label}')
    else:
        print(f'FAIL: {label}')
    return bool(cond)


def selftest():
    ok = True
    ok &= _assert(
        TPL.format(pct=50) == 'v=DMARC1; p=quarantine; pct=50; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r',
        'TPL.format(pct=50) produz a string DMARC exata')
    ok &= _assert(
        parse_pct('v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r') == 25,
        'parse_pct extrai pct=25 de um txtdata valido')
    ok &= _assert(
        parse_pct('v=DMARC1; p=quarantine; rua=mailto:rua@dmarc.brevo.com') is None,
        'parse_pct devolve None em string sem pct=')
    return ok


# ---------- CLI ----------

def main():
    argv = sys.argv[1:]
    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)
    if '--status' in argv or not argv:
        do_status(); return
    print(f'flag desconhecida ou ainda nao implementada nesta task: {argv}', file=sys.stderr)
    sys.exit(2)


if __name__ == '__main__':
    main()
