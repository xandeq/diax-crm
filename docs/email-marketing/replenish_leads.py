# -*- coding: utf-8 -*-
"""REPOSIÇÃO AUTOMÁTICA de leads via Tavily (sem Selenium, sem serviço pago de scraping).
Busca empresas por nicho com foco Grande Vitória/ES + Brasil, extrai emails corporativos
do raw content, filtra webmail/estrangeiro, deduplica contra o CRM e importa via
/api/v1/customers/import (source=4 Scraping, tags='tavily,{nicho}').

Uso:  python replenish_leads.py [--cap N]  (default 120 leads novos por rodada)
Chamado automaticamente pelo daily_waves quando o estoque de sexta < 120.
"""
import os, sys, re, json, time, datetime, requests

sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import load_env, login, base, ts

EMAIL_RE = re.compile(r'[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}')
WEBMAIL = {'gmail.com','hotmail.com','yahoo.com','yahoo.com.br','outlook.com','live.com',
           'bol.com.br','ig.com.br','terra.com.br','uol.com.br','msn.com','icloud.com',
           'protonmail.com','yandex.com','me.com'}
BAD_LOCAL = ('noreply','no-reply','nao-responda','naoresponda','mailer-daemon','postmaster','abuse','webmaster@wix')
FOREIGN_TLD = ('.ar','.cl','.co','.mx','.pt','.es','.us','.uk','.de','.fr','.it')

# nichos que a máquina de waves consome (rotação) — queries ES-first
NICHE_QUERIES = [
    ('clinica',      ['clinica medica Vitoria ES email contato', 'clinica especializada Vila Velha Serra email contato site:com.br']),
    ('loja',         ['loja comercio Vitoria ES email contato site:com.br', 'boutique loja Vila Velha email contato']),
    ('consultoria',  ['consultoria empresarial Vitoria ES email contato', 'assessoria empresas Espirito Santo email site:com.br']),
    ('restaurante',  ['restaurante Vitoria ES email contato site:com.br', 'restaurante Vila Velha Serra email reservas']),
    ('construtora',  ['construtora Vitoria ES email contato site:com.br', 'construtora incorporadora Espirito Santo email']),
    ('advocacia',    ['escritorio advocacia Vitoria ES email contato site:com.br']),
    ('engenharia',   ['empresa engenharia Vitoria ES email contato site:com.br']),
    ('academia',     ['academia crossfit Vitoria Vila Velha email contato site:com.br']),
    ('logistica',    ['transportadora logistica Espirito Santo email contato site:com.br']),
    ('arquitetura',  ['escritorio arquitetura Vitoria ES email contato site:com.br']),
    ('moveis',       ['moveis planejados marcenaria Vitoria ES email contato site:com.br']),
    ('otica',        ['otica Vitoria Vila Velha Serra email contato site:com.br']),
    ('pousada',      ['pousada hotel Espirito Santo email reservas contato site:com.br']),
    ('estudio',      ['estudio pilates yoga Vitoria ES email contato site:com.br']),
    ('medico',       ['consultorio medico especialista Vitoria ES email contato site:com.br']),
    ('escola',       ['escola particular colegio Vitoria ES email contato site:com.br']),
    ('turismo',      ['agencia turismo viagens Vitoria ES email contato site:com.br']),
    ('informatica',  ['empresa informatica TI Vitoria ES email contato site:com.br']),
]


def tavily_search(api_key, query, max_results=10):
    try:
        r = requests.post('https://api.tavily.com/search', timeout=40,
                          json={'api_key': api_key, 'query': query, 'max_results': max_results,
                                'search_depth': 'basic', 'include_raw_content': True})
        if r.status_code != 200:
            print(f'   tavily {r.status_code}: {r.text[:120]}'); return []
        return r.json().get('results', [])
    except Exception as e:
        print(f'   tavily err: {e}'); return []


_MX_CACHE = {}

def domain_has_mx(dom):
    """Valida MX do domínio (cache) — corta bounce antes de entrar no CRM (meta <4%)."""
    if dom in _MX_CACHE: return _MX_CACHE[dom]
    ok = False
    try:
        import dns.resolver
        ok = len(dns.resolver.resolve(dom, 'MX', lifetime=5)) > 0
    except ImportError:
        # sem dnspython: fallback nslookup
        import subprocess
        try:
            out = subprocess.run(['nslookup', '-type=MX', dom], capture_output=True, text=True, timeout=8).stdout
            ok = 'mail exchanger' in out.lower()
        except Exception:
            ok = True  # na dúvida não bloqueia
    except Exception:
        ok = False
    _MX_CACHE[dom] = ok
    return ok


def email_ok(email):
    email = email.lower().strip().rstrip('.')
    if any(b in email for b in BAD_LOCAL): return None
    dom = email.split('@', 1)[1]
    if dom in WEBMAIL: return None
    if any(dom.endswith(t) for t in FOREIGN_TLD): return None
    if not (dom.endswith('.br') or dom.endswith('.com') or dom.endswith('.net')): return None
    if re.search(r'\.(png|jpg|jpeg|gif|webp|css|js)$', email): return None
    if not domain_has_mx(dom): return None
    return email


def existing_emails(H):
    """Emails já no CRM (paginado) — dedupe antes de importar."""
    seen = set(); page = 1
    while page <= 60:
        r = requests.get(base + '/api/v1/leads', params={'page': page, 'pageSize': 200}, headers=H, timeout=30).json()
        items = r.get('items', [])
        if not items: break
        for it in items:
            em = (it.get('email') or '').lower().strip()
            if em: seen.add(em)
        if not r.get('hasNextPage'): break
        page += 1
    return seen


def run(cap=120):
    load_env(); H = login()
    key = os.environ.get('TAVILY_API_KEY')
    if not key:
        print('sem TAVILY_API_KEY'); return {}
    print(f'[{ts()}] REPLENISH via Tavily — cap {cap}')
    seen = existing_emails(H)
    print(f'[{ts()}] emails já no CRM: {len(seen)}')
    summary = {}
    total_new = 0
    for niche, queries in NICHE_QUERIES:
        if total_new >= cap: break
        found = {}
        for q in queries:
            if total_new + len(found) >= cap: break
            for res in tavily_search(key, q, 10):
                raw = (res.get('raw_content') or '') + ' ' + (res.get('content') or '')
                url = res.get('url') or ''
                title = (res.get('title') or '').strip()[:120]
                for m in set(EMAIL_RE.findall(raw)):
                    em = email_ok(m)
                    if not em or em in seen or em in found: continue
                    found[em] = {'email': em, 'name': title or em.split('@')[0], 'website': url, 'query': q}
            time.sleep(1)
        if not found:
            print(f'[{ts()}] {niche:13} 0 novos'); continue
        leads = list(found.values())[:max(0, cap - total_new)]
        payload = {'customers': [{
            'name': ld['name'], 'email': ld['email'], 'phone': '', 'companyName': ld['name'],
            'website': ld['website'], 'city': '',
            'notes': f'Tavily replenish {datetime.date.today().isoformat()} | {ld["query"]}',
            'tags': f'tavily,{niche}',
        } for ld in leads], 'source': 4, 'dryRun': False}
        r = requests.post(base + '/api/v1/customers/import', headers=H, json=payload, timeout=90)
        jr = r.json() if r.status_code in (200, 201) else {}
        ok = jr.get('successCount', 0)
        total_new += ok
        for ld in leads: seen.add(ld['email'])
        summary[niche] = ok
        print(f'[{ts()}] {niche:13} +{ok} importados (skip {jr.get("skippedCount", 0)})')
    print(f'[{ts()}] TOTAL NOVOS: {total_new}')
    return summary


if __name__ == '__main__':
    cap = 120
    if '--cap' in sys.argv:
        cap = int(sys.argv[sys.argv.index('--cap') + 1])
    run(cap)
