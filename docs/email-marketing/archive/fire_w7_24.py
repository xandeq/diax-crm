# -*- coding: utf-8 -*-
"""Wave 7 — 2026-06-24 — escola x Apps + hotel x Landing Page."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-24-w7.json'

SEGMENTS = [
    dict(
        seg='escola', search_terms=['escola', 'colegio particular', 'instituto educacional'],
        service='Apps',
        hook='Um app mobile pode conectar pais, alunos e professores da sua escola em tempo real.',
        subject='Um aplicativo de comunicacao escolar para o {{empresa}} engajar mais os pais?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Escolas que usam app próprio reduzem circulares em papel, facilitam a comunicação com pais e aumentam o engajamento da comunidade escolar.',
        bullets=['Comunicados e avisos direto no celular dos pais',
                 'Notas e frequencia acessiveis em tempo real',
                 'Agendamento de reunioes e eventos escolares',
                 'Publicado na App Store e Google Play com a marca da escola'],
        wa='Ola! Quero um diagnostico rapido de um app para minha escola',
        img1='A Brazilian school principal showing a school communication app on a tablet to parents at a modern school entrance, realistic editorial photo',
        img2='A bright modern Brazilian school with students in uniforms and teachers using tablets in a well-equipped classroom, realistic editorial photo',
    ),
    dict(
        seg='hotel', search_terms=['hotel', 'pousada boutique', 'hotel fazenda'],
        service='Landing Page',
        hook='Uma landing page de temporada pode fazer hóspedes reservarem direto com você, sem pagar OTA.',
        subject='Uma landing page de reservas diretas para o {{empresa}} nessa temporada?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Uma página dedicada por temporada elimina a comissão das OTAs, exibe disponibilidade e converte a reserva direto pelo WhatsApp.',
        bullets=['Foco em reservas diretas sem comissao de intermediario',
                 'Fotos, diferenciais e depoimentos para despertar desejo',
                 'Botao de WhatsApp para fechar na hora',
                 'Pronta para a sua proxima temporada ou promocao'],
        wa='Ola! Quero um diagnostico rapido de uma landing page de reservas para meu hotel',
        img1='A Brazilian hotel manager presenting a seasonal booking landing page on a laptop at a beautiful hotel lobby, realistic editorial photo',
        img2='A charming Brazilian boutique hotel with a pool, tropical garden and inviting outdoor seating area in natural light, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 7 -- 24/06 -- escola x Apps + hotel x LP =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-24', 'wave': 7, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-24 w7'
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
    print(f'\n[{ts()}] Wave 7 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 7 SEND =====')
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

    print(f'[{ts()}] Wave 7 -- DONE')


if __name__ == '__main__':
    run()
