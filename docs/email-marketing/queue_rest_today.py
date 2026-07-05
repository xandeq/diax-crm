# -*- coding: utf-8 -*-
"""Enfileira spa/hotel/transportadora (loja JA foi enfileirada na campanha 625808df).
Depois monitora as 4 campanhas ate o worker (ciclo 5min) dispatchar. NAO re-enfileira loja."""
import os, requests, json, datetime, time
from pathlib import Path

for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
    line = line.strip()
    if line and not line.startswith('#') and '=' in line:
        k, v = line.split('=', 1); os.environ[k] = v

base = 'https://api.alexandrequeiroz.com.br'
DIR = str(Path(__file__).resolve().parent) + '/'
tok = requests.post(base + '/api/v1/auth/login', json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']}, timeout=20).json()
tok = tok.get('accessToken') or tok.get('token')
H = {'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'}
HEALTHY = ('Brevo', 'Mailjet', 'Resend', 'SendGrid')

SUBJECT = {'spa': "Um app de agendamento com a marca do seu spa?",
           'hotel': "Reservas diretas no site do seu hotel?",
           'transportadora': "Um sistema sob medida para a sua transportadora?"}
SERVICE = {'spa': 'Apps', 'hotel': 'Sites', 'transportadora': 'Software sob demanda'}

leads = json.load(open(DIR + 'previews/wave1115-leads.json', encoding='utf-8'))
def ts(): return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

# loja JA enfileirada
track = {'loja': '625808df-56bc-4a79-a700-dc8869781b7e'}

for seg in ['spa', 'hotel', 'transportadora']:
    html = open(DIR + 'templates/wave-' + seg + '.html', encoding='utf-8').read()
    cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                       json={"name": "AQ - " + seg + " - " + SERVICE[seg] + " - 2026-06-18", "subject": SUBJECT[seg], "bodyHtml": html})
    cid = cr.json().get('id')
    future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
    sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H, timeout=20, json={'scheduledAt': future.isoformat()})
    st = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H, json={}, timeout=60)
    ok = [{'customerId': x['id'], 'assignedProvider': x['assignedProvider']} for x in leads[seg]['leads'] if x['assignedProvider'] in HEALTHY]
    qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60, json={"campaignId": cid, "leads": ok})
    jr = json.loads(qq.text)
    print(f'[{ts()}] {seg:14} sched {sc.status_code} test {st.status_code} | QUEUE {qq.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')
    if qq.status_code in (200, 201, 202):
        track[seg] = cid
    else:
        print('   !!! queue rejeitado:', qq.text[:300])
    time.sleep(2)

print(f'\n[{ts()}] Campanhas enfileiradas: {list(track)}')
print('Monitorando dispatch do worker (ciclo 5min)...\n')

def snap(cid):
    d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

for t in range(20):  # ate ~10 min
    time.sleep(30)
    line = []
    total = 0
    for seg, cid in track.items():
        s, d, f = snap(cid)
        total += s
        line.append(f'{seg}={s}/{d}/{f}')
    print(f'  +{(t+1)*30:3}s  total_sent={total} | ' + ' '.join(line))
    if total >= 60:  # ~63 esperados
        print('\n>>> Dispatch completo.')
        break

res = {seg: dict(zip(('sent', 'delivered', 'failed'), snap(cid))) for seg, cid in track.items()}
res['_campaigns'] = track
json.dump(res, open(DIR + 'result-wave-2026-06-18.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
print('\nLIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})
print('Salvo: result-wave-2026-06-18.json')
