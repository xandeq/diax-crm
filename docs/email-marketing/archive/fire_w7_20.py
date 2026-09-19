# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-20 - WAVE 7 (final).
Limpeza do inventario: academia/supermercado/estetica/imobiliaria/salao niches.
Usa servicos diferentes dos usados hoje para cada niche.
"""
import os, sys, json, time, datetime, requests

sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w7.json'
PREV_WAVES = [
    'previews/wave-2026-06-20-w1.json', 'previews/wave-2026-06-20-w2.json',
    'previews/wave-2026-06-20-w3.json', 'previews/wave-2026-06-20-w4.json',
    'previews/wave-2026-06-20-w5.json', 'previews/wave-2026-06-20-w6.json',
]

SEGMENTS = [
    dict(seg='academia', search='academia', service='Apps',
         hook='E se seus alunos pudessem ver o hor&aacute;rio das aulas, marcar avalia&ccedil;&otilde;es e renovar a matr&iacute;cula pelo celular?',
         subject='Um app para os alunos da {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app pr&oacute;prio da academia permite que alunos vejam hor&aacute;rios, acompanhem sua evolu&ccedil;&atilde;o, recebam dicas de treino e renovem a matr&iacute;cula &mdash; aumenta a retenção e diferencia sua academia.',
         bullets=['Hor&aacute;rios de aulas e agendamento de personal',
                  'Acompanhamento de evolução física e treinos',
                  'Notificações de promo&ccedil;&otilde;es e eventos',
                  'Renova&ccedil;&atilde;o de matrícula pelo celular'],
         wa='Ola! Quero um diagnostico rapido de um app para minha academia',
         img1='A Brazilian gym trainer showing a fitness app on a tablet to a member in a modern gym, realistic editorial photo',
         img2='A Brazilian gym member using a workout tracking app on a smartphone during training, realistic editorial photo'),
    dict(seg='supermercado', search='supermercado', service='Sites',
         hook='Seu supermercado tem um site com ofertas da semana para atrair mais clientes online?',
         subject='Um site com ofertas e encarte digital para o {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Supermercados que publicam suas ofertas online atraem mais clientes &mdash; um site com o encarte digital da semana, hor&aacute;rios, localiza&ccedil;&atilde;o e WhatsApp para pedidos aumenta o fluxo de clientes.',
         bullets=['Encarte digital com ofertas da semana',
                  'Hor&aacute;rios de funcionamento e localiza&ccedil;&atilde;o',
                  'Aparecer no Google de quem busca supermercado na cidade',
                  'WhatsApp para pedidos e delivery'],
         wa='Ola! Quero um diagnostico rapido de um site para meu supermercado',
         img1='A Brazilian supermarket manager showing a supermarket website with weekly promotions on a tablet, realistic editorial photo',
         img2='A well-organized Brazilian supermarket with colorful product displays and happy customers, realistic editorial photo'),
    dict(seg='estetica', search='estetica', service='Landing pages',
         hook='Uma landing page transforma quem pesquisa procedimentos estéticos em clientes agendados.',
         subject='Uma pagina de captacao de clientes para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages. Cl&iacute;entes buscam procedimentos est&eacute;ticos no Google e no Instagram &mdash; uma p&aacute;gina que mostra seus servi&ccedil;os, fotos do antes/depois e tem WhatsApp para agendamento converte muito mais que s&oacute; um perfil social.',
         bullets=['Galeria de procedimentos e fotos antes/depois',
                  'Lista de servi&ccedil;os e pacotes ofertados',
                  'Depoimentos de clientes satisfeitas',
                  'WhatsApp para agendamento direto'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para minha clinica estetica',
         img1='A Brazilian esthetician showing beauty treatment results on a professional landing page on a tablet in a modern beauty clinic, realistic editorial photo',
         img2='A modern Brazilian beauty clinic with elegant decor and a satisfied client after an aesthetic procedure, realistic editorial photo'),
    dict(seg='imobiliaria', search='imobiliaria', service='Apps',
         hook='E se seus clientes pudessem buscar im&oacute;veis e agendar visitas diretamente pelo celular?',
         subject='Um app de imoveis para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app pr&oacute;prio permite que seus clientes pesquisem im&oacute;veis pelo celular com fotos, v&iacute;deos, localiza&ccedil;&atilde;o e agendamento de visita &mdash; facilita a jornada do comprador e destaca sua imobili&aacute;ria.',
         bullets=['Cat&aacute;logo de im&oacute;veis com fotos e v&iacute;deos',
                  'Filtros por pre&ccedil;o, bairro, n&uacute;mero de quartos',
                  'Agendamento de visita com o corretor',
                  'Notifica&ccedil;&otilde;es de novos im&oacute;veis do interesse do cliente'],
         wa='Ola! Quero um diagnostico rapido de um app de imoveis para minha imobiliaria',
         img1='A Brazilian real estate agent showing property listings on a custom mobile app on a tablet to a young couple, realistic editorial photo',
         img2='A Brazilian real estate office with a professional agent at a desk with property listings on a computer, realistic editorial photo'),
    dict(seg='salao', search='salao', service='Sites',
         hook='Sua clientes encontram seu sal&atilde;o de beleza quando pesquisam no Google ou precisam de indica&ccedil;&atilde;o?',
         subject='Um site profissional para o {{empresa}} que atrai novas clientes?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Clientes pesquisam sal&otilde;es no Google antes de agendar &mdash; um site bonito com fotos do trabalho, servi&ccedil;os, pre&ccedil;os e agendamento pelo WhatsApp atrai mais clientes novas sem precisar depender s&oacute; de indica&ccedil;&atilde;o.',
         bullets=['Galeria de fotos dos seus trabalhos de beleza',
                  'Lista de servi&ccedil;os e tabela de pre&ccedil;os',
                  'Aparecer no Google de quem busca sal&atilde;o na cidade',
                  'Agendamento pelo WhatsApp direto do site'],
         wa='Ola! Quero um diagnostico rapido de um site para meu salao de beleza',
         img1='A Brazilian hair salon owner showing a professional salon website on a laptop with a smiling hairstylist in the background, realistic editorial photo',
         img2='A modern Brazilian beauty salon with stylish decor, mirrors and professional hairstylists working, realistic editorial photo'),
]


def load_used_today():
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
    print(f'[{ts()}] login OK. WAVE 7 FINAL (20/06): academia-apps/supermercado/estetica/imobiliaria/salao')
    os.makedirs(DIR + 'previews', exist_ok=True)

    used_today = load_used_today()
    print(f'[{ts()}] Pre-seeded {len(used_today)} emails from w1-w6')

    seen_emails = set(used_today)
    seen_domains = {e.split('@', 1)[1] for e in used_today if '@' in e}
    seen_company = set()
    import re
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

    want = PER_PROVIDER * len(HEALTHY)
    wave = {'date': '2026-06-20', 'wave': 7, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:15} {cfg["service"]:22} | exam={examined:4} | sel={len(leads)}')
        if not leads:
            print(f'   (0 leads — pulando)')
            continue

        print(f'[{ts()}]   img1...'); img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1={str(img1)[:60]}')
        print(f'[{ts()}]   img2...'); img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img2={str(img2)[:60]}')

        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w7'
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
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

        for ld in leads: ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 7 FINAL preview saved: {total} leads')
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:22} {len(s["leads"])} leads -> cid {s["campaignId"][:8]}...')
    return wave


def do_send():
    load_env(); H = login()
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); sys.exit(1)

    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] SEND WAVE 7 - {len(wave["segments"])} segmentos')
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
                print(f'   (ja enfileirado)'); continue
            print(f'   !!! QUEUE rejeitado: {err}'); break

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
                import pyodbc
                conn = pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};SERVER=sql1002.site4now.net;DATABASE=db_aaf0a8_diaxcrm;UID=db_aaf0a8_diaxcrm_admin;PWD=Alexandre10#;Encrypt=yes;TrustServerCertificate=yes;', timeout=30)
                cur = conn.cursor()
                cur.execute('SELECT COUNT(*) FROM email_queue_items WHERE campaign_id=? AND status=2', cid)
                db_sent = cur.fetchone()[0]
                conn.close()
                if db_sent > 0:
                    print(f'[{ts()}] breaker OK (DB confirms {db_sent} sent)')
                else:
                    print(f'\n[{ts()}] !!! BREAKER ABERTO'); sys.exit(1)
            else:
                print(f'[{ts()}] breaker OK\n')

        if i < len(wave['segments']) - 1:
            time.sleep(15)
        total_sent += jr.get('queuedCount', 0)

    time.sleep(20)
    print(f'\n[{ts()}] === WAVE 7 QUEUED: {total_sent} ===')
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'build'
    if cmd == 'build': do_build()
    elif cmd == 'send': do_send()
    else: print('Usage: python fire_w7_20.py [build|send]')
