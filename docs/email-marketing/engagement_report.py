# -*- coding: utf-8 -*-
"""Relatorio de engajamento (abertura/clique) da fonte de verdade = Brevo.
Roda como passo pos-envio do loop autonomo. Agrupa por campanha (tag=campaignId),
filtra send-tests internos, e destaca quem CLICOU (lead quente p/ follow-up).
Uso: python engagement_report.py [dias]   (default 2)
"""
import os, sys, json, datetime, requests
from collections import defaultdict
for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
    line = line.strip()
    if line and not line.startswith('#') and '=' in line:
        k, v = line.split('=', 1); os.environ[k] = v

base = 'https://api.alexandrequeiroz.com.br'
BH = {'api-key': os.environ['BREVO_API_KEY'], 'accept': 'application/json'}
INTERNAL = ('admin@alexandrequeiroz', 'xandeq@gmail', 'xandeq+')
days = int(sys.argv[1]) if len(sys.argv) > 1 else 2

def crm_h():
    r = requests.post(base + '/api/v1/auth/login', json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']}, timeout=20).json()
    return {'Authorization': 'Bearer ' + (r.get('accessToken') or r.get('token'))}

def fetch(ev):
    out, off = [], 0
    while True:
        u = f'https://api.brevo.com/v3/smtp/statistics/events?limit=100&offset={off}&days={days}&event={ev}'
        d = requests.get(u, headers=BH, timeout=25)
        if d.status_code != 200:
            break
        e = d.json().get('events', [])
        if not e:
            break
        out += e; off += 100
        if len(e) < 100 or off >= 1000:
            break
    return out

H = crm_h()
camps = {}
cl = requests.get(base + '/api/v1/email-campaigns/campaigns?pageSize=80', headers=H, timeout=20).json()
for c in (cl.get('items', cl) if isinstance(cl, dict) else cl):
    camps[c.get('id')] = c.get('name')

by = defaultdict(lambda: {'opens': set(), 'clicks': set()})
for e in fetch('opened'):
    if e.get('tag'): by[e['tag']]['opens'].add(e.get('email'))
for e in (fetch('clicks') or fetch('click')):
    if e.get('tag'): by[e['tag']]['clicks'].add(e.get('email'))

real = lambda emails: [x for x in emails if x and not any(i in x for i in INTERNAL)]
report = {'generated': datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).isoformat(), 'days': days, 'campaigns': [], 'hot_leads': []}
print(f'=== ENGAJAMENTO (Brevo, {days}d) — prospects reais ===')
tot_open, tot_click = set(), set()
for t, v in sorted(by.items(), key=lambda x: -len(x[1]['opens'])):
    ro, rc = real(v['opens']), real(v['clicks'])
    if not ro and not rc:
        continue
    tot_open |= set(ro); tot_click |= set(rc)
    nm = camps.get(t, t[:8])
    report['campaigns'].append({'campaign': nm, 'opened': ro, 'clicked': rc})
    for c in rc:
        report['hot_leads'].append({'email': c, 'campaign': nm})
    flag = '  CLICOU: ' + ', '.join(rc) if rc else ''
    print(f"  {str(nm)[:44]:44} aberturas={len(ro)} cliques={len(rc)}{flag}")
print(f'\nTOTAL: {len(tot_open)} prospects abriram, {len(tot_click)} clicaram (leads quentes).')
if tot_click:
    print('LEADS QUENTES (abriram E clicaram):', ', '.join(sorted(tot_click)))
json.dump(report, open('D:/claude-code/diax-crm/docs/email-marketing/engagement-report.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
print('salvo: engagement-report.json')
