# -*- coding: utf-8 -*-
"""Patch w1+w2 campaigns (add utm_source to HTML + set Scheduled status), then send all waves.
Run after wave 3 build completes.
"""
import os, sys, json, datetime, time, pyodbc, requests
from pathlib import Path

DIR = str(Path(__file__).resolve().parent) + '/'
CONN_STR = (
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=sql1002.site4now.net;DATABASE=db_aaf0a8_diaxcrm;'
    'UID=db_aaf0a8_diaxcrm_admin;PWD=Alexandre10#;'
    'Encrypt=yes;TrustServerCertificate=yes;'
)
base = 'https://api.alexandrequeiroz.com.br'
HEALTHY = ['Brevo', 'Mailjet', 'Resend', 'SendGrid']

def ts():
    return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

def load_env():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ[k] = v

def login():
    r = requests.post(base + '/api/v1/auth/login',
                      json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']},
                      timeout=20).json()
    return {'Authorization': 'Bearer ' + (r.get('accessToken') or r.get('token')), 'Content-Type': 'application/json'}

def campaign_counts(cid, H):
    d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

# ─────────────────── STEP 1: PATCH EXISTING CAMPAIGNS IN DB ───────────────────
def patch_campaigns_db(campaign_ids):
    """Add utm_source=email to HTML + set status=Scheduled for given campaign IDs."""
    print(f'[{ts()}] Conectando ao SQL Server...')
    conn = pyodbc.connect(CONN_STR, timeout=30)
    conn.autocommit = True
    cur = conn.cursor()

    scheduled_at = (datetime.datetime.utcnow() + datetime.timedelta(minutes=5)).strftime('%Y-%m-%d %H:%M:%S')
    updated = 0
    for cid in campaign_ids:
        # Update body_html: replace &amp;utm_campaign=aq with UTMs
        cur.execute("""
            UPDATE email_campaigns
            SET body_html = REPLACE(body_html,
                '&amp;utm_campaign=aq',
                '&amp;utm_source=email&amp;utm_medium=cold_email&amp;utm_campaign=aq'),
                status = 1,
                scheduled_at = ?
            WHERE id = ?
              AND status = 0
        """, scheduled_at, cid)
        if cur.rowcount > 0:
            print(f'[{ts()}]   OK patched {cid[:8]}... -> status=Scheduled, utm_source added')
            updated += 1
        else:
            # Maybe already scheduled or doesn't exist
            cur.execute('SELECT id, status, body_html FROM email_campaigns WHERE id = ?', cid)
            row = cur.fetchone()
            if row:
                has_utm = 'utm_source=email' in (row[2] or '')
                print(f'[{ts()}]   ~ {cid[:8]}... status={row[1]} has_utm={has_utm} (no update needed)')
            else:
                print(f'[{ts()}]   ✗ {cid[:8]}... NOT FOUND in DB')
    print(f'[{ts()}] DB patch: {updated}/{len(campaign_ids)} campaigns updated')
    conn.close()

# ─────────────────── STEP 2: SEND A WAVE ───────────────────
def send_wave(wave_file, wave_num, H):
    wave = json.load(open(wave_file, encoding='utf-8'))
    print(f'\n[{ts()}] === SEND WAVE {wave_num} ({wave["date"]}) - {len(wave["segments"])} seg ===')
    results = []
    aborted = False
    for i, s in enumerate(wave['segments']):
        cid = s['campaignId']
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']}
                 for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60,
                           json={'campaignId': cid, 'leads': leads})
        jr = {}
        try: jr = json.loads(qq.text)
        except: pass
        print(f'[{ts()}] {s["seg"]:14} {s.get("service",""):20} | QUEUE {qq.status_code} '
              f'queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid[:8]}...')
        if qq.status_code not in (200, 201, 202):
            # "Nenhum destinatário válido" = leads already queued from a previous partial run → skip gracefully
            if 'Validation.Recipients' in qq.text:
                print(f'   (leads ja enfileirados de run anterior — pulando segmento)')
                results.append({'seg': s['seg'], 'cid': cid, 'queued': 0, 'skipped': 0})
                if i == 0:
                    # still need to check breaker on this campaign before moving to next
                    print(f'[{ts()}] sonda breaker: aguardando worker processar segmento ja enfileirado...')
                    sent = 0
                    for t in range(42):
                        time.sleep(10)
                        sent, deliv, fail = campaign_counts(cid, H)
                        print(f'   +{(t+1)*10}s sent={sent} delivered={deliv} failed={fail}')
                        if sent > 0:
                            break
                    if sent == 0:
                        print(f'\n[{ts()}] !!! BREAKER ABERTO - {s["seg"]} 0 enviados em 420s')
                        aborted = True; break
                    print(f'[{ts()}] breaker OK - seguindo.\n')
                continue
            print(f'   !!! QUEUE rejeitado: {qq.text[:300]}')
            aborted = True
            break
        results.append({'seg': s['seg'], 'cid': cid, 'queued': jr.get('queuedCount'), 'skipped': jr.get('skippedCount')})
        if i == 0:
            print(f'[{ts()}] sonda breaker: aguardando worker (ciclo 5min)...')
            sent = 0
            for t in range(42):
                time.sleep(10)
                sent, deliv, fail = campaign_counts(cid, H)
                print(f'   +{(t+1)*10}s sent={sent} delivered={deliv} failed={fail}')
                if sent > 0:
                    break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO — wave {wave_num} {s["seg"]} 0 enviados em 420s')
                aborted = True; break
            print(f'[{ts()}] breaker OK — seguindo.\n')
        if i < len(wave['segments']) - 1 and not aborted:
            time.sleep(15)

    time.sleep(20)
    total_sent = 0
    for r in results:
        s2, d2, f2 = campaign_counts(r['cid'], H)
        r['sent'] = s2; r['delivered'] = d2; r['failed'] = f2
        total_sent += s2
        print(f"  {r['seg']:14} queued={r['queued']} | sent={s2} delivered={d2} failed={f2}")
    print(f'[{ts()}] Wave {wave_num} TOTAL enviado: {total_sent} | aborted={aborted}')

    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})
    return total_sent, aborted


if __name__ == '__main__':
    load_env()
    H = login()
    print(f'[{ts()}] Login OK. Iniciando patch + send pipeline para 2026-06-20')

    # ── Step 1: Patch w1 + w2 campaigns in DB ──
    w1 = json.load(open(DIR + 'previews/wave-2026-06-20-w1.json', encoding='utf-8'))
    w2 = json.load(open(DIR + 'previews/wave-2026-06-20-w2.json', encoding='utf-8'))
    all_cids = [s['campaignId'] for s in w1['segments']] + [s['campaignId'] for s in w2['segments']]

    # Patch w3 if available
    w3_file = DIR + 'previews/wave-2026-06-20-w3.json'
    w3 = None
    try:
        w3 = json.load(open(w3_file, encoding='utf-8'))
        all_cids += [s['campaignId'] for s in w3['segments']]
    except FileNotFoundError:
        print(f'[{ts()}] wave 3 ainda não disponível — patchando só w1+w2')

    patch_campaigns_db(all_cids)

    # ── Step 2: Send all waves ──
    t1, a1 = send_wave(DIR + 'previews/wave-2026-06-20-w1.json', 1, H)
    if a1:
        print(f'[{ts()}] Wave 1 abortado — PARANDO. Verifique os logs.'); sys.exit(1)

    print(f'\n[{ts()}] Aguardando 60s antes de wave 2...')
    time.sleep(60)

    t2, a2 = send_wave(DIR + 'previews/wave-2026-06-20-w2.json', 2, H)
    if a2:
        print(f'[{ts()}] Wave 2 abortado — PARANDO.'); sys.exit(1)

    if w3:
        print(f'\n[{ts()}] Aguardando 60s antes de wave 3...')
        time.sleep(60)
        t3, a3 = send_wave(w3_file, 3, H)
        total = t1 + t2 + t3
        print(f'\n[{ts()}] ═══ PIPELINE COMPLETO: w1={t1} + w2={t2} + w3={t3} = {total} enviados ═══')
    else:
        total = t1 + t2
        print(f'\n[{ts()}] ═══ PIPELINE w1+w2: {total} enviados (w3 pendente) ═══')

    print(f'[{ts()}] Verificar resultados em: {DIR}result-wave-2026-06-20.json')
