# -*- coding: utf-8 -*-
"""Testes offline do diagnóstico de site (sem rede)."""
import sys, os, datetime as dt
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from site_check import classify_url, analyze, summarize_wave, summarize_fup, Finding

Y = dt.date.today().year

GOOD = ('<html><head><title>Clínica Vida — Cardiologia em Vitória ES</title>'
        '<meta name="viewport" content="width=device-width, initial-scale=1">'
        '<meta name="description" content="Clínica de cardiologia em Vitória. Agende sua consulta."></head>'
        f'<body><a href="https://wa.me/5527999999999">WhatsApp</a><footer>© {Y} Clínica Vida</footer></body></html>')

BAD = ('<html><head><title>Home</title></head>'
       '<body><p>Bem-vindo</p><footer>Copyright 2017 Empresa</footer></body></html>')


def codes(fs): return [f.code for f in fs]

# --- classify_url -------------------------------------------------------
def test_classify_none_and_site():
    assert classify_url('') == 'none'
    assert classify_url(None) == 'none'
    assert classify_url('https://www.coimbraimoveis.com/') == 'site'

def test_classify_directories_and_socials():
    for u in ('https://www.econodata.com.br/consulta-empresa/1942', 'https://cliniguia.com/unidades/serra-es/x',
              'https://www.facebook.com/loja', 'https://instagram.com/loja', 'https://www.google.com/maps/place/x',
              'https://www.apontador.com.br/local/x', 'https://cnpj.biz/123'):
        assert classify_url(u) == 'directory', u

# --- analyze ------------------------------------------------------------
def test_good_site_has_no_findings():
    fs = analyze(GOOD, final_url='https://clinicavida.com.br/', status=200, elapsed=0.8, nbytes=120_000)
    assert fs == []

def test_bad_site_findings_ordered_by_severity():
    fs = analyze(BAD, final_url='http://empresa.com.br/', status=200, elapsed=4.2, nbytes=90_000)
    c = codes(fs)
    assert c[0] == 'no_https'
    assert 'no_viewport' in c and 'slow' in c and 'no_whatsapp' in c
    assert 'old_copyright' in c and 'no_meta_desc' in c and 'short_title' in c
    assert c.index('no_viewport') < c.index('no_whatsapp') < c.index('old_copyright')

def test_http_error_is_single_top_finding():
    fs = analyze('', final_url='https://x.com.br/', status=500, elapsed=1.0, nbytes=0)
    assert codes(fs) == ['down']

def test_bot_block_is_inconclusive_not_down():
    # review Codex: 403/429/503 p/ robô ≠ site fora do ar — nenhuma afirmação
    for st in (401, 403, 429, 503):
        fs = analyze('', 'https://x.com.br/', st, 1.0, 0)
        assert codes(fs) == ['inconclusive'], st
        assert summarize_wave(fs, 'X') == ''
        assert summarize_fup(fs, 'X') is None
    assert summarize_fup(None, 'X') is None

def test_private_targets_rejected():
    from site_check import is_public_ip, safe_url
    for ip in ('127.0.0.1', '10.1.2.3', '192.168.0.10', '172.16.5.5', '169.254.169.254', '0.0.0.0', '::1', 'fd00::1'):
        assert not is_public_ip(ip), ip
    assert is_public_ip('8.8.8.8') and is_public_ip('2001:4860:4860::8888')
    assert safe_url('ftp://x.com.br/') is None
    assert safe_url('http://localhost/') is None
    assert safe_url('http://127.0.0.1:8000/') is None
    assert safe_url('http://169.254.169.254/latest/meta-data') is None

def test_short_title_keeps_raw_text_for_renderer_to_escape():
    html = GOOD.replace('Clínica Vida — Cardiologia em Vitória ES', '<b>Home')
    fs = analyze(html, 'https://a.com.br/', 200, 0.5, 1000)
    t = [f for f in fs if f.code == 'short_title'][0]
    assert t.extra == '<b>Home'

def test_ssl_invalid_flag():
    fs = analyze(GOOD, final_url='https://x.com.br/', status=200, elapsed=0.5, nbytes=1000, ssl_invalid=True)
    assert codes(fs)[0] == 'ssl_invalid'

def test_whatsapp_variants_detected():
    for snippet in ('href="https://api.whatsapp.com/send?phone=55"', 'href="whatsapp://send?text=oi"', 'wa.me/55'):
        html = GOOD.replace('https://wa.me/5527999999999', snippet)
        assert 'no_whatsapp' not in codes(analyze(html, 'https://a.com.br/', 200, 0.5, 1000))

def test_recent_copyright_not_flagged():
    html = BAD.replace('2017', str(Y - 1))
    assert 'old_copyright' not in codes(analyze(html, 'https://a.com.br/', 200, 0.5, 1000))

# --- summaries ----------------------------------------------------------
def test_summarize_wave_none_and_directory():
    s = summarize_wave([Finding('none', 100, '')], 'Padaria Sol')
    assert 'Padaria Sol' in s and 'site próprio' in s
    s = summarize_wave([Finding('directory', 95, 'econodata.com.br')], 'Padaria Sol')
    assert 'econodata.com.br' in s

def test_summarize_wave_empty_returns_empty():
    assert summarize_wave([], 'X') == ''

def test_summarize_wave_skips_minor_only_findings():
    # só meta description/título → técnico demais p/ abrir a conversa: sem achado na wave
    fs = analyze(GOOD.replace('<meta name="description"', '<meta name="x"'), 'https://a.com.br/', 200, 0.5, 1000)
    assert [f.code for f in fs] == ['no_meta_desc']
    assert summarize_wave(fs, 'X') == ''
    assert summarize_fup(fs, 'X') != []   # no FUP2 (lista) ainda entra

def test_summarize_fup_takes_top3_as_list():
    fs = analyze(BAD, 'http://e.com.br/', 200, 4.2, 1000)
    items = summarize_fup(fs, 'Empresa')
    assert len(items) == 3 and all(isinstance(i, str) and i for i in items)
    assert 'HTTPS' in items[0]
