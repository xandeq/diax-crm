# -*- coding: utf-8 -*-
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from mx_check import parse_nslookup_mx, is_junk_domain, deliverable_domain

NS_OK = """Servidor:  UnKnown
Address:  192.168.0.1

Resposta não autoritativa:
clinicavida.com.br      MX preference = 10, mail exchanger = mail.clinicavida.com.br
clinicavida.com.br      MX preference = 20, mail exchanger = mx2.clinicavida.com.br
"""
NS_NONE = """Servidor:  UnKnown
Address:  192.168.0.1

*** UnKnown can't find nadaqui.com.br: Non-existent domain
"""
NS_NULL = "x.com.br  MX preference = 0, mail exchanger = .\n"   # Null MX (RFC 7505) = não recebe email

def test_parse_mx():
    assert parse_nslookup_mx(NS_OK) == ['mail.clinicavida.com.br', 'mx2.clinicavida.com.br']
    assert parse_nslookup_mx(NS_NONE) == []
    assert parse_nslookup_mx(NS_NULL) == []

def test_junk_domains():
    for d in ('instagram.local', 'x.local', 'sentry-next.wixpress.com', 'example.com', 'email.com', 'site.com.br',
              'sentry.io', 'google_maps', 'localhost', 'wixpress.com', 'test.com'):
        assert is_junk_domain(d), d
    assert not is_junk_domain('clinicavida.com.br')

def test_deliverable_uses_cache_and_resolver():
    cache = {}
    calls = []
    def fake(domain):
        calls.append(domain); return ['mx.' + domain] if domain == 'ok.com.br' else []
    assert deliverable_domain('ok.com.br', cache, resolver=fake) is True
    assert deliverable_domain('ok.com.br', cache, resolver=fake) is True      # cache: sem 2ª chamada
    assert deliverable_domain('dead.com.br', cache, resolver=fake) is False
    assert deliverable_domain('instagram.local', cache, resolver=fake) is False  # junk: nem resolve
    assert calls == ['ok.com.br', 'dead.com.br']
