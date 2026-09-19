# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-20 - WAVE 5.
Novos combos: clinica->Sites, contabilidade->Landing, hotel->Apps, medico->Sites.
"""
import os, sys, json, time, datetime, requests

sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, qp, gen_image, build_html, email_ok,
    fetch_leads, assign_providers, ensure_breaker_closed
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w5.json'
PREV_WAVES = ['previews/wave-2026-06-20-w1.json', 'previews/wave-2026-06-20-w2.json',
              'previews/wave-2026-06-20-w3.json', 'previews/wave-2026-06-20-w4.json']

SEGMENTS = [
    dict(seg='clinica', search='clinica', service='Sites',
         hook='Sua cl&iacute;nica aparece quando algu&eacute;m pesquisa no Google por especialistas na sua cidade?',
         subject='Um site profissional para a {{empresa}} que capta pacientes direto do Google?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Pacientes pesquisam cl&iacute;nicas no Google antes de ligar &mdash; um site que aparece bem, transmite confian&ccedil;a e facilita o agendamento traz mais consultas sem depender de indica&ccedil;&atilde;o.',
         bullets=['Apresenta&ccedil;&atilde;o da equipe m&eacute;dica e especialidades',
                  'Aparecer no Google de quem busca cl&iacute;nica na cidade',
                  'Bot&atilde;o de agendamento e contato WhatsApp',
                  'R&aacute;pido, seguro (HTTPS) e bonito no celular'],
         wa='Ola! Quero um diagnostico rapido de um site para minha clinica',
         img1='A Brazilian clinic receptionist showing a professional clinic website on a computer at a modern medical reception, realistic editorial photo',
         img2='A welcoming Brazilian medical clinic entrance with a friendly receptionist and bright waiting area, realistic editorial photo'),
    dict(seg='contabilidade', search='contabilidade', service='Landing pages',
         hook='Uma landing page para seu escrit&oacute;rio de contabilidade que converte empresas em clientes.',
         subject='Uma landing page de captacao de clientes para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages. Empres&aacute;rios que precisam de contabilidade pesquisam no Google &mdash; uma p&aacute;gina objetiva que explica servi&ccedil;os, mostra diferenciais e tem WhatsApp converte muito mais que um perfil de rede social.',
         bullets=['Apresenta&ccedil;&atilde;o clara dos servi&ccedil;os contábeis ofertados',
                  'Depoimentos de clientes e casos de sucesso',
                  'Contato direto por WhatsApp para or&ccedil;amento',
                  'Aparece no Google para empresas que buscam contador'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para meu escritorio de contabilidade',
         img1='A Brazilian accountant presenting services on a laptop to a business client in a professional office, realistic editorial photo',
         img2='A modern Brazilian accounting office with organized documents and a professional team working, realistic editorial photo'),
    dict(seg='hotel', search='hotel', service='Apps',
         hook='E se os h&oacute;spedes do seu hotel pudessem pedir servi&ccedil;os e fazer check-in pelo celular?',
         subject='Um app de h&oacute;spede com a marca do {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app de h&oacute;spede permite check-in digital, solicita&ccedil;&atilde;o de servi&ccedil;os, cardápio do restaurant e avalia&ccedil;&otilde;es &mdash; eleva a experi&ecirc;ncia e reduz demanda no front desk.',
         bullets=['Check-in e check-out digital pelo celular',
                  'Solicita&ccedil;&atilde;o de room service, toalhas, concierge',
                  'Mapa do hotel, restaurante e atra&ccedil;&otilde;es locais',
                  'Avisos de promo&ccedil;&otilde;es e upsell de servi&ccedil;os'],
         wa='Ola! Quero um diagnostico rapido de um app de hospede para meu hotel',
         img1='A hotel guest using a hotel app on a smartphone to request room service in a luxury Brazilian hotel room, realistic editorial photo',
         img2='A modern Brazilian hotel lobby with guests using mobile devices and professional staff at the front desk, realistic editorial photo'),
    dict(seg='medico', search='medico', service='Sites',
         hook='Seu consultório médico aparece no Google quando alguém busca sua especialidade na cidade?',
         subject='Um site profissional para o Dr. {{empresa}} que atrai pacientes pelo Google?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Pacientes buscam m&eacute;dicos no Google antes de ligar. Um site com sua especialidade, forma&ccedil;&atilde;o, atendimentos aceitos e WhatsApp para agendamento gera consultas diretas &mdash; sem depender s&oacute; de indica&ccedil;&atilde;o.',
         bullets=['Perfil profissional com especialidade e forma&ccedil;&atilde;o',
                  'Aparece no Google de quem busca seu especialidade na cidade',
                  'Convenios e planos de sa&uacute;de aceitos',
                  'Agendamento pelo WhatsApp direto do site'],
         wa='Ola! Quero um diagnostico rapido de um site para meu consultorio medico',
         img1='A Brazilian doctor looking at a professional medical website on a laptop in a modern doctor office, realistic editorial photo',
         img2='A professional Brazilian doctor consulting with a patient in a modern medical office with good lighting, realistic editorial photo'),
]


def load_used_today():
    """Load all emails sent in previous waves today to avoid re-contact."""
    used = set()
    for wf in PREV_WAVES:
        try:
            w = json.load(open(DIR + wf, encoding='utf-8'))
            for s in w.get('segments', []):
                for ld in s.get('leads', []):
                    e = ld.get('email', '').lower()
                    if e: used.add(e)
        except FileNotFoundError:
            pass
    return used


def do_build():
    load_env(); H = login()
    print(f'[{ts()}] login OK. WAVE 5 (20/06): clinica-sites/contabilidade-landing/hotel-apps/medico-sites')
    os.makedirs(DIR + 'previews', exist_ok=True)

    used_today = load_used_today()
    print(f'[{ts()}] Pre-seeded {len(used_today)} emails from w1-w4')

    seen_emails = set(used_today)
    seen_domains = {e.split('@', 1)[1] for e in used_today if '@' in e}
    seen_company = set()
    import re

    # Also load previous wave companies
    for wf in PREV_WAVES:
        try:
            w = json.load(open(DIR + wf, encoding='utf-8'))
            for s in w.get('segments', []):
                for ld in s.get('leads', []):
                    c = (ld.get('company') or '').lower()
                    ck = re.sub(r'[^a-z0-9]', '', c)
                    if ck: seen_company.add(ck)
        except FileNotFoundError:
            pass

    want = PER_PROVIDER * len(HEALTHY)  # 20 per segment
    wave = {'date': '2026-06-20', 'wave': 5, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:15} {cfg["service"]:22} | exam={examined:4} | sel={len(leads)}')
        if not leads:
            print(f'   (0 leads — pulando {cfg["seg"]})')
            continue

        print(f'[{ts()}]   img1...')
        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1={str(img1)[:60]}')
        print(f'[{ts()}]   img2...')
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img2={str(img2)[:60]}')

        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w5'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2,
                           json={'name': camp_name, 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIACAO FALHOU {cr.status_code}: {cr.text[:300]}')
            continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')

        for ld in leads: ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 5 preview saved: {total} leads')
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:22} {len(s["leads"])} leads -> cid {s["campaignId"][:8]}...')
    return wave


def do_send():
    load_env(); H = login()
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); sys.exit(1)

    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] SEND WAVE 5 - {len(wave["segments"])} segmentos')
    total_sent = 0
    for i, s in enumerate(wave['segments']):
        cid = s['campaignId']
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']}
                 for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                           headers=H, timeout=60, json={'campaignId': cid, 'leads': leads})
        jr = {}
        try: jr = qq.json()
        except: pass
        print(f'[{ts()}] {s["seg"]:15} {s.get("service",""):22} | QUEUE {qq.status_code} '
              f'queued={jr.get("queuedCount")} | cid {cid[:8]}...')
        if qq.status_code not in (200, 201, 202):
            err = qq.text[:300]
            if 'Validation.Recipients' in err:
                print(f'   (ja enfileirado — pulando)')
                continue
            print(f'   !!! QUEUE rejeitado: {err}')
            break

        if i == 0:
            print(f'[{ts()}] sonda breaker...')
            sent = 0
            for t in range(42):
                time.sleep(10)
                d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
                sent = d.get('sentCount', 0)
                print(f'   +{(t+1)*10}s sent={sent} del={d.get("deliveredCount",0)} fail={d.get("failedCount",0)}')
                if sent > 0: break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO - 0 enviados em 420s'); sys.exit(1)
            print(f'[{ts()}] breaker OK\n')

        if i < len(wave['segments']) - 1:
            time.sleep(15)
        total_sent += jr.get('queuedCount', 0)

    time.sleep(20)
    print(f'\n[{ts()}] === WAVE 5 QUEUED: {total_sent} ===')
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'build'
    if cmd == 'build': do_build()
    elif cmd == 'send': do_send()
    else: print('Usage: python fire_w5_20.py [build|send]')
