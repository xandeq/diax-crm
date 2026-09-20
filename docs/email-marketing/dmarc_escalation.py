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
  python dmarc_escalation.py --drill                   # dry-run do drill de reversao (12-05)
  python dmarc_escalation.py --drill --apply            # roda o drill de verdade contra _gsddrill,
                                                          # NUNCA contra o _dmarc real
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

# 12-05 Task 2: registro descartavel para provar o par escrita<->reversao SEM usar o _dmarc real
# como cobaia. Subdominio inexistente, sem nenhum servico apontando pra ele, TTL curto.
DRILL_RECORD = '_gsddrill.alexandrequeiroz.com.br.'

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
    5 (Spam), contados por queue_item_id DISTINTO. Nunca o contador agregado de opt-out da
    campanha, que mistura spam com descadastro voluntario (achado da pesquisa,
    BrevoWebhookController.HandleOptOutAsync)."""
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
    ok &= _selftest_decide()
    return ok


# ---------- decisao (funcao pura — nao consulta nada) ----------

def decide(st, health, today):
    """(acao, motivo, pct_alvo). Ordem de avaliacao FIXA:
      a) bounce/spam acima do limiar -> revert (ou hold se pct ja e o minimo). Vem ANTES de
         tudo de proposito: reversao por saude nao espera os 7 dias nem exige amostra minima
         (D-02/D-04).
      b) pct==100 -> done
      c) sem dmarc_changed_at no state -> hold
      d) menos de STEP_MIN_DAYS desde o ultimo degrau -> hold
      e) amostra insuficiente (health['sent'] < ADVANCE_MIN_SENT) -> hold. So bloqueia AVANCO,
         nunca reversao — ver (a).
      f) senao -> advance
    """
    pct = get_pct(st)
    bounce_pct = health['bounce_rate'] * 100
    spam_pct = health['spam_rate'] * 100

    if health['bounce_rate'] > BOUNCE_REVERT or health['spam_rate'] > SPAM_REVERT:
        motivo = (f"bounce={bounce_pct:.2f}% spam={spam_pct:.2f}% em {health['sent']} envios "
                  f"(limiar bounce>{BOUNCE_REVERT*100:.0f}% ou spam>{SPAM_REVERT*100:.1f}%)")
        if pct <= STEPS[0]:
            return ('hold', f'ja no degrau minimo (pct={STEPS[0]}) - {motivo}', STEPS[0])
        idx = STEPS.index(pct) if pct in STEPS else 0
        alvo = STEPS[max(idx - 1, 0)]
        return ('revert', motivo, alvo)

    if pct >= STEPS[-1]:
        return ('done', f'pct={STEPS[-1]} atingido', STEPS[-1])

    changed_at = get_changed_at(st)
    if not changed_at:
        return ('hold', 'sem data de ultimo degrau no state (dmarc_changed_at ausente)', pct)

    changed_date = datetime.date.fromisoformat(changed_at)
    dias_passados = (today - changed_date).days
    if dias_passados < STEP_MIN_DAYS:
        faltam = STEP_MIN_DAYS - dias_passados
        return ('hold', f'faltam {faltam} dias para o proximo degrau ({dias_passados}/{STEP_MIN_DAYS} dias)', pct)

    if health['sent'] < ADVANCE_MIN_SENT:
        return ('hold', f"amostra insuficiente: {health['sent']} enviados < {ADVANCE_MIN_SENT}", pct)

    idx = STEPS.index(pct) if pct in STEPS else 0
    proximo = STEPS[min(idx + 1, len(STEPS) - 1)]
    motivo = (f"bounce={bounce_pct:.2f}% spam={spam_pct:.2f}% em {health['sent']} envios, "
              f"{dias_passados} dias no degrau — dentro dos limiares")
    return ('advance', motivo, proximo)


def next_step(pct):
    idx = STEPS.index(pct) if pct in STEPS else 0
    return STEPS[min(idx + 1, len(STEPS) - 1)]


def prev_step(pct):
    idx = STEPS.index(pct) if pct in STEPS else 0
    return STEPS[max(idx - 1, 0)]


# ---------- escrita (gated por apply=False default — NUNCA acionada por este plano) ----------

def set_pct(pct, apply=False):
    """Monta (e, se apply=True, executa) a escrita no WHM. Sempre re-localiza a Line ATUAL via
    read_zone_dmarc() primeiro (Pitfall 1 — nunca reaproveitar a Line de uma chamada anterior,
    nem a do --status). Em dry-run (default), imprime a requisicao com o token mascarado e
    NAO faz nenhuma chamada de escrita."""
    load_env()
    zone = read_zone_dmarc()
    line = zone['line']
    txtdata = TPL.format(pct=pct)
    params = {
        'api.version': 1, 'domain': DOMAIN, 'line': line, 'type': 'TXT',
        'name': RECORD, 'class': 'IN', 'ttl': TTL, 'txtdata': txtdata,
    }
    host = os.environ.get('WHM_HOST'); port = os.environ.get('WHM_PORT'); user = os.environ.get('WHM_USER')
    url = f'https://{host}:{port}/json-api/editzonerecord'

    if not apply:
        print('DRY-RUN - requisicao que SERIA enviada:')
        print(f'  GET {url}')
        print(f'  Authorization: whm {user}:***')
        for k, v in params.items():
            print(f'  {k}={v}')
        print(f'  txtdata final: {txtdata}')
        return {'applied': False, 'params': params}

    resp = whm_get('editzonerecord', params)
    if resp.get('metadata', {}).get('result') != 1:
        raise RuntimeError(f'editzonerecord falhou: {resp}')
    time.sleep(5)
    verify_out = dns_verify()
    if f'pct={pct}' not in verify_out:
        raise RuntimeError(f'verificacao pos-escrita falhou — nslookup nao mostra pct={pct}:\n{verify_out}')
    log_line(f'editzonerecord aplicado: line={line} pct={pct} - verificado no NS autoritativo')
    return {'applied': True, 'params': params, 'response': resp, 'verify': verify_out}


def commit_state(st, pct, acao, health=None, motivo=None):
    """Grava dmarc_pct/dmarc_changed_at/dmarc_history. Chamada APENAS no caminho apply=True
    (Pitfall 2: a data e a unica memoria de "quando mudou" — DNS nao guarda isso; gravar em
    dry-run corromperia o gate dos 7 dias). Additive: preserva todas as chaves existentes."""
    from_pct = get_pct(st)
    today_iso = datetime.date.today().isoformat()
    st['dmarc_pct'] = pct
    st['dmarc_changed_at'] = today_iso
    st.setdefault('dmarc_history', []).append({
        'date': today_iso, 'from': from_pct, 'to': pct, 'acao': acao, 'motivo': motivo, 'health': health,
    })
    save_state(st)
    return st


def tg_send(text):
    """Copia modulo-local do padrao de daily_waves.py — NAO importa/estende
    D:/claude-code/n8n-local/alerta_telegram.py (choices fechados waha/n8n, state em outro repo)."""
    try:
        tok = os.environ.get('TELEGRAM_BOT_TOKEN'); chat = os.environ.get('TELEGRAM_CHAT_ID')
        if not tok or not chat: return
        requests.post(f'https://api.telegram.org/bot{tok}/sendMessage',
                      json={'chat_id': chat, 'text': text, 'parse_mode': 'HTML'}, timeout=20)
    except Exception as e:
        log_line(f'telegram falhou: {e}')


# ---------- drill de reversao (12-05 Task 2) — nunca toca o _dmarc real ----------

def dns_verify_name(name):
    """Mesma logica de dns_verify(), mas parametrizada por nome — usada pelo drill contra
    DRILL_RECORD, nunca contra RECORD."""
    r = subprocess.run(['nslookup', '-type=TXT', name.rstrip('.'), NS_AUTH],
                        capture_output=True, text=True, timeout=30)
    return (r.stdout or '') + (r.stderr or '')


def _drill_find(name):
    """dumpzone -> lista de registros TXT com este name (0, 1 ou, em teoria, mais de 1 — o
    chamador decide o que fazer com cada contagem; drill nunca assume 'exatamente 1' como
    read_zone_dmarc faz para o _dmarc real)."""
    resp = whm_get('dumpzone', {'api.version': 1, 'domain': DOMAIN})
    records = resp['data']['zone'][0]['record']
    found = [r for r in records if r.get('type') == 'TXT' and r.get('name') == name]
    return found


def _nslookup_retry(name, expect_substr, max_tries=3, wait_between=15, wait_before=10):
    """Fura cache aguardando o NS recarregar a zona (TTL curto nao afeta consulta direta ao NS,
    mas o reload nao e instantaneo). Regra de 2 retries do projeto: no maximo 3 tentativas."""
    log_line(f'aguardando {wait_before}s antes do primeiro nslookup de {name}...')
    time.sleep(wait_before)
    last_out = ''
    for attempt in range(1, max_tries + 1):
        last_out = dns_verify_name(name)
        if expect_substr in last_out:
            return last_out
        if attempt < max_tries:
            log_line(f'nslookup tentativa {attempt}/{max_tries} nao contem {expect_substr!r}, retry em {wait_between}s')
            time.sleep(wait_between)
    raise RuntimeError(f'nslookup nao mostrou {expect_substr!r} apos {max_tries} tentativas em {name}:\n{last_out}')


def _drill_fallback_b(log, motivo):
    """Fallback B (12-05 Task 2): se addzonerecord/removezonerecord nao funcionarem, reescreve o
    _dmarc real com o MESMO valor que ele ja tem (pct=25) — exercita localizar->editar->verificar
    contra o registro real com resultado semanticamente nulo. Nao chama commit_state (nao e um
    degrau). LIMITACAO CONHECIDA, declarada aqui de proposito: a MUDANCA de valor do _dmarc so
    fica provada em dry-run neste caminho, nunca em producao."""
    log_line(f'FALLBACK B acionado: {motivo}')
    log['fallback_used'] = True
    log['fallback_motivo'] = motivo
    result = set_pct(25, apply=True)  # reescreve com o MESMO valor - resultado semanticamente nulo
    log['fallback_result'] = result
    tg_send('🧪 DMARC drill (fallback B): addzonerecord indisponivel, exercitado '
            'localizar/editar/verificar contra o _dmarc real com valor identico (pct=25→25). '
            'Mudanca de valor NAO provada em producao neste caminho.')
    return log


def do_drill(apply=False):
    """Task 2 (12-05): prova em producao, de ponta a ponta, do par escrita<->reversao contra
    DRILL_RECORD — nunca contra o _dmarc real. Dry-run por padrao (apply=False, so imprime o
    plano); apply=True executa de verdade. PROIBIDO alterar o txtdata do _dmarc para um valor
    diferente do atual neste caminho (o fallback B reescreve com o MESMO valor, de proposito)."""
    load_env()
    name = DRILL_RECORD
    log = {'target': name, 'steps': [], 'fallback_used': False}

    if not apply:
        print('DRY-RUN --drill: nenhuma chamada de escrita executada. Use --drill --apply para rodar de verdade.')
        print(f'  alvo: {name}')
        print('  sequencia: addzonerecord(v1) -> localizar -> editzonerecord(v2) -> nslookup(v2) -> '
              'localizar -> editzonerecord(v1, reversao) -> nslookup(v1) -> removezonerecord -> '
              'dumpzone(confere ausencia) -> tg_send de teste')
        return log

    pre_existing = _drill_find(name)
    if pre_existing:
        raise RuntimeError(f'{name} ja existe na zona antes do drill comecar (lixo de execucao anterior?): {pre_existing}')

    # Passo 1: addzonerecord v1
    add_params = {'api.version': 1, 'domain': DOMAIN, 'type': 'TXT', 'name': name,
                  'class': 'IN', 'ttl': 300, 'txtdata': 'gsd-drill-v1'}
    try:
        add_resp = whm_get('addzonerecord', add_params)
    except Exception as e:
        log_line(f'addzonerecord levantou excecao ({e}) - acionando fallback B')
        return _drill_fallback_b(log, motivo=str(e))
    if add_resp.get('metadata', {}).get('result') != 1:
        log_line(f'addzonerecord nao retornou result=1 ({add_resp}) - acionando fallback B')
        return _drill_fallback_b(log, motivo=str(add_resp))
    log['steps'].append({'step': 1, 'action': 'addzonerecord v1', 'result': add_resp.get('metadata')})
    log_line(f'drill passo 1 OK: addzonerecord {name} = gsd-drill-v1')

    try:
        # Passo 2+3: localizar por nome (nunca pela posicao do add) e editar para v2
        found = _drill_find(name)
        if len(found) != 1:
            raise RuntimeError(f'esperava exatamente 1 {name} apos add, achei {len(found)}: {found}')
        line = int(found[0]['Line'])
        edit_v2 = {'api.version': 1, 'domain': DOMAIN, 'line': line, 'type': 'TXT', 'name': name,
                   'class': 'IN', 'ttl': 300, 'txtdata': 'gsd-drill-v2'}
        edit_resp = whm_get('editzonerecord', edit_v2)
        if edit_resp.get('metadata', {}).get('result') != 1:
            raise RuntimeError(f'editzonerecord (v2) falhou: {edit_resp}')
        log['steps'].append({'step': 3, 'action': 'editzonerecord v2', 'line': line, 'result': edit_resp.get('metadata')})
        log_line(f'drill passo 3 OK: editzonerecord line={line} -> gsd-drill-v2')

        # Passo 4: verificar v2 no NS autoritativo — a mesma asserção que set_pct(apply=True) faz
        out_v2 = _nslookup_retry(name, 'gsd-drill-v2')
        log['steps'].append({'step': 4, 'nslookup': out_v2.strip()})
        log_line('drill passo 4 OK: nslookup confirma gsd-drill-v2')

        # Passo 5: reversao — relocalizar (a Line pode ter mudado) e editar de volta para v1
        found2 = _drill_find(name)
        if len(found2) != 1:
            raise RuntimeError(f'esperava exatamente 1 {name} antes de reverter, achei {len(found2)}: {found2}')
        line2 = int(found2[0]['Line'])
        edit_v1 = {'api.version': 1, 'domain': DOMAIN, 'line': line2, 'type': 'TXT', 'name': name,
                   'class': 'IN', 'ttl': 300, 'txtdata': 'gsd-drill-v1'}
        edit_resp2 = whm_get('editzonerecord', edit_v1)
        if edit_resp2.get('metadata', {}).get('result') != 1:
            raise RuntimeError(f'editzonerecord (reversao v1) falhou: {edit_resp2}')
        log['steps'].append({'step': 5, 'action': 'editzonerecord reversao v1', 'line': line2, 'result': edit_resp2.get('metadata')})
        log_line(f'drill passo 5 OK: editzonerecord line={line2} -> gsd-drill-v1 (reversao)')

        out_v1 = _nslookup_retry(name, 'gsd-drill-v1')
        log['steps'].append({'step': '5-verify', 'nslookup': out_v1.strip()})
        log_line('drill passo 5 verify OK: nslookup confirma gsd-drill-v1 apos reversao — prova o caminho de D-02')

        # Passo 6: remover pela Line relocalizada e confirmar ausencia no dumpzone final
        found3 = _drill_find(name)
        if found3:
            line3 = int(found3[0]['Line'])
            rm_resp = whm_get('removezonerecord', {'api.version': 1, 'domain': DOMAIN, 'line': line3})
            log['steps'].append({'step': 6, 'action': 'removezonerecord', 'line': line3, 'result': rm_resp.get('metadata')})
            log_line(f'drill passo 6: removezonerecord line={line3} result={rm_resp.get("metadata")}')
        found_final = _drill_find(name)
        if found_final:
            raise RuntimeError(f'{name} ainda presente na zona apos removezonerecord: {found_final}')
        log['steps'].append({'step': 'dumpzone-final', 'contains_gsddrill': False})
        log_line('drill passo 6 confirmado: dumpzone final NAO contem _gsddrill')

    finally:
        # Limpeza garantida: um registro de drill orfao na zona e lixo, nao incidente, mas nao
        # pode ficar. Tenta remover de novo se, por qualquer excecao acima, ainda existir.
        try:
            leftover = _drill_find(name)
            if leftover:
                line4 = int(leftover[0]['Line'])
                whm_get('removezonerecord', {'api.version': 1, 'domain': DOMAIN, 'line': line4})
                log_line(f'limpeza finally: removeu {name} orfao (line={line4})')
        except Exception as e:
            log_line(f'limpeza finally falhou: {e} - {name} pode ter ficado orfao, checar manualmente')

    # Passo 7: Telegram de teste pelo mesmo tg_send() que a reversao real usaria
    tg_send('🧪 DMARC drill: reversão exercitada com sucesso em _gsddrill (nenhuma alteração no _dmarc)')
    log['steps'].append({'step': 7, 'action': 'tg_send teste'})
    log_line('drill passo 7: telegram de teste enviado')

    return log


# ---------- CLI: camada de decisao/escrita ----------

def do_check(simulate_bad=False):
    load_env()
    st = load_state()
    if simulate_bad:
        # --simulate-bad-window exercita o caminho de REVERSAO de decide() sem depender do
        # banco real. O pct real de producao hoje e 25 (o minimo) — nesse pct, mesmo saude
        # ruim so produz 'hold, ja no minimo' (ver decide(), ramo a). Para de fato exercitar
        # o caminho 'revert', o pct usado na simulacao tambem e sintetico (nao vem do state
        # real nem e persistido) — documentado aqui de proposito, nao e um bug.
        st = {'dmarc_pct': 50, 'dmarc_changed_at': (datetime.date.today() - datetime.timedelta(days=10)).isoformat()}
        health = {'sent': 500, 'bounced': 60, 'spam': 0, 'bounce_rate': 0.12, 'spam_rate': 0.0, 'since': 'simulado'}
        print('(--simulate-bad-window ativo - pct=50 e health INJETADOS, nao vem do banco/state real)')
    else:
        since_dt, _desc = _since_datetime_from_state(st)
        health = step_health(since_dt)
    acao, motivo, pct_alvo = decide(st, health, datetime.date.today())
    print(f"acao={acao} pct_atual={get_pct(st)} pct_alvo={pct_alvo}")
    print(f"motivo: {motivo}")
    print(f"health: sent={health['sent']} bounced={health.get('bounced')} spam={health.get('spam')} "
          f"bounce_rate={health['bounce_rate']*100:.2f}% spam_rate={health['spam_rate']*100:.2f}%")
    return acao, motivo, pct_alvo


def do_advance(apply=False):
    st = load_state()
    pct = get_pct(st)
    alvo = next_step(pct)
    health = None
    if apply:
        since_dt, _ = _since_datetime_from_state(st)
        health = step_health(since_dt)
    result = set_pct(alvo, apply=apply)
    if apply:
        commit_state(st, alvo, 'advance', health=health, motivo='advance manual (12-05)')
        tg_send(f"✅ <b>DMARC {pct}% → {alvo}%</b> — bounce {health['bounce_rate']*100:.2f}% / "
                f"spam {health['spam_rate']*100:.2f}% em {health['sent']} envios. Próximo degrau em 7 dias.")
    return result


def do_revert(apply=False):
    st = load_state()
    pct = get_pct(st)
    alvo = prev_step(pct)
    health = None
    if apply:
        since_dt, _ = _since_datetime_from_state(st)
        health = step_health(since_dt)
    result = set_pct(alvo, apply=apply)
    if apply:
        data_ref = get_changed_at(st) or 'desconhecida'
        commit_state(st, alvo, 'revert', health=health, motivo='revert manual (12-05)')
        tg_send(f"🚨 <b>DMARC REVERTIDO</b> {pct}% → {alvo}% — bounce {health['bounce_rate']*100:.2f}% / "
                f"spam {health['spam_rate']*100:.2f}% em {health['sent']} envios desde {data_ref}")
    return result


def do_set_pct(pct, apply=False):
    st = load_state()
    result = set_pct(pct, apply=apply)
    if apply:
        commit_state(st, pct, 'set-pct-manual', motivo=f'set-pct manual {get_pct(st)}->{pct}')
    return result


# ---------- autoteste offline (Task 2: cenarios de decide()) ----------

def _selftest_decide():
    ok = True
    today = datetime.date(2026, 9, 20)
    health_clean = {'sent': 500, 'bounced': 0, 'spam': 0, 'bounce_rate': 0.0, 'spam_rate': 0.0}

    # bounce 12% com pct=50 -> revert para 25
    st_50 = {'dmarc_pct': 50, 'dmarc_changed_at': '2026-09-01'}
    health_bad = {'sent': 500, 'bounced': 60, 'spam': 0, 'bounce_rate': 0.12, 'spam_rate': 0.0}
    acao, _, alvo = decide(st_50, health_bad, today)
    ok &= _assert(acao == 'revert' and alvo == 25, f'bounce 12% pct=50 -> revert para 25 (got {acao}/{alvo})')

    # bounce 12% com pct=25 -> hold (ja no minimo)
    st_25 = {'dmarc_pct': 25, 'dmarc_changed_at': '2026-09-01'}
    acao, _, alvo = decide(st_25, health_bad, today)
    ok &= _assert(acao == 'hold' and alvo == 25, f'bounce 12% pct=25 -> hold no minimo (got {acao}/{alvo})')

    # spam 0.2% com bounce 0% -> revert
    health_spam = {'sent': 500, 'bounced': 0, 'spam': 1, 'bounce_rate': 0.0, 'spam_rate': 0.002}
    acao, _, _ = decide(st_50, health_spam, today)
    ok &= _assert(acao == 'revert', f'spam 0.2% bounce 0% -> revert (got {acao})')

    # saude limpa + changed_at 3 dias atras -> hold citando dias que faltam
    st_3d = {'dmarc_pct': 25, 'dmarc_changed_at': (today - datetime.timedelta(days=3)).isoformat()}
    acao, motivo, _ = decide(st_3d, health_clean, today)
    ok &= _assert(acao == 'hold' and 'dias' in motivo, f'3 dias -> hold citando dias (got {acao}/{motivo})')

    # saude limpa + 8 dias + sent=150 -> hold por amostra insuficiente
    st_8d = {'dmarc_pct': 25, 'dmarc_changed_at': (today - datetime.timedelta(days=8)).isoformat()}
    health_150 = {'sent': 150, 'bounced': 0, 'spam': 0, 'bounce_rate': 0.0, 'spam_rate': 0.0}
    acao, motivo, _ = decide(st_8d, health_150, today)
    ok &= _assert(acao == 'hold' and '150' in motivo, f'8 dias sent=150 -> hold por amostra (got {acao}/{motivo})')

    # saude limpa + 8 dias + sent=500 e pct=25 -> advance para 50
    health_500 = {'sent': 500, 'bounced': 0, 'spam': 0, 'bounce_rate': 0.0, 'spam_rate': 0.0}
    acao, _, alvo = decide(st_8d, health_500, today)
    ok &= _assert(acao == 'advance' and alvo == 50, f'8 dias sent=500 pct=25 -> advance para 50 (got {acao}/{alvo})')

    # pct=100 e saude limpa -> done
    st_100 = {'dmarc_pct': 100, 'dmarc_changed_at': (today - datetime.timedelta(days=30)).isoformat()}
    acao, _, _ = decide(st_100, health_clean, today)
    ok &= _assert(acao == 'done', f'pct=100 saude limpa -> done (got {acao})')

    return ok


# ---------- CLI ----------

def main():
    argv = sys.argv[1:]
    apply_flag = '--apply' in argv
    simulate_bad = '--simulate-bad-window' in argv

    if '--selftest' in argv:
        sys.exit(0 if selftest() else 1)
    if '--status' in argv:
        do_status(); return
    if '--check' in argv:
        do_check(simulate_bad=simulate_bad); return
    if '--advance' in argv:
        do_advance(apply=apply_flag); return
    if '--revert' in argv:
        do_revert(apply=apply_flag); return
    if '--set-pct' in argv:
        idx = argv.index('--set-pct')
        pct = int(argv[idx + 1])
        do_set_pct(pct, apply=apply_flag); return
    if '--drill' in argv:
        do_drill(apply=apply_flag); return
    # sem argumentos = mesmo que --check
    do_check(simulate_bad=simulate_bad)


if __name__ == '__main__':
    main()
