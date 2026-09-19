# -*- coding: utf-8 -*-
"""Wave 7 — 2026-06-27 — academia x Sites + imobiliaria x Landing Page."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-27-w7.json'

SEGMENTS = [
    dict(
        seg='academia', search_terms=['academia', 'pilates', 'yoga', 'academia funcional'],
        service='Sites',
        hook='Um site profissional pode fazer novos alunos encontrarem sua academia no Google e se matricularem.',
        subject='Um site para a {{empresa}} atrair novos alunos pelo Google?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quem quer começar a treinar pesquisa academia no Google — um site profissional apresenta modalidades, estrutura, horários e converte a matrícula pelo WhatsApp.',
        bullets=['Modalidades, horarios e planos em destaque',
                 'Fotos da estrutura e equipamentos para gerar desejo',
                 'Aparecer no Google para quem busca academia na cidade',
                 'Matricula e agendamento de aula experimental pelo WhatsApp'],
        wa='Ola! Quero um diagnostico rapido de um site para minha academia',
        img1='A Brazilian gym director showing a professional gym website on a laptop at a modern reception with gym equipment visible, realistic editorial photo',
        img2='A modern Brazilian fitness studio with various exercise equipment and people working out in a clean bright space, realistic editorial photo',
    ),
    dict(
        seg='imobiliaria', search_terms=['imobiliaria', 'corretor de imoveis', 'imobiliaria residencial', 'corretor imovel'],
        service='Landing Page',
        hook='Uma landing page por bairro pode gerar leads qualificados de compradores e locatários direto do Google.',
        subject='Uma landing page de captacao de compradores para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Uma página por bairro ou tipo de imóvel aparece no Google para quem busca exatamente aquela região, apresenta os destaques do portfólio e converte o contato pelo WhatsApp.',
        bullets=['Pagina focada em bairro ou tipo de imovel para captar lead certo',
                 'Galeria de imóveis em destaque com fotos e valores',
                 'Aparecer no Google para quem busca imovel na cidade',
                 'Formulario e WhatsApp para agendar visita imediata'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha imobiliaria',
        img1='A Brazilian real estate agent showing a professional property landing page on a tablet to clients at a modern real estate office, realistic editorial photo',
        img2='A modern Brazilian real estate office with agents presenting property listings on large screens to interested buyers, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 7 -- 27/06 -- academia x Sites + imobiliaria x LP =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-27', 'wave': 7, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-27 w7'
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
