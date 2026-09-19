# -*- coding: utf-8 -*-
"""Wave 3 — 2026-06-25 — hotel x Sites + pet x Landing Page."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-25-w3.json'

SEGMENTS = [
    dict(
        seg='hotel', search_terms=['hotel', 'pousada', 'pousada boutique', 'hotel resort'],
        service='Sites',
        hook='Um site profissional pode fazer hóspedes encontrarem seu hotel no Google e reservarem direto.',
        subject='Um site para o {{empresa}} aparecer no Google e receber reservas diretas?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quem planeja viagem pesquisa hotel no Google — um site profissional apresenta fotos, comodidades, localização e converte a reserva diretamente pelo WhatsApp, sem comissão de OTA.',
        bullets=['Galeria de fotos dos apartamentos e areas comuns',
                 'Aparecer no Google Maps e pesquisa organica',
                 'Tabela de tarifas e disponibilidade por periodo',
                 'Reserva direta pelo WhatsApp sem pagar comissao'],
        wa='Ola! Quero um diagnostico rapido de um site para meu hotel',
        img1='A Brazilian hotel owner reviewing a professional hotel website on a laptop showing beautiful room photos in a hotel office, realistic editorial photo',
        img2='A stunning Brazilian boutique hotel facade with tropical garden and inviting pool area in warm afternoon sunlight, realistic editorial photo',
    ),
    dict(
        seg='pet', search_terms=['pet shop', 'petshop', 'veterinaria', 'clinica veterinaria pet'],
        service='Landing Page',
        hook='Uma landing page pode fazer donos de pets encontrarem sua clínica veterinária na hora mais crítica.',
        subject='Uma landing page para o {{empresa}} aparecer quando alguém busca veterinário de emergência?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Quando um pet precisa de cuidado urgente, o dono pesquisa no Google — uma landing page do seu pet shop aparece no topo, apresenta serviços e converte o contato na hora.',
        bullets=['Aparecer no Google para busca urgente de veterinario',
                 'Servicos de consulta, vacina, banho e tosa em destaque',
                 'Horario de funcionamento e plantao de emergencia',
                 'WhatsApp para agendamento imediato'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu pet shop',
        img1='A Brazilian veterinarian presenting a professional pet clinic landing page on a tablet at a modern vet clinic with pets in background, realistic editorial photo',
        img2='A welcoming Brazilian pet shop and veterinary clinic entrance with happy pets and owners, modern and clean environment, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 3 -- 25/06 -- hotel x Sites + pet x LP =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-25', 'wave': 3, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-25 w3'
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
    print(f'\n[{ts()}] Wave 3 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 3 SEND =====')
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

    print(f'[{ts()}] Wave 3 -- DONE')


if __name__ == '__main__':
    run()
