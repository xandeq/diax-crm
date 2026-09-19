# -*- coding: utf-8 -*-
"""Wave 4 — 2026-06-27 — academia x Apps + clinica x Sites."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-27-w4.json'

SEGMENTS = [
    dict(
        seg='academia', search_terms=['academia', 'academia de ginastica', 'crossfit', 'musculacao'],
        service='Apps',
        hook='Um app mobile pode fidelizar alunos da sua academia com treinos, evoluções e promoções.',
        subject='Um aplicativo com a marca da {{empresa}} para engajar e reter seus alunos?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma academia com app próprio envia treinos personalizados, controla evolução dos alunos, promove desafios e reduz a evasão com fidelização digital.',
        bullets=['Treinos personalizados direto no celular do aluno',
                 'Acompanhamento de evolucao e metas por periodo',
                 'Desafios e ranking para engajar a comunidade',
                 'Publicado na App Store e Google Play com sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app para minha academia',
        img1='A Brazilian gym owner showing a fitness app on a smartphone to an athlete during a workout in a modern gym, realistic editorial photo',
        img2='A modern Brazilian gym with people training using weights and machines, energetic atmosphere with motivational lighting, realistic editorial photo',
    ),
    dict(
        seg='clinica', search_terms=['clinica', 'clinica estetica', 'clinica de saude', 'clinica medica especializada'],
        service='Sites',
        hook='Um site profissional pode fazer novos pacientes encontrarem sua clínica no Google e agendar consulta.',
        subject='Um site para a {{empresa}} aparecer no Google e atrair novos pacientes?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quem precisa de uma consulta ou procedimento pesquisa no Google — uma clínica com site profissional aparece primeiro, apresenta especialidades e converte pelo WhatsApp.',
        bullets=['Especialidades e procedimentos em destaque no site',
                 'Aparecer no Google para quem busca clinica na cidade',
                 'Fotos da estrutura e equipe para gerar confianca',
                 'Agendamento de consulta pelo WhatsApp direto do site'],
        wa='Ola! Quero um diagnostico rapido de um site para minha clinica',
        img1='A Brazilian clinic director presenting a professional medical website on a computer at a bright modern clinic, realistic editorial photo',
        img2='A modern Brazilian medical clinic with a welcoming reception area and professional health staff, clean and trustworthy environment, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 4 -- 27/06 -- academia x Apps + clinica x Sites =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-27', 'wave': 4, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-27 w4'
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
        print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')

    print(f'[{ts()}] Wave 4 -- DONE')


if __name__ == '__main__':
    run()
