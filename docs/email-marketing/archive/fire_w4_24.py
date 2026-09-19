# -*- coding: utf-8 -*-
"""Wave 4 — 2026-06-24 — transportadora x Landing Page + restaurante x Sites."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-24-w4.json'

SEGMENTS = [
    dict(
        seg='transportadora', search_terms=['transportadora', 'logistica', 'frete', 'transporte de carga'],
        service='Landing Page',
        hook='Uma landing page pode fazer empresas que precisam de frete encontrarem sua transportadora no Google.',
        subject='Uma landing page para captar clientes de frete para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Empresas que precisam de transporte pesquisam no Google — uma transportadora com landing page profissional aparece primeiro, apresenta frota e rotas, e fecha contratos pelo WhatsApp.',
        bullets=['Frota, rotas e servicos em destaque',
                 'Aparece no Google para quem busca frete na regiao',
                 'Formulario de orcamento e WhatsApp para fechar contrato',
                 'Credibilidade com fotos da frota e depoimentos'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha transportadora',
        img1='A Brazilian logistics company manager reviewing a professional landing page on a laptop in a transport office with trucks visible through the window, realistic editorial photo',
        img2='A modern Brazilian transport company fleet of trucks parked in a logistics yard, professional and organized, realistic editorial photo',
    ),
    dict(
        seg='restaurante', search_terms=['restaurante', 'lanchonete', 'pizzaria', 'churrascaria'],
        service='Sites',
        hook='Um site profissional pode fazer clientes famintos encontrarem seu restaurante no Google antes da concorrência.',
        subject='Um site para o {{empresa}} aparecer no Google e receber mais reservas?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quem procura um lugar para comer pesquisa no Google — um restaurante com site profissional exibe cardápio, horários e localização, e converte mais visitas em clientes.',
        bullets=['Cardapio online com fotos dos pratos',
                 'Aparecer no Google para quem busca restaurante na cidade',
                 'Reservas e pedidos pelo WhatsApp direto do site',
                 'Rapido no celular com identidade visual do restaurante'],
        wa='Ola! Quero um diagnostico rapido de um site para meu restaurante',
        img1='A Brazilian restaurant owner showing a professional website on a tablet to a customer at a modern restaurant, realistic editorial photo',
        img2='A modern Brazilian restaurant interior with well-presented dishes on tables and welcoming ambiance, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 4 -- 24/06 -- transportadora x LP + restaurante x Sites =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-24', 'wave': 4, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-24 w4'
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
    print(f'\n[{ts()}] Wave 4 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 4 SEND =====')
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); return
    H = login()

    for seg in wave['segments']:
        cid = seg['campaignId']
        payload = [{'customerId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in seg['leads']]
        r = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                          headers=H, json={'campaignId': cid, 'leads': payload}, timeout=60)
        jr = r.json() if r.status_code in (200, 201, 202) else {}
        print(f'[{ts()}] {seg["seg"]:15} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')

    print(f'[{ts()}] Wave 4 -- DONE')


if __name__ == '__main__':
    run()
