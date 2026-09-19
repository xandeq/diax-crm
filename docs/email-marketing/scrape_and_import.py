# -*- coding: utf-8 -*-
"""
Scraper + Importer para reabastecer CRM com leads por nicho.
Usa Selenium + Google Search para buscar emails de empresas brasileiras.
Importa direto via /api/v1/customers/import do DIAX CRM.

Nichos alvo: academia, imobiliaria, medico, estetica, salao, joalheria,
             odontologia, farmacia, autoescola + novos: advogado, engenharia
"""
import os, sys, json, time, re, csv, io, requests, random, logging
from pathlib import Path

sys.path.insert(0, 'D:/claude-code/diax-crm/scraper-google-email/')
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')

from waves_core import load_env, login, base, ts

logging.basicConfig(level=logging.INFO, format='%(asctime)s %(message)s')
log = logging.getLogger(__name__)

# Queries por nicho — keyword no nome da query garante que a maioria dos
# resultados terá o termo no nome da empresa/site.
NICHE_QUERIES = [
    # (niche_search_key, google_query, min_leads_goal)
    ('academia',    'academia ginastica crossfit fitness Brasil email contato site:.br OR site:.com.br', 30),
    ('medico',      'medico clinica consultorio especialista Brasil email contato site:.br OR site:.com.br', 30),
    ('imobiliaria', 'imobiliaria corretor imoveis Brasil email contato site:.br OR site:.com.br', 25),
    ('estetica',    'clinica estetica esteticista beleza Brasil email contato site:.br OR site:.com.br', 20),
    ('salao',       'salao beleza cabelereiro manicure Brasil email contato site:.br OR site:.com.br', 20),
    ('odontologia', 'dentista odontologia clinica odontologica Brasil email contato site:.br OR site:.com.br', 20),
    ('farmacia',    'farmacia drogaria manipulacao Brasil email contato site:.br OR site:.com.br', 20),
    ('autoescola',  'autoescola CFC habilitacao Brasil email contato site:.br OR site:.com.br', 15),
    ('joalheria',   'joalheria relojoaria bijuteria Brasil email contato site:.br OR site:.com.br', 15),
]


def scrape_niche(query, max_sites=60):
    """Run Google search + crawl sites for emails. Returns list of dicts."""
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
    try:
        urls = search_google(drv, query, max_pages=3)
        log.info(f'  Found {len(urls)} URLs from Google')
        urls = urls[:max_sites]
        contents = crawl_sites(drv, urls, max_sites)
        for page in contents:
            for rec in extract_emails_and_company(page):
                results.append({
                    'name': rec.company_name or '',
                    'email': rec.email,
                    'phone': rec.phone or '',
                    'city': rec.city or '',
                    'website': page.url,
                })
    finally:
        drv.quit()
    return results


def is_valid_corporate_email(email):
    """Validates email is corporate (not webmail, not foreign, has @)."""
    if not email or '@' not in email:
        return False
    email = email.lower().strip()
    dom = email.split('@', 1)[1]
    # Foreign TLD filter
    FOREIGN = ['.co.uk', '.co.in', '.ca', '.com.au', '.de', '.fr', '.it', '.nl',
               '.se', '.no', '.dk', '.pl', '.ru', '.ua', '.cn', '.jp', '.kr']
    if any(dom.endswith(t) for t in FOREIGN):
        return False
    # Webmail filter
    WEBMAIL = {'gmail.com', 'hotmail.com', 'yahoo.com', 'outlook.com', 'live.com',
               'bol.com.br', 'ig.com.br', 'terra.com.br', 'uol.com.br', 'msn.com',
               'icloud.com', 'me.com', 'protonmail.com', 'yandex.com'}
    if dom in WEBMAIL:
        return False
    return True


def import_to_crm(H, leads, niche_key):
    """Import leads to CRM via /api/v1/customers/import."""
    if not leads:
        return 0, 0, 0
    payload = {
        'customers': [
            {
                'name': ld.get('name') or ld.get('email').split('@')[0],
                'email': ld['email'],
                'phone': ld.get('phone') or '',
                'companyName': ld.get('name') or '',
                'website': ld.get('website') or '',
                'city': ld.get('city') or '',
                'notes': f'Scraped niche: {niche_key} | query: {ld.get("query", "")}',
                'tags': niche_key,
            }
            for ld in leads
        ],
        'source': 4,  # Scraping
        'dryRun': False,
    }
    r = requests.post(base + '/api/v1/customers/import', headers=H, json=payload, timeout=60)
    if r.status_code not in (200, 201):
        log.error(f'  Import FAILED {r.status_code}: {r.text[:300]}')
        return 0, 0, len(leads)
    jr = r.json()
    return jr.get('successCount', 0), jr.get('skippedCount', 0), jr.get('failedCount', 0)


def run():
    load_env()
    H = login()
    print(f'[{ts()}] SCRAPER+IMPORTER INICIADO')
    print(f'[{ts()}] {len(NICHE_QUERIES)} nichos alvos')
    print()

    seen_emails = set()
    summary = {}

    for niche_key, query, goal in NICHE_QUERIES:
        print(f'[{ts()}] === {niche_key.upper()} (goal={goal}) ===')
        print(f'[{ts()}]   Query: {query[:80]}')

        try:
            raw = scrape_niche(query, max_sites=min(goal * 3, 100))
        except Exception as e:
            log.error(f'  Scrape FAILED: {e}')
            summary[niche_key] = {'scraped': 0, 'imported': 0, 'error': str(e)}
            continue

        # Filter: valid corporate email, not seen, company name if possible
        valid = []
        for rec in raw:
            email = rec.get('email', '').lower().strip()
            if not email or email in seen_emails:
                continue
            if not is_valid_corporate_email(email):
                continue
            seen_emails.add(email)
            rec['query'] = query
            valid.append(rec)

        print(f'[{ts()}]   Raw: {len(raw)} | Valid corporate: {len(valid)}')

        if not valid:
            summary[niche_key] = {'scraped': 0, 'imported': 0}
            print(f'[{ts()}]   Nenhum lead válido — pulando')
            continue

        ok, skip, fail = import_to_crm(H, valid, niche_key)
        print(f'[{ts()}]   Import: {ok} success, {skip} skip, {fail} fail')
        summary[niche_key] = {'scraped': len(valid), 'imported': ok, 'skipped': skip, 'failed': fail}

        # Small delay between niches to avoid Google rate limit
        if (NICHE_QUERIES.index((niche_key, query, goal)) + 1) < len(NICHE_QUERIES):
            delay = random.uniform(15, 30)
            print(f'[{ts()}]   Aguardando {delay:.0f}s antes do próximo niche...')
            time.sleep(delay)

    print()
    print(f'[{ts()}] === RESUMO FINAL ===')
    total_imported = 0
    for niche, s in summary.items():
        imp = s.get('imported', 0)
        total_imported += imp
        print(f'  {niche:15} scraped={s.get("scraped",0):4} imported={imp:4} skip={s.get("skipped",0):3} fail={s.get("failed",0):3}')
    print(f'\n  TOTAL IMPORTADO: {total_imported}')
    print(f'  Novo inventário disponível para próximas waves!')

    # Save summary
    json.dump(summary, open('scraper-result-2026-06-20.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'[{ts()}] Resultado salvo em scraper-result-2026-06-20.json')


if __name__ == '__main__':
    run()
