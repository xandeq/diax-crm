# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-20 - WAVE 4.
Novos nichos: escola->Sites, loja->Apps, pet->Landing, restaurante->Software.
Reutiliza helpers de fire_today_20.py (build_html, gen_image, email_ok, etc.).
"""
import os, sys, json, time, datetime, requests

# Import shared helpers from fire_today_20
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, qp, gen_image, build_html, email_ok,
    fetch_leads, assign_providers, ensure_breaker_closed
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w4.json'

SEGMENTS = [
    dict(seg='escola', search='escola', service='Sites',
         hook='Sua escola conquista mais alunos com um site que faz a matricula chegar at&eacute; voc&ecirc;.',
         subject='Um site profissional para a {{empresa}} que converte visitas em matr&iacute;culas?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Uma escola com site profissional passa credibilidade, responde d&uacute;vidas e capta matr&iacute;culas direto pelo WhatsApp &mdash; sem depender s&oacute; de indica&ccedil;&atilde;o.',
         bullets=['P&aacute;gina de apresenta&ccedil;&atilde;o da escola e diferenciais',
                  'Formul&aacute;rio de interesse em matr&iacute;cula',
                  'Depoimentos de alunos e fotos das instala&ccedil;&otilde;es',
                  'R&aacute;pido e bonito no celular &mdash; onde os pais pesquisam'],
         wa='Ola! Quero um diagnostico rapido de um site para minha escola',
         img1='A Brazilian school principal reviewing a modern school website on a laptop in a bright school office, realistic editorial photo',
         img2='A well-lit Brazilian school building entrance with students and parents, welcoming and professional atmosphere, realistic editorial photo'),
    dict(seg='loja', search='loja', service='Apps',
         hook='E se os seus clientes pudessem ver e comprar do seu cat&aacute;logo direto pelo celular?',
         subject='Um app com cat&aacute;logo e vendas diretas para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app com cat&aacute;logo e pedidos diretos elimina a depend&ecirc;ncia do iFood, fideliza o cliente na sua marca e aumenta o ticket m&eacute;dio.',
         bullets=['Cat&aacute;logo de produtos com fotos e pre&ccedil;os atualizados',
                  'Pedidos e pagamento direto sem comiss&atilde;o de terceiros',
                  'Notifica&ccedil;&otilde;es de promo&ccedil;&otilde;es e lan&ccedil;amentos',
                  'Publicado na App Store e Google Play com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um app para minha loja',
         img1='A Brazilian store owner showing their product catalog app on a smartphone at a retail shop counter, realistic editorial photo',
         img2='A modern Brazilian retail store interior with organized products, warm lighting and happy customers browsing, realistic editorial photo'),
    dict(seg='pet', search='pet shop', service='Landing pages',
         hook='Uma p&aacute;gina s&oacute; para o seu pet shop que captura clientes para banho e tosa.',
         subject='Uma landing page de captacao de clientes para o {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages. Uma p&aacute;gina dedicada com agenda de banho e tosa, depoimentos e fotos dos pets atendidos converte visitantes em clientes fieis.',
         bullets=['P&aacute;gina focada em captacao de clientes de banho e tosa',
                  'Galeria de fotos dos pets atendidos &mdash; prova social',
                  'Agendamento direto pelo WhatsApp',
                  'Aparece no Google de quem busca pet shop na cidade'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para meu pet shop',
         img1='A Brazilian pet shop owner presenting a landing page on a tablet at a grooming salon with dogs, realistic editorial photo',
         img2='A cheerful Brazilian pet grooming salon with dogs being bathed and happy pet owners, bright and clean environment, realistic editorial photo'),
    dict(seg='restaurante', search='restaurante', service='Software sob demanda',
         hook='Seu restaurante ainda controla reservas, pedidos e estoque em planilhas?',
         subject='Um sistema de gest&atilde;o sob medida para o {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas. Um sistema s&oacute; de gest&atilde;o centraliza reservas, comanda, estoque e financeiro &mdash; reduz erro humano e libera o ger&ecirc;nte para o que importa.',
         bullets=['Controle de mesas, reservas e comandas digitais',
                  'Gest&atilde;o de estoque com alertas de reposi&ccedil;&atilde;o',
                  'Relat&oacute;rios financeiros e de desempenho por per&iacute;odo',
                  'Feito sob medida para o fluxo do seu restaurante'],
         wa='Ola! Quero um diagnostico rapido de um sistema para meu restaurante',
         img1='A Brazilian restaurant manager using a restaurant management software on a tablet in a modern restaurant kitchen, realistic editorial photo',
         img2='A busy Brazilian restaurant with tables full of customers and professional waitstaff, warm ambient lighting, realistic editorial photo'),
]


def do_build():
    load_env()
    H = login()
    print(f'[{ts()}] login OK. WAVE 4 (20/06): escola/loja/pet/restaurante')
    os.makedirs(DIR + 'previews', exist_ok=True)
    seen_emails, seen_domains, seen_company = set(), set(), set()

    # Pre-seed seen from today's waves to avoid re-contacting
    for wf in ['previews/wave-2026-06-20-w1.json', 'previews/wave-2026-06-20-w2.json', 'previews/wave-2026-06-20-w3.json']:
        try:
            prev = json.load(open(DIR + wf, encoding='utf-8'))
            for seg in prev.get('segments', []):
                for ld in seg.get('leads', []):
                    e = ld.get('email', '').lower()
                    if e and '@' in e:
                        seen_emails.add(e)
                        seen_domains.add(e.split('@', 1)[1])
                    c = (ld.get('company') or '').lower()
                    import re
                    ck = re.sub(r'[^a-z0-9]', '', c)
                    if ck:
                        seen_company.add(ck)
        except FileNotFoundError:
            pass
    print(f'[{ts()}] Pre-seeded {len(seen_emails)} emails from w1/w2/w3 to avoid re-contact')

    want = PER_PROVIDER * len(HEALTHY)  # 20 per segment
    wave = {'date': '2026-06-20', 'wave': 4, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:12} {cfg["service"]:22} | examinados={examined:4} | selecionados={len(leads)}')
        if not leads:
            print(f'   (0 leads validos - pulando {cfg["seg"]})')
            continue

        print(f'[{ts()}]   gerando imagem 1...')
        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1 = {str(img1)[:60]}')
        print(f'[{ts()}]   gerando imagem 2...')
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img2 = {str(img2)[:60]}')

        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        # Create campaign via API
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w4'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        camp_body = {
            'name': camp_name,
            'subject': cfg['subject'],
            'fromName': 'Alexandre Queiroz',
            'fromEmail': 'contato@alexandrequeiroz.com.br',
            'bodyHtml': html,
            'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')
        }
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers={
            'Authorization': 'Bearer ' + login()['Authorization'].split(' ', 1)[1],
            'Content-Type': 'application/json'
        }, json=camp_body, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIACAO CAMPANHA FALHOU {cr.status_code}: {cr.text[:300]}')
            # If UTM issue, try to relogin and retry once
            continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        print(f'[{ts()}]   campanha criada: {cid}')

        # Schedule campaign
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers={'Authorization': 'Bearer ' + login()['Authorization'].split(' ', 1)[1],
                                    'Content-Type': 'application/json'},
                           json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   schedule: {sc.status_code}')

        for ld in leads:
            ld['campaignId'] = cid

        wave['segments'].append({
            'seg': cfg['seg'],
            'service': cfg['service'],
            'campaignId': cid,
            'leads': leads
        })

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'\n[{ts()}] Wave 4 preview saved: {WAVE_FILE}')
    total = sum(len(s['leads']) for s in wave['segments'])
    print(f'[{ts()}] Total leads selecionados: {total}')
    for s in wave['segments']:
        print(f'  {s["seg"]:12} {s["service"]:22} {len(s["leads"])} leads -> cid {s["campaignId"][:8]}...')
    return wave


def do_send():
    load_env()
    H = login()
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); sys.exit(1)

    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] SEND WAVE 4 ({wave["date"]}) - {len(wave["segments"])} segmentos')
    total_sent = 0
    for i, s in enumerate(wave['segments']):
        cid = s['campaignId']
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']}
                 for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                           headers=H, timeout=60,
                           json={'campaignId': cid, 'leads': leads})
        jr = {}
        try: jr = qq.json()
        except: pass
        print(f'[{ts()}] {s["seg"]:12} {s.get("service",""):22} | QUEUE {qq.status_code} '
              f'queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid[:8]}...')
        if qq.status_code not in (200, 201, 202):
            print(f'   !!! QUEUE rejeitado: {qq.text[:300]}')
            break

        if i == 0:
            print(f'[{ts()}] sonda breaker: aguardando worker (ciclo 5min)...')
            sent = 0
            for t in range(42):
                time.sleep(10)
                d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
                sent = d.get('sentCount', 0)
                print(f'   +{(t+1)*10}s sent={sent} del={d.get("deliveredCount",0)} fail={d.get("failedCount",0)}')
                if sent > 0:
                    break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO - 0 enviados em 420s. Abortando.')
                sys.exit(1)
            print(f'[{ts()}] breaker OK - seguindo.\n')

        if i < len(wave['segments']) - 1:
            time.sleep(15)
        total_sent += jr.get('queuedCount', 0)

    time.sleep(20)
    print(f'\n[{ts()}] === WAVE 4 QUEUED: {total_sent} leads ===')
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'build'
    if cmd == 'build':
        do_build()
    elif cmd == 'send':
        do_send()
    else:
        print('Usage: python fire_w4_20.py [build|send]')
