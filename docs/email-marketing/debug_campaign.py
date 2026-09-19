# -*- coding: utf-8 -*-
import os, requests, sys

for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
    line = line.strip()
    if line and not line.startswith('#') and '=' in line:
        k, v = line.split('=', 1); os.environ[k] = v

r = requests.post('https://api.alexandrequeiroz.com.br/api/v1/auth/login',
                  json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']},
                  timeout=20).json()
H = {'Authorization': 'Bearer ' + (r.get('accessToken') or r.get('token')), 'Content-Type': 'application/json'}

tests = [
    ('empresa var', 'Teste1', '<p>Ola {{empresa}}!</p>'),
    ('unsubscribe', 'Teste2', '<a href="{{unsubscribe_url}}">unsub</a>'),
    ('long 4000',   'Teste3', '<p>' + 'A' * 4000 + '</p>'),
    ('long 8000',   'Teste4', '<p>' + 'A' * 8000 + '</p>'),
    ('empresa+unsub+table', 'Teste5',
     '<table width="100%" style="max-width:600px"><tr><td><p>{{empresa}}</p><a href="{{unsubscribe_url}}">x</a></td></tr></table>'),
]

base = 'https://api.alexandrequeiroz.com.br'
for name, subj, html in tests:
    cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                       json={'name': 'DBG - ' + name, 'subject': subj, 'bodyHtml': html})
    print(f'{name:30} status={cr.status_code} id={cr.json().get("id", "ERR")[:8] if cr.status_code == 200 else cr.text[:120]}')

# Now test with the actual build_html output
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing')
import waves_core as ft
html_full = ft.build_html('Test hook', 'https://placehold.co/600x375.jpg', 'https://placehold.co/544x340.jpg', 'Test intro', ['Item 1', 'Item 2', 'Item 3', 'Item 4'], 'Ola teste')
print(f'\nFull template HTML len={len(html_full)}')
# Try halves
cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                   json={'name': 'DBG full template', 'subject': 'Full tpl', 'bodyHtml': html_full})
print(f'Full template status={cr.status_code}: {cr.text[:200]}')
