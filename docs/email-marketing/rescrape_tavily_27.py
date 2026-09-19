# -*- coding: utf-8 -*-
"""
Re-scrape via Tavily API (sem Selenium) para nichos secos 27/06:
transportadora, restaurante, pet, salao.

Uso:
  python rescrape_tavily_27.py
"""
import os, sys, re, json, time, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from waves_core import load_env, login, base, ts

EMAIL_RE = re.compile(r'[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}')
WEBMAIL = {'gmail.com','hotmail.com','yahoo.com','yahoo.com.br','outlook.com','live.com',
           'bol.com.br','ig.com.br','terra.com.br','uol.com.br','msn.com','icloud.com',
           'protonmail.com','yandex.com','me.com'}

NICHE_QUERIES = [
    ('transportadora', [
        'transportadora frete carga email contato site:com.br',
        'empresa transporte logistica email contato site:com.br',
        'transportadora carga expressa Brasil contato email',
        'frete cargas empresa email site:com.br',
    ], 20),
    ('restaurante', [
        'restaurante delivery email contato site:com.br',
        'restaurante comercial almoço jantar email site:com.br',
        'churrascaria pizzaria sushi email contato Brasil',
        'restaurante gastronomia email reservas contato',
    ], 25),
    ('pet', [
        'pet shop clínica veterinária email contato site:com.br',
        'veterinario banho tosa email contato Brasil site:com.br',
        'clínica veterinária consulta email site:com.br',
    ], 15),
    ('salao', [
        'salão beleza cabeleireiro email contato site:com.br',
        'barbearia salão email contato Brasil',
        'cabeleireiro manicure email agendamento site:com.br',
    ], 15),
]


def tavily_search(api_key, query, max_results=10):
    r = requests.post(
        'https://api.tavily.com/search',
        json={
            'api_key': api_key,
            'query': query,
            'max_results': max_results,
            'search_depth': 'basic',
            'include_raw_content': True,
        },
        timeout=30,
    )
    if r.status_code != 200:
        print(f'   Tavily error {r.status_code}: {r.text[:200]}')
        return []
    return r.json().get('results', [])


def extract_emails_from_results(results):
    found = []
    for res in results:
        url = res.get('url', '')
        domain = url.split('/')[2] if '//' in url else ''
        domain = domain.replace('www.', '')
        text = (res.get('content', '') or '') + ' ' + (res.get('raw_content', '') or '')
        emails = EMAIL_RE.findall(text)
        for email in emails:
            email = email.lower().strip().strip('.,;:')
            dom = email.split('@')[-1] if '@' in email else ''
            if dom in WEBMAIL:
                continue
            if len(dom) < 4 or '.' not in dom:
                continue
            # Extrair nome de empresa do dominio
            name_parts = dom.split('.')[0].replace('-', ' ').title()
            found.append({
                'email': email,
                'domain': dom,
                'name': name_parts,
                'website': f'https://{domain}' if domain else '',
            })
    return found


def import_to_crm(H, leads, niche_key):
    if not leads:
        return 0, 0, 0
    payload = {
        'customers': [
            {
                'name': ld.get('name') or ld['email'].split('@')[0],
                'email': ld['email'],
                'phone': '',
                'companyName': ld.get('name') or '',
                'website': ld.get('website') or '',
                'city': '',
                'notes': f'Scraped via Tavily niche: {niche_key} | rescrape 27/06',
                'tags': niche_key,
            }
            for ld in leads
        ],
        'source': 4,
        'dryRun': False,
    }
    r = requests.post(base + '/api/v1/customers/import', headers=H, json=payload, timeout=60)
    if r.status_code not in (200, 201):
        print(f'   Import FAILED {r.status_code}: {r.text[:200]}')
        return 0, 0, len(leads)
    jr = r.json()
    return jr.get('successCount', 0), jr.get('skippedCount', 0), jr.get('failedCount', 0)


def run():
    load_env()
    api_key = os.environ.get('TAVILY_API_KEY')
    if not api_key:
        print('TAVILY_API_KEY não encontrada')
        return
    H = login()
    print(f'[{ts()}] RESCRAPE TAVILY 27/06 — transportadora/restaurante/pet/salao')
    print()

    seen_emails = set()
    summary = {}

    for niche_key, queries, goal in NICHE_QUERIES:
        print(f'[{ts()}] === {niche_key.upper()} (goal={goal}, {len(queries)} queries) ===')
        all_results = []
        for q in queries:
            results = tavily_search(api_key, q, max_results=10)
            print(f'   query: {q[:60]} -> {len(results)} results')
            all_results.extend(results)
            time.sleep(1)

        raw = extract_emails_from_results(all_results)
        valid = []
        for rec in raw:
            email = rec['email']
            if email in seen_emails:
                continue
            seen_emails.add(email)
            valid.append(rec)

        print(f'[{ts()}]   Emails únicos encontrados: {len(valid)}')

        if not valid:
            summary[niche_key] = {'found': 0, 'imported': 0}
            print(f'[{ts()}]   Nenhum email válido encontrado via Tavily')
            continue

        ok, skip, fail = import_to_crm(H, valid[:goal], niche_key)
        print(f'[{ts()}]   Import: {ok} success, {skip} skip, {fail} fail')
        summary[niche_key] = {'found': len(valid), 'imported': ok, 'skipped': skip, 'failed': fail}

    print()
    print(f'[{ts()}] === RESUMO FINAL ===')
    for niche, s in summary.items():
        print(f'  {niche:15} found={s.get("found",0):4} imported={s.get("imported",0):4}')

    out = 'D:/claude-code/diax-crm/docs/email-marketing/rescrape-tavily-result-2026-06-27.json'
    json.dump(summary, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'[{ts()}] Resultado salvo.')


if __name__ == '__main__':
    run()
