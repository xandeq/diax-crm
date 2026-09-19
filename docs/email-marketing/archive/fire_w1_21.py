# -*- coding: utf-8 -*-
"""Wave 1 — 2026-06-21 — medico x Landing Page."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-21-w1.json'

SEGMENTS = [
    dict(
        seg='medico', search_terms=['medico', 'clinica', 'consultorio', 'especialista'],
        service='Landing Page',
        hook='Uma landing page pode fazer pacientes encontrarem o seu consultório no Google 24 horas por dia.',
        subject='Uma landing page de captação de pacientes para o Dr. {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Quem precisa de médico pesquisa no Google — um consultório com landing page aparece nos primeiros resultados, passa credibilidade e recebe pedidos de consulta pelo WhatsApp todos os dias.',
        bullets=['Especialidade e área de atuação em destaque',
                 'Aparece no Google para quem busca médico na cidade',
                 'Formulário de agendamento e WhatsApp para consulta rápida',
                 'Depoimentos de pacientes e credenciais do CRM'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu consultorio medico',
        img1='A Brazilian doctor reviewing a professional medical landing page on a laptop in a modern clinic office, realistic editorial photo',
        img2='A professional Brazilian medical clinic reception with modern decor and patients waiting, realistic editorial photo',
    ),
]

MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)  # 20


def fetch_leads_multi(search_terms, H, seen_e, seen_d, seen_c, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want:
            break
        leads, _ = fetch_leads({'search': term}, H, seen_e, seen_d, seen_c, want - len(all_leads))
        if leads:
            print(f'   search="{term}" → {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def run():
    load_env(); H = login()
    print(f'\n[{ts()}] ===== WAVE 1 — 21/06 — medico x Landing Page =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-21', 'wave': 1, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} × {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_e, seen_d, seen_c, MAX_PER_SEG)
        if not leads:
            print('   0 leads disponíveis — pulando'); continue

        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads selecionados')

        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-21 w1'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2,
                           json={'name': camp_name, 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIAÇÃO FALHOU {cr.status_code}: {cr.text[:300]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')

        for ld in leads:
            ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total_built = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 1 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTÁRIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 1 SEND =====')
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); return
    H = login()

    for seg in wave['segments']:
        cid = seg['campaignId']
        payload = [{'leadId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in seg['leads']]
        r = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/queue-with-assignment',
                          headers=H, json=payload, timeout=60)
        jr = r.json() if r.status_code in (200, 201) else {}
        print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')

    print(f'[{ts()}] Wave 1 — DONE')


if __name__ == '__main__':
    run()
