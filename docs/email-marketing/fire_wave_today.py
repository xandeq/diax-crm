# -*- coding: utf-8 -*-
"""Reenvio 2026-06-18 dos 4 segmentos que o circuit breaker comeu em 17/06 (loja/spa/hotel/transportadora).
Usa os templates wave-*.html JA CORRIGIDOS (logo width-only). Envia via API do CRM, so 4 providers saudaveis.
SONDA: apos o 1o segmento, verifica sentCount real; se 0 (breaker aberto) ABORTA o resto."""
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

SUBJECT = {
    'loja': "A {{empresa}} aparece no Google de quem quer comprar?",
    'spa': "Um app de agendamento com a marca do seu spa?",
    'hotel': "Reservas diretas no site do seu hotel?",
    'transportadora': "Um sistema sob medida para a sua transportadora?",
}
SERVICE = {'loja': 'Sites', 'spa': 'Apps', 'hotel': 'Sites', 'transportadora': 'Software sob demanda'}

leads = json.load(open(DIR + 'previews/wave1115-leads.json', encoding='utf-8'))

def ts(): return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

def campaign_sent(cid):
    d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

segs = ['loja', 'spa', 'hotel', 'transportadora']
results = []
aborted = False

for i, seg in enumerate(segs):
    html = open(DIR + 'templates/wave-' + seg + '.html', encoding='utf-8').read()
    cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                       json={"name": "AQ - " + seg + " - " + SERVICE[seg] + " - 2026-06-18",
                             "subject": SUBJECT[seg], "bodyHtml": html})
    cid = cr.json().get('id')
    # agenda (Draft -> Scheduled) p/ passar o ReadinessGate de status
    future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
    sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H, timeout=20,
                       json={'scheduledAt': future.isoformat()})
    st = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H, json={}, timeout=60)
    ok = [{'customerId': x['id'], 'assignedProvider': x['assignedProvider']}
          for x in leads[seg]['leads'] if x['assignedProvider'] in HEALTHY]
    qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60,
                       json={"campaignId": cid, "leads": ok})
    jr = json.loads(qq.text)
    qc = jr.get('queuedCount'); sk = jr.get('skippedCount')
    print(f'[{ts()}] {seg:14} -> {SERVICE[seg]:18} | sched {sc.status_code} test {st.status_code} | QUEUE {qq.status_code} queued={qc} skip={sk} | cid {cid}')
    results.append({'seg': seg, 'cid': cid, 'queued': qc, 'skipped': sk, 'leads': len(ok)})

    # ---- SONDA de breaker apos o 1o segmento ----
    if i == 0:
        if qq.status_code not in (200, 201, 202):
            print(f'\n[{ts()}] !!! QUEUE rejeitado ({qq.status_code}) no 1o segmento. Sched foi {sc.status_code}.')
            print('    Resposta queue:', qq.text[:400])
            if sc.status_code not in (200, 201, 202):
                print('    Resposta schedule:', sc.text[:300])
            print('    ABORTANDO (nada enviado nos demais).')
            aborted = True
            break
        print(f'[{ts()}] sonda: aguardando worker dispatchar {seg}...')
        sent = 0
        for t in range(9):  # ~90s
            time.sleep(10)
            sent, deliv, fail = campaign_sent(cid)
            print(f'   +{(t+1)*10}s  sent={sent} delivered={deliv} failed={fail}')
            if sent > 0:
                break
        if sent == 0:
            print(f'\n[{ts()}] !!! BREAKER AINDA ABERTO — {seg} ficou em 0 enviados apos 90s.')
            print('    ABORTANDO os demais segmentos para nao criar rascunhos mortos.')
            print('    FIX: reiniciar o app DIAX no servidor Kestrel (limpa o breaker em memoria).')
            aborted = True
            break
        print(f'[{ts()}] breaker OK — {seg} dispatchando. Seguindo com os demais.\n')
    if i < len(segs) - 1 and not aborted:
        time.sleep(20)  # espacamento leve entre segmentos

# ---- snapshot final de entrega ----
print('\n=== SNAPSHOT FINAL (aguarda 20s p/ worker) ===')
time.sleep(20)
total_sent = 0
for r in results:
    s, d, fa = campaign_sent(r['cid'])
    r['sent'] = s; r['delivered'] = d; r['failed'] = fa
    total_sent += s
    print(f"  {r['seg']:14} queued={r['queued']} | sent={s} delivered={d} failed={fa}")
print(f'\nTOTAL enviado (sentCount): {total_sent} | aborted={aborted}')

out = {'date': '2026-06-18', 'aborted': aborted, 'total_sent': total_sent, 'segments': results}
json.dump(out, open(DIR + 'result-wave-2026-06-18.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
print('Salvo: result-wave-2026-06-18.json')

# health final
ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})
