# -*- coding: utf-8 -*-
"""Diagnóstico rápido e HONESTO do site de um lead — combustível de personalização.

Nível 3 da skill cold-email: a observação tem que vir do mundo do leitor e levar ao
problema que o serviço resolve. Cada achado é um fato verificado no site (HTTPS,
celular, WhatsApp, velocidade, rodapé antigo, título/descrição) — nunca inventado.

Uso: from site_check import check_site, check_many, summarize_wave, summarize_fup
     python site_check.py https://exemplo.com.br   (debug: imprime achados)
Cache: previews/site-checks.json (por host, 30 dias) — não re-testar todo dia.
"""
import re, json, os, sys, time, datetime as dt, concurrent.futures as cf
from collections import namedtuple
from urllib.parse import urlparse

Finding = namedtuple('Finding', 'code sev extra')   # sev: maior = mais grave

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
CACHE_FILE = DIR + 'previews/site-checks.json'
CACHE_DAYS = 30
TIMEOUT = 8

# Páginas de terceiros que o extrator às vezes grava como "website" do lead.
DIRECTORY_HOSTS = ('econodata', 'cliniguia', 'facebook.', 'instagram.', 'linkedin.', 'google.', 'goo.gl',
                   'apontador', 'guiamais', 'telelistas', 'solutudo', 'cnpj.biz', 'cnpj.info', 'consultacnpj',
                   'empresascnpj', 'casadosdados', 'yelp.', 'tripadvisor', 'ifood', 'doctoralia', 'boaconsulta',
                   'jusbrasil', 'olx.', 'mercadolivre', 'shopee', 'wa.me', 'whatsapp.', 'bit.ly', 'linktr.ee',
                   'youtube.', 'tiktok.', 'kekanto', 'hotmart', 'lojaintegrada', 'wixsite', 'site123',
                   'negocio.site', 'business.site', 'nuvemshop', 'blogspot', 'wordpress.com')

SEVERITY = {'down': 100, 'ssl_invalid': 98, 'none': 96, 'directory': 95, 'no_https': 90,
            'no_viewport': 80, 'slow': 70, 'no_whatsapp': 60, 'old_copyright': 50,
            'no_meta_desc': 40, 'short_title': 30}

GENERIC_TITLES = {'', 'home', 'início', 'inicio', 'página inicial', 'pagina inicial', 'index', 'site',
                  'bem-vindo', 'bem vindo', 'welcome', 'untitled', 'sem título', 'nova página', 'documento'}


def classify_url(url):
    u = (url or '').strip().lower()
    if not u or u in ('-', 'n/a', 'null', 'none'):
        return 'none'
    host = urlparse(u if '://' in u else 'http://' + u).netloc
    if not host or '.' not in host:
        return 'none'
    return 'directory' if any(d in host for d in DIRECTORY_HOSTS) else 'site'


def _host(url):
    return urlparse(url if '://' in url else 'http://' + url).netloc.lower().replace('www.', '')


def analyze(html, final_url, status, elapsed, nbytes, ssl_invalid=False):
    """Lista de Finding ordenada por gravidade. Puro (sem rede) — testável."""
    fs = []
    if ssl_invalid:
        fs.append(Finding('ssl_invalid', SEVERITY['ssl_invalid'], ''))
    if not (200 <= (status or 0) < 400):
        fs.append(Finding('down', SEVERITY['down'], str(status or 'sem resposta')))
        return sorted(fs, key=lambda f: -f.sev)
    if final_url.lower().startswith('http://'):
        fs.append(Finding('no_https', SEVERITY['no_https'], ''))
    h = html or ''
    hl = h.lower()
    if 'name="viewport"' not in hl and "name='viewport'" not in hl:
        fs.append(Finding('no_viewport', SEVERITY['no_viewport'], ''))
    if elapsed is not None and elapsed > 3.0:
        fs.append(Finding('slow', SEVERITY['slow'], f'{elapsed:.1f}'))
    if not re.search(r'wa\.me/|api\.whatsapp\.com|whatsapp://|web\.whatsapp\.com|whatsapp\.com/send', hl):
        fs.append(Finding('no_whatsapp', SEVERITY['no_whatsapp'], ''))
    years = [int(y) for y in re.findall(r'(?:©|&copy;|copyright)\s*(?:\d{4}\s*[-–]\s*)?((?:19|20)\d{2})', hl)]
    if years:
        y = max(years)
        if y <= dt.date.today().year - 3:   # 3+ anos: 'parado' vira afirmação honesta
            fs.append(Finding('old_copyright', SEVERITY['old_copyright'], str(y)))
    if not re.search(r'<meta[^>]+name=["\']description["\'][^>]+content=["\'][^"\']{20,}', hl):
        fs.append(Finding('no_meta_desc', SEVERITY['no_meta_desc'], ''))
    m = re.search(r'<title[^>]*>(.*?)</title>', h, re.I | re.S)
    title = re.sub(r'\s+', ' ', m.group(1)).strip() if m else ''
    # Sem <title> pode ser SPA renderizada por JS — não afirmar nada. Só título genérico/curto.
    if title and (title.lower() in GENERIC_TITLES or len(title) < 12):
        fs.append(Finding('short_title', SEVERITY['short_title'], title))
    return sorted(fs, key=lambda f: -f.sev)


def fetch_and_analyze(url):
    """Rede: GET com timeout; SSL inválido -> flag + re-tenta sem verificar p/ analisar o HTML."""
    import requests, urllib3
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
    u = url if '://' in url else 'https://' + url
    hdr = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128 Safari/537.36',
           'Accept-Language': 'pt-BR,pt;q=0.9'}
    ssl_bad = False
    t0 = time.time()
    try:
        r = requests.get(u, headers=hdr, timeout=TIMEOUT, allow_redirects=True)
    except requests.exceptions.SSLError:
        ssl_bad = True
        try:
            r = requests.get(u, headers=hdr, timeout=TIMEOUT, allow_redirects=True, verify=False)
        except Exception:
            return [Finding('down', SEVERITY['down'], 'ssl')]
    except requests.exceptions.RequestException:
        if u.startswith('https://'):   # talvez só http exista
            try:
                r = requests.get('http://' + u[8:], headers=hdr, timeout=TIMEOUT, allow_redirects=True)
            except Exception:
                return [Finding('down', SEVERITY['down'], 'sem resposta')]
        else:
            return [Finding('down', SEVERITY['down'], 'sem resposta')]
    elapsed = time.time() - t0
    return analyze(r.text[:400_000], r.url, r.status_code, elapsed, len(r.content), ssl_invalid=ssl_bad)


def _load_cache():
    try: return json.load(open(CACHE_FILE, encoding='utf-8'))
    except Exception: return {}


def _save_cache(c):
    os.makedirs(os.path.dirname(CACHE_FILE), exist_ok=True)
    json.dump(c, open(CACHE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)


def check_site(url, cache=None):
    """-> {'kind': none|directory|site, 'host': ..., 'findings': [Finding...]}"""
    kind = classify_url(url)
    if kind == 'none':
        return {'kind': 'none', 'host': '', 'findings': [Finding('none', SEVERITY['none'], '')]}
    host = _host(url)
    if kind == 'directory':
        return {'kind': 'directory', 'host': host, 'findings': [Finding('directory', SEVERITY['directory'], host)]}
    c = cache if cache is not None else _load_cache()
    ent = c.get(host)
    if ent and (dt.date.today() - dt.date.fromisoformat(ent['at'])).days < CACHE_DAYS:
        return {'kind': 'site', 'host': host, 'findings': [Finding(*f) for f in ent['findings']]}
    fs = fetch_and_analyze(url)
    c[host] = {'at': dt.date.today().isoformat(), 'findings': [list(f) for f in fs]}
    if cache is None: _save_cache(c)
    return {'kind': 'site', 'host': host, 'findings': fs}


def check_many(urls, workers=8):
    """{url: result} em paralelo; cache salvo uma vez no fim."""
    cache = _load_cache()
    out = {}
    todo = [u for u in dict.fromkeys(urls)]
    with cf.ThreadPoolExecutor(max_workers=workers) as ex:
        for u, res in zip(todo, ex.map(lambda x: check_site(x, cache), todo)):
            out[u] = res
    _save_cache(cache)
    return out


# ---------- texto (pt-BR, honesto, específico) ----------
def _text(f, empresa):
    return {
        'none':          f'não encontrei um site próprio da {empresa} — quem pesquisa no Google hoje só acha telefone e endereço',
        'directory':     f'a {empresa} aparece no Google só por páginas de terceiros ({f.extra}), sem um site próprio',
        'down':          f'o site não abriu quando testei ({f.extra}) — quem clica no Google cai no vazio',
        'ssl_invalid':   'o certificado de segurança do site está inválido ou vencido — o navegador mostra aviso vermelho antes de entrar',
        'no_https':      'o site abre sem cadeado (HTTPS) — o Chrome marca como "Não seguro" e o Google rebaixa no ranking',
        'no_viewport':   'o site não está adaptado para celular — e 7 em cada 10 buscas locais hoje são pelo celular',
        'slow':          f'a página levou {f.extra}s para responder — acima de 3s, metade dos visitantes desiste',
        'no_whatsapp':   'não há botão de WhatsApp — o cliente precisa copiar o número para falar com vocês',
        'old_copyright': f'o rodapé ainda marca {f.extra} — passa a impressão de site parado',
        'no_meta_desc':  'falta a descrição para o Google — o resultado da busca mostra um trecho aleatório da página',
        'short_title':   f'o título da página é genérico ("{f.extra}") — o Google não entende que serviço vocês oferecem' if f.extra
                         else 'a página está sem título — o Google não entende que serviço vocês oferecem',
    }[f.code]


WAVE_MIN_SEV = 50   # meta description/título são técnicos demais p/ abrir conversa com dono de PME


def summarize_wave(findings, empresa):
    """1 frase p/ o email da wave (achado mais grave, se relevante). '' se o site está ok."""
    if not findings or findings[0].sev < WAVE_MIN_SEV: return ''
    return _text(findings[0], empresa)


def summarize_fup(findings, empresa, n=3):
    """Até n itens p/ o FUP2 (a 'análise gratuita' entregue de verdade)."""
    return [_text(f, empresa) for f in findings[:n]]


if __name__ == '__main__':
    for u in sys.argv[1:]:
        r = check_site(u)
        print(u, '->', r['kind'], r['host'])
        for f in r['findings']: print('  ', f.code, f.sev, f.extra)
        print('  WAVE:', summarize_wave(r['findings'], 'Empresa X'))
