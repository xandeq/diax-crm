# -*- coding: utf-8 -*-
"""PONTE MANUAL extrator->CRM — fallback até o ExtractorPullWorker (.NET) estar em prod.

Enquanto o worker não roda em prod (precisa: deploy do CRM da branch chore/email-automation-versioned
+ EXTRATOR_SERVICE_TOKEN no host do extrator + DIAX_ExtractorPull__Enabled=true), este script
mantém o CRM abastecido: puxa leads recém-scrapeados do Postgres do extrator (via SSH read),
aplica o filtro de qualidade (mesma lógica do ExtractorIntegrationService da branch) e importa
via POST /api/v1/customers/import (source=4=Scraping). Dedup por email é frouxo neste endpoint
para source=Scraping — rode no máx 1x/dia e confie no crm_status do extrator (só pega 'novo').

Uso:
  python import_extrator_bridge.py --dry        # só mostra o que importaria
  python import_extrator_bridge.py              # importa de fato
  python import_extrator_bridge.py --days 3     # janela de extração (default 5)

Requer: SSH key vps_hostinger_ed25519, secrets locais (DIAX_ADMIN_*), fire_today_20 (login/base).
"""
import sys, subprocess, datetime
sys.path.insert(0, r'D:\claude-code\diax-crm\docs\email-marketing')
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import requests
from fire_today_20 import load_env, login, base

DRY = '--dry' in sys.argv
DAYS = int(sys.argv[sys.argv.index('--days') + 1]) if '--days' in sys.argv else 5

SSH = ['ssh', '-i', '/c/Users/acq20/.ssh/vps_hostinger_ed25519', '-p', '2282',
       '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=20', 'root@185.173.110.180']
SQL = (f"select email, company_name, city, state, phone, website, coalesce(category,''), source "
       f"from leads where extracted_at > now() - interval '{DAYS} days' "
       f"and email is not null and email <> '' and crm_status = 'novo';")
DOCKER = f'docker exec extrator-postgres psql -U extrator -d extrator -t -A -F"|" -c "{SQL}"'

BLOCKED_DOMAINS = {'sun.com','blok.ai','overchat.ai','redcross.org','fox.com','foxtv.com',
                   'totalshape.com','marketchameleon.com','scaler.com','soundtransit.org','futurenet.com'}
BLOCKED_TLD = ('.es','.fi','.eu','.ar','.cl','.mx','.pt','.co.uk','.ca','.com.au','.de','.fr',
               '.it','.nl','.se','.no','.dk','.pl','.ru','.ua','.cn','.jp','.kr','.us','.uk')

def clean(email):
    if not email or '@' not in email: return None
    e = email.strip().lower()
    if '%' in e: return None
    dom = e.split('@', 1)[1]
    if dom in BLOCKED_DOMAINS: return None
    if any(dom.endswith(t) for t in BLOCKED_TLD): return None
    return e

def fetch_from_extrator():
    out = subprocess.run(SSH + [DOCKER], capture_output=True, text=True, timeout=60)
    if out.returncode != 0:
        print('SSH/extrator falhou:', out.stderr[:300]); sys.exit(1)
    return out.stdout.splitlines()

def build(lines):
    rows, rejected, seen = [], 0, set()
    for line in lines:
        p = line.rstrip('\n').split('|')
        if len(p) < 8: continue
        email, company, city, state, phone, website, category, _ = p[:8]
        ce = clean(email)
        if not ce or ce in seen:
            rejected += ce is None; continue
        seen.add(ce)
        rows.append({'name': company.strip() or ce.split('@')[0], 'email': ce,
                     'phone': phone.strip(), 'companyName': company.strip(),
                     'website': website.strip(), 'city': city.strip(),
                     'notes': f'Extrator import-bridge {datetime.date.today().isoformat()} | {city.strip()}/{state.strip()} | {category.strip()}',
                     'tags': (category.strip() or 'extrator')[:40]})
    return rows, rejected

def main():
    lines = fetch_from_extrator()
    rows, rejected = build(lines)
    print(f'extrator: {len(lines)} novos | limpos: {len(rows)} | rejeitados(lixo): {rejected}')
    if DRY:
        for r in rows[:15]: print('  ', r['email'], '|', r['name'][:40], '|', r['city'])
        return
    load_env(); H = login()
    ok = skip = fail = 0
    for i in range(0, len(rows), 100):
        chunk = rows[i:i+100]
        r = requests.post(base + '/api/v1/customers/import', headers=H,
                          json={'customers': chunk, 'source': 4, 'dryRun': False}, timeout=90)
        if r.status_code not in (200, 201):
            print(f'  batch {i}: FAIL {r.status_code} {r.text[:200]}'); fail += len(chunk); continue
        jr = r.json()
        ok += jr.get('successCount', 0); skip += jr.get('skippedCount', 0); fail += jr.get('failedCount', 0)
    print(f'importado: novos~{ok} skip={skip} fail(ja-existiam/invalidos)={fail}')

if __name__ == '__main__':
    main()
