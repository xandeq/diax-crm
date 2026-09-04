# -*- coding: utf-8 -*-
"""Testes offline: classificação de respostas + rascunhos + ranking de nichos."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from reply_watch import is_auto_reply, classify_intent, build_drafts, wa_link, hot_whatsapp_draft
from niche_perf import rank_niches, format_niche_report

# --- auto-reply / bounce -------------------------------------------------
def test_auto_replies_and_bounces_are_ignored():
    assert is_auto_reply('Resposta automática: Fora do escritório', 'joao@x.com.br', {})
    assert is_auto_reply('Out of Office Re: site', 'joao@x.com.br', {})
    assert is_auto_reply('Undeliverable: cardápio online', 'postmaster@x.com.br', {})
    assert is_auto_reply('Re: site', 'mailer-daemon@googlemail.com', {})
    assert is_auto_reply('Re: site', 'joao@x.com.br', {'Auto-Submitted': 'auto-replied'})
    assert is_auto_reply('Re: site', 'joao@x.com.br', {'Precedence': 'bulk'})
    assert not is_auto_reply('Re: cardápio online', 'joao@x.com.br', {})

# --- intenção -----------------------------------------------------------
def test_intent_positive_negative_neutral():
    assert classify_intent('Oi Alexandre, pode enviar sim. Quanto custa?') == 'positive'
    assert classify_intent('Tenho interesse, me manda um orçamento') == 'positive'
    assert classify_intent('Por favor não envie mais emails, remover da lista') == 'negative'
    assert classify_intent('Não temos interesse no momento.') == 'negative'
    assert classify_intent('Quem é você? Como conseguiu meu contato?') == 'neutral'
    assert classify_intent('') == 'neutral'

def test_negative_beats_positive_when_both_present():
    # "não tenho interesse em orçamento" -> negativo (a negação manda)
    assert classify_intent('Não tenho interesse em orçamento, obrigado') == 'negative'

# --- rascunhos ----------------------------------------------------------
LEAD = dict(email='contato@clinicavida.com.br', name='Clínica Vida', seg='clinica', service='Site com Agendamento',
            findings=['o site não está adaptado para celular — e 7 em cada 10 buscas locais hoje são pelo celular'])

def test_drafts_positive_have_next_step_and_finding():
    em, wa = build_drafts(LEAD, 'positive')
    assert 'Clínica Vida' in em and 'celular' in em and ('amanhã' in em or 'hoje' in em)
    assert len(wa) < 420 and 'Clínica Vida' in wa
    assert 'Sou o Alexandre' not in em

def test_drafts_negative_are_short_and_close_the_door():
    em, wa = build_drafts(LEAD, 'negative')
    assert 'não' in em.lower() and len(em) < 400 and wa == ''

def test_wa_link_encodes_and_uses_lead_phone_when_present():
    assert wa_link('27999990000', 'Olá, tudo bem?').startswith('https://wa.me/5527999990000?text=Ol')
    assert wa_link('+55 (27) 99999-0000', 'x') == 'https://wa.me/5527999990000?text=x'
    assert wa_link('', 'x') == ''

def test_hot_whatsapp_draft_mentions_finding_and_service():
    t = hot_whatsapp_draft(LEAD)
    assert 'Clínica Vida' in t and 'celular' in t and len(t) < 500

# --- nichos -------------------------------------------------------------
STATS = {
    'clinica':      {'sent': 300, 'clicks': 12, 'replies': 3},
    'loja':         {'sent': 200, 'clicks': 0,  'replies': 0},
    'restaurante':  {'sent': 160, 'clicks': 0,  'replies': 0},
    'engenharia':   {'sent': 90,  'clicks': 5,  'replies': 1},
    'pousada':      {'sent': 30,  'clicks': 0,  'replies': 0},   # pouca amostra: não corta
    'advocacia':    {'sent': 250, 'clicks': 2,  'replies': 0},
}

def test_rank_niches_cut_and_boost():
    r = rank_niches(STATS, min_sends_cut=150, min_sends_boost=40, top_n=2)
    assert set(r['cut']) == {'loja', 'restaurante'}
    assert r['boost'] == ['engenharia', 'clinica']          # score por envio: eng 10/90 > cli 27/300
    assert 'pousada' not in r['cut'] and 'advocacia' not in r['cut']

def test_format_niche_report_lists_cut_and_boost():
    txt = format_niche_report(rank_niches(STATS))
    assert 'cortar' in txt.lower() and 'loja' in txt and 'engenharia' in txt
