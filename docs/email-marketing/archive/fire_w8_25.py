# -*- coding: utf-8 -*-
"""Wave 8 — 2026-06-25 — imobiliaria x Apps + clinica x Software de Gestao. Wave final."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-25-w8.json'

# Wave final — limitar para nao exceder safe limit
PER_PROVIDER_FINAL = 5

SEGMENTS = [
    dict(
        seg='imobiliaria', search_terms=['imobiliaria', 'corretor imoveis', 'construtora', 'loteamento'],
        service='Apps',
        hook='Um app mobile pode fazer clientes buscarem imóveis do seu portfólio direto no celular.',
        subject='Um aplicativo de busca de imóveis com a marca do {{empresa}} para seus clientes?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma imobiliária com app próprio permite que clientes pesquisem imóveis, salvem favoritos, agendem visitas e se comuniquem com o corretor direto pelo app.',
        bullets=['Busca de imoveis por bairro, tipo e faixa de preco',
                 'Favoritos e historico de imóveis vistos pelo cliente',
                 'Agendamento de visita direto pelo app com o corretor',
                 'Publicado na App Store e Google Play com sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app para minha imobiliaria',
        img1='A Brazilian real estate agent showing a property search app on a smartphone to clients at a modern real estate office, realistic editorial photo',
        img2='A modern Brazilian real estate office with large screens displaying property listings and agents helping clients, realistic editorial photo',
    ),
    dict(
        seg='clinica', search_terms=['clinica', 'clinica odontologica', 'clinica de nutricao', 'clinica fisioterapia'],
        service='Software de Gestao',
        hook='Um sistema sob medida pode centralizar pacientes, agenda e financeiro da sua clínica em um só lugar.',
        subject='Um sistema de gestao personalizado para a {{empresa}} eliminar planilhas e papel?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Clínicas que centralizam pacientes, agendamentos e financeiro num sistema próprio reduzem o retrabalho e melhoram o atendimento ao paciente.',
        bullets=['Prontuario eletronico e historico completo do paciente',
                 'Agenda integrada com confirmacao automatica por mensagem',
                 'Controle financeiro de consultas, procedimentos e recebimentos',
                 'Feito sob medida para o fluxo especifico da sua clinica'],
        wa='Ola! Quero um diagnostico rapido de um sistema de gestao para minha clinica',
        img1='A Brazilian clinic manager reviewing a custom clinic management system on a computer at a modern medical clinic, realistic editorial photo',
        img2='A professional Brazilian clinic team using digital systems to manage patient records and appointments in a modern environment, realistic editorial photo',
    ),
]

MAX_PER_SEG = PER_PROVIDER_FINAL * len(HEALTHY)


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
    print(f'\n[{ts()}] ===== WAVE 8 -- 25/06 -- imobiliaria x Apps + clinica x SG [WAVE FINAL] =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-25', 'wave': 8, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-25 w8'
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
    print(f'\n[{ts()}] Wave 8 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 8 SEND =====')
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

    print(f'[{ts()}] Wave 8 -- DONE (wave final do dia 25/06)')


if __name__ == '__main__':
    run()
