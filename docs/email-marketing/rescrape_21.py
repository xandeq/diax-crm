# -*- coding: utf-8 -*-
"""
Re-scrape para nichos esgotados: estetica, autoescola, joalheria.
Usa queries alternativas mais genéricas para encontrar leads que a query
anterior não capturou (scraped=0 no run de 20/06).

Salva em scraper-result-2026-06-21.json com contadores acumulados.

Uso:
  python rescrape_21.py
"""
import os, sys, json, time, random, logging
from pathlib import Path

sys.path.insert(0, 'D:/claude-code/diax-crm/scraper-google-email/')
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')

from waves_core import load_env, login, base, ts

logging.basicConfig(level=logging.INFO, format='%(asctime)s %(message)s')
log = logging.getLogger(__name__)

# Queries alternativas — mais genéricas e com variações regionais.
# Para estetica: inclui "spa", "laser", "peeling" além do termo principal.
# Para autoescola: inclui "CFC", "CNH", "habilitacao" separados.
# Para joalheria: adiciona "relojoaria" e "bijuteria" como nichos próximos.
NICHE_QUERIES = [
    ('estetica', [
        'clinica estetica Brasil email contato site:.br',
        'esteticista spa laser peeling Brasil email site:.br',
        'estetica corporal facial Brasil email contato site:.com.br',
        'clinica beleza estetica endereco email site:.br',
    ], 20),
    ('autoescola', [
        'autoescola CFC habilitacao Brasil email contato site:.br',
        'autoescola CNH pratica teorica Brasil email site:.br',
        'escola de motoristas carteira CNH Brasil site:.com.br email',
        'autoescola habilitacao ABC SP RJ MG email contato',
    ], 15),
    ('joalheria', [
        'joalheria relojoaria Brasil email contato site:.br',
        'bijuteria joia ouro prata Brasil email site:.br',
        'joalheria artesanal aliancas Brasil email contato site:.com.br',
        'relojoaria relogio conserto Brasil email site:.br',
    ], 15),
]


def scrape_niche_multi(queries, max_sites_per_query=40):
    """Run multiple Google search queries for a niche, merge results."""
    from selenium import webdriver
    from selenium.webdriver.chrome.options import Options
    from selenium.webdriver.chrome.service import Service
    from webdriver_manager.chrome import ChromeDriverManager
    from scraper.google_search import search_google
    from scraper.crawler import crawl_sites
    from scraper.extractor import extract_emails_and_company

    opts = Options()
    opts.add_argument('--headless=new')
    opts.add_argument('--no-sandbox')
    opts.add_argument('--disable-dev-shm-usage')
    opts.add_argument('--window-size=1280,720')
    opts.add_argument('--disable-blink-features=AutomationControlled')
    opts.add_argument('user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36')
    opts.add_experimental_option('excludeSwitches', ['enable-automation'])
    opts.add_experimental_option('useAutomationExtension', False)

    drv = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=opts)
    drv.set_page_load_timeout(20)
    results = []
    seen_urls = set()

    try:
        for query in queries:
            log.info(f'  Query: {query[:70]}')
            try:
                urls = search_google(drv, query, max_pages=2)
                urls = [u for u in urls if u not in seen_urls][:max_sites_per_query]
                seen_urls.update(urls)
                log.info(f'  Found {len(urls)} new URLs')
                contents = crawl_sites(drv, urls, max_sites_per_query)
                for page in contents:
                    for rec in extract_emails_and_company(page):
                        results.append({
                            'name': rec.company_name or '',
                            'email': rec.email,
                            'phone': rec.phone or '',
                            'city': rec.city or '',
                            'website': page.url,
                        })
                # Pause between queries to avoid rate limit
                time.sleep(random.uniform(8, 15))
            except Exception as e:
                log.error(f'  Query failed: {e}')
                continue
    finally:
        drv.quit()

    return results


def is_valid_corporate_email(email):
    if not email or '@' not in email:
        return False
    email = email.lower().strip()
    dom = email.split('@', 1)[1]
    FOREIGN = ['.co.uk', '.co.in', '.ca', '.com.au', '.de', '.fr', '.it', '.nl',
               '.se', '.no', '.dk', '.pl', '.ru', '.ua', '.cn', '.jp', '.kr']
    if any(dom.endswith(t) for t in FOREIGN):
        return False
    WEBMAIL = {'gmail.com', 'hotmail.com', 'yahoo.com', 'outlook.com', 'live.com',
               'bol.com.br', 'ig.com.br', 'terra.com.br', 'uol.com.br', 'msn.com',
               'icloud.com', 'me.com', 'protonmail.com', 'yandex.com'}
    if dom in WEBMAIL:
        return False
    return True


def import_to_crm(H, leads, niche_key):
    if not leads:
        return 0, 0, 0
    payload = {
        'customers': [
            {
                'name': ld.get('name') or ld['email'].split('@')[0],
                'email': ld['email'],
                'phone': ld.get('phone') or '',
                'companyName': ld.get('name') or '',
                'website': ld.get('website') or '',
                'city': ld.get('city') or '',
                'notes': f'Scraped niche: {niche_key} | rescrape 21/06',
                'tags': niche_key,
            }
            for ld in leads
        ],
        'source': 4,
        'dryRun': False,
    }
    r = requests.post(base + '/api/v1/customers/import', headers=H, json=payload, timeout=60)
    if r.status_code not in (200, 201):
        log.error(f'  Import FAILED {r.status_code}: {r.text[:300]}')
        return 0, 0, len(leads)
    jr = r.json()
    return jr.get('successCount', 0), jr.get('skippedCount', 0), jr.get('failedCount', 0)


import requests

def run():
    load_env()
    H = login()
    print(f'[{ts()}] RE-SCRAPE 21/06 — nichos estetica/autoescola/joalheria')
    print(f'[{ts()}] Objetivo: reabastecer CRM com novos leads corporativos')
    print()

    seen_emails = set()
    summary = {}

    for niche_key, queries, goal in NICHE_QUERIES:
        print(f'[{ts()}] === {niche_key.upper()} (goal={goal}, {len(queries)} queries) ===')

        try:
            raw = scrape_niche_multi(queries, max_sites_per_query=40)
        except Exception as e:
            log.error(f'  Scrape FAILED: {e}')
            summary[niche_key] = {'scraped': 0, 'imported': 0, 'error': str(e)}
            continue

        valid = []
        for rec in raw:
            email = rec.get('email', '').lower().strip()
            if not email or email in seen_emails:
                continue
            if not is_valid_corporate_email(email):
                continue
            seen_emails.add(email)
            valid.append(rec)

        print(f'[{ts()}]   Raw: {len(raw)} | Valid corporate: {len(valid)}')

        if not valid:
            summary[niche_key] = {'scraped': 0, 'imported': 0}
            print(f'[{ts()}]   Nenhum lead válido — nichos talvez precise de VPN ou horário diferente')
            continue

        ok, skip, fail = import_to_crm(H, valid, niche_key)
        print(f'[{ts()}]   Import: {ok} success, {skip} skip, {fail} fail')
        summary[niche_key] = {'scraped': len(valid), 'imported': ok, 'skipped': skip, 'failed': fail}

        if (list(NICHE_QUERIES).index((niche_key, queries, goal)) + 1) < len(NICHE_QUERIES):
            delay = random.uniform(20, 40)
            print(f'[{ts()}]   Aguardando {delay:.0f}s antes do próximo niche...')
            time.sleep(delay)

    print()
    print(f'[{ts()}] === RESUMO FINAL ===')
    for niche, s in summary.items():
        imp = s.get('imported', 0)
        print(f'  {niche:15} scraped={s.get("scraped",0):4} imported={imp:4} skip={s.get("skipped",0):3} fail={s.get("failed",0):3}')

    out = 'D:/claude-code/diax-crm/docs/email-marketing/scraper-result-2026-06-21.json'
    json.dump(summary, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'[{ts()}] Resultado salvo em scraper-result-2026-06-21.json')
    print(f'[{ts()}] Se ainda scraped=0 após re-scrape, tentar no horário noturno (22h-06h) ou com VPN BR.')


if __name__ == '__main__':
    run()
