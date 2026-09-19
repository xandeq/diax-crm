# -*- coding: utf-8 -*-
"""Wave 2 — 2026-06-25 — advocacia x Landing Page + escola x Sites."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-25-w2.json'

SEGMENTS = [
    dict(
        seg='advocacia', search_terms=['advocacia', 'escritorio de advocacia', 'advogado'],
        service='Landing Page',
        hook='Uma landing page por área de atuação pode trazer clientes qualificados para seu escritório todo dia.',
        subject='Uma landing page especializada para captar clientes para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Uma página por especialidade (trabalhista, família, empresarial) aparece no Google para quem busca exatamente aquela área e converte direto pelo WhatsApp.',
        bullets=['Pagina focada em uma especialidade para atrair cliente certo',
                 'Aparecer no Google para quem busca advogado especialista',
                 'Depoimentos e casos de sucesso para gerar credibilidade',
                 'Formulario de consulta gratuita para converter o contato'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu escritorio de advocacia',
        img1='A Brazilian lawyer presenting a specialized law firm landing page on a laptop at a prestigious legal office, realistic editorial photo',
        img2='A modern Brazilian law office with a lawyer consulting a client at a conference table, professional and trustworthy environment, realistic editorial photo',
    ),
    dict(
        seg='escola', search_terms=['escola', 'colegio', 'colegio particular', 'escola particular'],
        service='Sites',
        hook='Um site profissional pode mostrar a estrutura e o projeto pedagógico da sua escola para novos pais.',
        subject='Um site para o {{empresa}} atrair novas matrículas pelo Google?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Pais que buscam escola para os filhos pesquisam no Google — uma escola com site profissional apresenta estrutura, valores, projeto pedagógico e converte matrículas pelo WhatsApp.',
        bullets=['Estrutura fisica, laboratorios e areas de lazer em fotos',
                 'Projeto pedagogico e diferenciais academicos em destaque',
                 'Aparecer no Google para quem busca escola na cidade',
                 'Formulario de visita e matricula para converter o interesse'],
        wa='Ola! Quero um diagnostico rapido de um site para minha escola',
        img1='A Brazilian school principal showing a professional school website on a laptop to parents in a modern school lobby, realistic editorial photo',
        img2='A bright and welcoming Brazilian private school exterior with students playing in a well-maintained courtyard, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 2 -- 25/06 -- advocacia x LP + escola x Sites =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-25', 'wave': 2, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-25 w2'
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
    print(f'\n[{ts()}] Wave 2 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 2 SEND =====')
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

    print(f'[{ts()}] Wave 2 -- DONE')


if __name__ == '__main__':
    run()
