# -*- coding: utf-8 -*-
"""Wave 6 — 2026-06-27 — advocacia x Software de Gestao + hotel x Software de Gestao."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-27-w6.json'

SEGMENTS = [
    dict(
        seg='advocacia', search_terms=['advocacia', 'escritorio advocacia', 'advogado trabalhista', 'advogado empresarial'],
        service='Software de Gestao',
        hook='Um sistema sob medida pode automatizar a gestão de processos e clientes do seu escritório.',
        subject='Um sistema de gestao de processos sob medida para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Escritórios de advocacia que ainda gerenciam processos em planilhas perdem tempo e cometem erros — um sistema próprio centraliza clientes, prazos e documentos.',
        bullets=['Controle de processos com prazos e alertas automaticos',
                 'CRM de clientes com historico completo de interacoes',
                 'Geracao de documentos e peticoes com templates',
                 'Dashboard financeiro com honorarios e recebimentos'],
        wa='Ola! Quero um diagnostico rapido de um sistema de gestao para meu escritorio de advocacia',
        img1='A Brazilian lawyer using a custom legal management software on a computer in a prestigious law office, realistic editorial photo',
        img2='A modern Brazilian law office with lawyers using multiple monitors showing case management dashboards, realistic editorial photo',
    ),
    dict(
        seg='hotel', search_terms=['hotel', 'pousada', 'resort', 'hotel boutique'],
        service='Software de Gestao',
        hook='Um sistema de gestão hoteleira sob medida pode centralizar reservas, hóspedes e financeiro.',
        subject='Um sistema de gestao hoteleira sob medida para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Hotéis que ainda usam planilhas para controlar reservas, quartos e financeiro perdem eficiência — um sistema próprio integra tudo em um painel centralizado.',
        bullets=['Controle de reservas e disponibilidade de quartos em tempo real',
                 'Checkin e checkout digital com historico do hospede',
                 'Integracao com canais de distribuicao e OTAs',
                 'Relatorios financeiros e de ocupacao por periodo'],
        wa='Ola! Quero um diagnostico rapido de um sistema de gestao para meu hotel',
        img1='A Brazilian hotel manager using a custom property management system on a tablet at the hotel reception, realistic editorial photo',
        img2='A modern Brazilian boutique hotel reception area with staff using digital systems and guests checking in, realistic editorial photo',
    ),
]

MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)


def fetch_leads_multi(search_terms, H, seen_e, seen_d, seen_c, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want:
            break
        leads, _ = fetch_leads({'search': term}, H, seen_e, seen_d, seen_c, want - len(all_leads))
        if leads:
            print(f'   search="{term}" -> {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def run():
    load_env(); H = login()
    print(f'\n[{ts()}] ===== WAVE 6 -- 27/06 -- advocacia x SG + hotel x SG =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-27', 'wave': 6, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} x {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_e, seen_d, seen_c, MAX_PER_SEG)
        if not leads:
            print('   0 leads disponiveis -- pulando'); continue

        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads selecionados')

        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-27 w6'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2,
                           json={'name': camp_name, 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIACAO FALHOU {cr.status_code}: {cr.text[:300]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')

        for ld in leads:
            ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total_built = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 6 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 6 SEND =====')
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); return
    H = login()

    for seg in wave['segments']:
        cid = seg['campaignId']
        payload = [{'customerId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in seg['leads']]
        r = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                          headers=H, json={'campaignId': cid, 'leads': payload}, timeout=60)
        jr = r.json() if r.status_code in (200, 201, 202) else {}
        print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')

    print(f'[{ts()}] Wave 6 -- DONE')


if __name__ == '__main__':
    run()
