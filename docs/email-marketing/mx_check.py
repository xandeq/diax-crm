# -*- coding: utf-8 -*-
"""Verificação de entregabilidade ANTES de enfileirar: domínio lixo/placeholder e MX.

Bounce semana 04/09: 10/133 (7,5%) — inclui instagram.local e wixpress.com que passaram
pelos filtros. Bounce > 5% derruba reputação do domínio com Gmail/Outlook.
MX via nslookup (Windows, sem dependência), cache 30d em previews/mx-cache.json.
"""
import re, json, os, subprocess, datetime as dt

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
CACHE_FILE = DIR + 'previews/mx-cache.json'
CACHE_DAYS = 30

JUNK_SUFFIXES = ('.local', '.localhost', '.invalid', '.test', '.example', '.internal', '.lan')
JUNK_HOSTS = {'localhost', 'example.com', 'example.com.br', 'email.com', 'site.com.br', 'site.com', 'test.com',
              'teste.com', 'teste.com.br', 'dominio.com', 'dominio.com.br', 'seusite.com.br', 'empresa.com.br',
              'google_maps', 'google.com', 'gmail.con', 'hotmail.con', 'wixpress.com', 'sentry.io', 'sentry-next.wixpress.com',
              'wordpress.com', 'wix.com', 'squarespace.com', 'godaddy.com', 'hostgator.com.br', 'mailinator.com'}
JUNK_PARTS = ('wixpress', 'sentry', 'placeholder', 'noemail', 'sememail', 'nomail')


def is_junk_domain(domain):
    d = (domain or '').lower().strip()
    if not d or '.' not in d and d != 'localhost' and d != 'google_maps':
        return True
    if d in JUNK_HOSTS or d.endswith(JUNK_SUFFIXES):
        return True
    return any(p in d for p in JUNK_PARTS)


def parse_nslookup_mx(out):
    hosts = re.findall(r'mail exchanger\s*=\s*([^\s]+)', out or '')
    return [h.rstrip('.') for h in hosts if h.rstrip('.')]          # Null MX "." => vazio


def resolve_mx(domain):
    try:
        out = subprocess.run(['nslookup', '-type=MX', domain], capture_output=True, text=True, timeout=8).stdout
    except Exception:
        return []
    mx = parse_nslookup_mx(out)
    if mx:
        return mx
    # sem MX: RFC 5321 usa o A do domínio — aceita se resolver (evita falso negativo)
    try:
        out = subprocess.run(['nslookup', domain], capture_output=True, text=True, timeout=8).stdout
        return ['A'] if re.search(r'Address(es)?:\s*\d+\.\d+\.\d+\.\d+', out.split('\n\n', 1)[-1]) else []
    except Exception:
        return []


def deliverable_domain(domain, cache, resolver=resolve_mx):
    d = (domain or '').lower().strip()
    if is_junk_domain(d):
        return False
    ent = cache.get(d)
    if ent and (dt.date.today() - dt.date.fromisoformat(ent['at'])).days < CACHE_DAYS:
        return bool(ent['ok'])
    ok = bool(resolver(d))
    cache[d] = {'at': dt.date.today().isoformat(), 'ok': ok}
    return ok


def load_cache():
    try: return json.load(open(CACHE_FILE, encoding='utf-8'))
    except Exception: return {}


def save_cache(c):
    os.makedirs(os.path.dirname(CACHE_FILE), exist_ok=True)
    json.dump(c, open(CACHE_FILE, 'w', encoding='utf-8'), indent=0)


if __name__ == '__main__':
    import sys
    c = load_cache()
    for d in sys.argv[1:]:
        print(d, '->', deliverable_domain(d, c))
    save_cache(c)
