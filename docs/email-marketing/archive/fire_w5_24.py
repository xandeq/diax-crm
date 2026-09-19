# -*- coding: utf-8 -*-
"""Wave 5 — 2026-06-24 — clinica x Software de Gestao + loja x Apps + pet x Sites."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-24-w5.json'

SEGMENTS = [
    dict(
        seg='clinica', search_terms=['clinica', 'centro medico', 'policlinica'],
        service='Software de Gestao',
        hook='Um sistema sob medida pode substituir planilhas, reduzir faltas e otimizar o fluxo da sua clínica.',
        subject='Um sistema de gestão sob medida para a {{empresa}} eliminar planilhas?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Clínicas que ainda usam planilhas e papel perdem tempo com agendamentos, prontuários e cobrança — um sistema personalizado centraliza tudo e libera sua equipe.',
        bullets=['Agendamentos e confirmacoes automaticas por mensagem',
                 'Prontuario eletronico acessivel de qualquer dispositivo',
                 'Controle financeiro e relatorios de producao',
                 'Feito sob medida para o fluxo da sua clinica'],
        wa='Ola! Quero um diagnostico rapido de um sistema de gestao para minha clinica',
        img1='A Brazilian clinic manager using a custom healthcare management system on a tablet in a modern medical reception, realistic editorial photo',
        img2='A modern Brazilian medical clinic with a doctor consulting a patient using digital records, clean and professional, realistic editorial photo',
    ),
    dict(
        seg='loja', search_terms=['loja', 'loja de roupas', 'loja de calcados', 'loja de moveis'],
        service='Apps',
        hook='Um app mobile pode fidelizar clientes da sua loja com promoções exclusivas direto no celular.',
        subject='Um aplicativo de fidelidade com a marca da {{empresa}} para seus clientes?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma loja com app próprio envia promoções direto para o celular do cliente, controla pontos de fidelidade e aumenta o ticket médio sem depender de redes sociais.',
        bullets=['Programa de fidelidade com pontos e recompensas',
                 'Notificacoes de promocoes e novidades direto no celular',
                 'Historico de compras e favoritos do cliente',
                 'Publicado na App Store e Google Play com sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app de fidelidade para minha loja',
        img1='A Brazilian retail store owner showing a loyalty app on a smartphone to a happy customer inside a modern store, realistic editorial photo',
        img2='A modern Brazilian retail store with well-organized products and a smiling attendant helping customers, realistic editorial photo',
    ),
    dict(
        seg='pet', search_terms=['pet shop', 'veterinaria', 'clinica veterinaria', 'petshop'],
        service='Sites',
        hook='Um site profissional pode fazer donos de pets encontrarem sua clínica veterinária no Google 24h.',
        subject='Um site para o {{empresa}} aparecer no Google quando alguém busca veterinário?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Donos de pets pesquisam veterinários no Google quando o bichinho precisa de cuidados — um pet shop com site profissional aparece primeiro, apresenta serviços e converte pelo WhatsApp.',
        bullets=['Servicos, horarios e localizacao em destaque',
                 'Aparece no Google para quem busca pet shop na cidade',
                 'Agendamento de consultas pelo WhatsApp direto do site',
                 'Galeria de fotos dos animais atendidos para gerar conexao'],
        wa='Ola! Quero um diagnostico rapido de um site para meu pet shop',
        img1='A Brazilian veterinarian showing a professional pet shop website on a laptop at a modern veterinary clinic, realistic editorial photo',
        img2='A modern Brazilian veterinary clinic with a vet examining a dog and a cat in a clean professional environment, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 5 -- 24/06 -- clinica x SG + loja x Apps + pet x Sites =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-24', 'wave': 5, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-24 w5'
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
