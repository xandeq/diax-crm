# -*- coding: utf-8 -*-
"""Wave 5 — 2026-06-25 — contabilidade x Sites + escola x Landing Page + pet x Apps."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-25-w5.json'

SEGMENTS = [
    dict(
        seg='contabilidade', search_terms=['contabilidade', 'contadora', 'escritorio contabil', 'contabilidade empresarial'],
        service='Sites',
        hook='Um site profissional pode fazer empresas encontrarem seu escritório contábil no Google com autoridade.',
        subject='Um site para o {{empresa}} transmitir autoridade e atrair novos clientes?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quando um empresário busca contador no Google, a credibilidade do site decide quem liga primeiro. Um site profissional mostra a equipe, serviços, certificações e converte pelo WhatsApp.',
        bullets=['Perfil da equipe e certificacoes para gerar credibilidade',
                 'Servicos contabeis detalhados para cada perfil de empresa',
                 'Aparecer no Google para quem busca contador na cidade',
                 'Blog com dicas fiscais para atrair trafego organico'],
        wa='Ola! Quero um diagnostico rapido de um site para meu escritorio contabil',
        img1='A Brazilian accounting team reviewing a professional accounting firm website on a large monitor in a modern office, realistic editorial photo',
        img2='A professional Brazilian accounting office with a diverse team of accountants working with computers and financial documents, realistic editorial photo',
    ),
    dict(
        seg='escola', search_terms=['escola', 'escola de idiomas', 'escola tecnica', 'escola profissionalizante'],
        service='Landing Page',
        hook='Uma landing page por curso pode trazer alunos qualificados que buscam exatamente o que você oferece.',
        subject='Uma landing page focada em captacao de alunos para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Uma página por curso ou modalidade aparece no Google para quem busca exatamente aquela formação, apresenta grade, professores e converte a matrícula direto pelo WhatsApp.',
        bullets=['Pagina focada em um curso especifico para atrair aluno certo',
                 'Grade curricular e carga horaria em destaque',
                 'Depoimentos de ex-alunos para gerar confianca',
                 'Botao de inscricao e WhatsApp para matricula imediata'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha escola',
        img1='A Brazilian school director showing a course landing page on a laptop to prospective students in a modern classroom, realistic editorial photo',
        img2='A modern Brazilian classroom with students engaged in practical learning activities, professional and inspiring environment, realistic editorial photo',
    ),
    dict(
        seg='pet', search_terms=['pet shop', 'petshop', 'banho e tosa', 'adestramento'],
        service='Apps',
        hook='Um app mobile pode fidelizar clientes do seu pet shop com agendamentos, histórico e promoções.',
        subject='Um aplicativo de fidelidade com a marca do {{empresa}} para donos de pets?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um pet shop com app próprio permite que clientes agendem banho e tosa 24h, acompanhem o histórico do pet e recebam promoções no celular.',
        bullets=['Agendamento de banho, tosa e consulta pelo app',
                 'Historico de servicos e vacinacao do pet',
                 'Notificacoes de promocoes e novidades no celular',
                 'Publicado na App Store e Google Play com sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app para meu pet shop',
        img1='A Brazilian pet shop owner showing a pet care app on a smartphone to a customer with their dog at a modern pet shop, realistic editorial photo',
        img2='A cheerful modern Brazilian pet shop with grooming area, products and happy pets with their owners, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 5 -- 25/06 -- contabilidade x Sites + escola x LP + pet x Apps =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-25', 'wave': 5, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-25 w5'
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
    print(f'\n[{ts()}] Wave 5 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 5 SEND =====')
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

    print(f'[{ts()}] Wave 5 -- DONE')


if __name__ == '__main__':
    run()
