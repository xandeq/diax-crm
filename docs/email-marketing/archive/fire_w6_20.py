# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-20 - WAVE 6.
Combos: advocacia->Software, medico->Landing, padaria->Sites, contabilidade->Apps, escola->Apps.
Todos sao servicos novos para esses nichos (nao repetidos hoje).
"""
import os, sys, json, time, datetime, requests

sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w6.json'
PREV_WAVES = [
    'previews/wave-2026-06-20-w1.json', 'previews/wave-2026-06-20-w2.json',
    'previews/wave-2026-06-20-w3.json', 'previews/wave-2026-06-20-w4.json',
    'previews/wave-2026-06-20-w5.json',
]

SEGMENTS = [
    dict(seg='advocacia', search='advocacia', service='Software sob demanda',
         hook='Um software personalizado pode transformar a gest&atilde;o jur&iacute;dica do seu escrit&oacute;rio.',
         subject='Um sistema sob medida para o escritorio {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo software sob demanda. Escrit&oacute;rios de advocacia perdem tempo com planilhas e sistemas gen&eacute;ricos &mdash; um sistema pr&oacute;prio para controle de processos, prazos, cliente e faturamento elimina retrabalho e aumenta a produtividade.',
         bullets=['Controle de processos e prazos jur&iacute;dicos',
                  'Gest&atilde;o de clientes e hist&oacute;rico de atendimentos',
                  'Gera&ccedil;&atilde;o autom&aacute;tica de documentos e contratos',
                  'Dashboard financeiro com honorários e recebimentos'],
         wa='Ola! Quero um diagnostico rapido de um sistema sob demanda para meu escritorio de advocacia',
         img1='A Brazilian lawyer reviewing legal documents on a custom software dashboard on a laptop in a modern law office, realistic editorial photo',
         img2='A professional Brazilian law office with organized legal files and a lawyer working on a computer, realistic editorial photo'),
    dict(seg='medico', search='medico', service='Landing pages',
         hook='Uma landing page profissional transforma a pesquisa online em consultas agendadas para o seu consultório.',
         subject='Uma pagina de captacao de pacientes para o Dr. {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages. Uma p&aacute;gina objetiva com sua especialidade, diferenciais, fotos e WhatsApp para contato converte muito mais que apenas presença no Google &mdash; ela convence o paciente a ligar antes de comparar concorrentes.',
         bullets=['Especialidade e diferenciais do seu atendimento',
                  'Depoimentos e avalia&ccedil;&otilde;es de pacientes',
                  'WhatsApp para agendamento direto da p&aacute;gina',
                  'R&aacute;pida, responsiva e otimizada para Google'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para meu consultorio medico',
         img1='A Brazilian doctor smiling with a professional medical landing page on a computer screen at a modern clinic, realistic editorial photo',
         img2='A Brazilian patient scheduling a medical appointment using a smartphone in a bright clinic waiting room, realistic editorial photo'),
    dict(seg='padaria', search='padaria', service='Sites',
         hook='Sua padaria tem site para mostrar o cardápio, horários e promoções para quem pesquisa no Google?',
         subject='Um site profissional para a {{empresa}} que atrai mais clientes?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quando algu&eacute;m procura &ldquo;padaria perto de mim&rdquo; no Google, aparecer com um site que mostra seu card&aacute;pio, fotos, hor&aacute;rios e localiza&ccedil;&atilde;o faz toda a diferen&ccedil;a para trazer mais clientes.',
         bullets=['Card&aacute;pio digital com fotos dos produtos',
                  'Hor&aacute;rios de funcionamento e localiza&ccedil;&atilde;o',
                  'Aparecer no Google para quem busca padaria na sua cidade',
                  'Contato pelo WhatsApp direto do site'],
         wa='Ola! Quero um diagnostico rapido de um site para minha padaria',
         img1='A Brazilian bakery owner showing a professional bakery website on a tablet with fresh bread and pastries in the background, realistic editorial photo',
         img2='A cozy Brazilian bakery with freshly baked goods displayed, warm lighting and customers, realistic editorial photo'),
    dict(seg='contabilidade', search='contabilidade', service='Apps',
         hook='E se seus clientes pudessem enviar documentos fiscais e ver relatórios direto pelo celular?',
         subject='Um app de comunicacao com clientes para o escritorio {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app pr&oacute;prio para seu escrit&oacute;rio cont&aacute;bil permite que clientes enviem documentos, recebam not&iacute;cias e visualizem relat&oacute;rios &mdash; reduz WhatsApp e email, aumenta a percep&ccedil;&atilde;o de profissionalismo.',
         bullets=['Envio de documentos e notas fiscais pelo celular',
                  'Avisos de obriga&ccedil;&otilde;es e prazos fiscais',
                  'Acesso a relat&oacute;rios cont&aacute;beis atualizados',
                  'Comunica&ccedil;&atilde;o direta escrit&oacute;rio-cliente'],
         wa='Ola! Quero um diagnostico rapido de um app para meu escritorio de contabilidade',
         img1='A Brazilian accountant presenting a custom accounting app on a tablet to a business client in a professional office, realistic editorial photo',
         img2='A Brazilian entrepreneur using a mobile accounting app to send documents to an accounting firm, realistic editorial photo'),
    dict(seg='escola', search='escola', service='Apps',
         hook='E se pais e alunos pudessem acompanhar notas, tarefas e recados da {{empresa}} pelo celular?',
         subject='Um app escolar para conectar a {{empresa}} com pais e alunos?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app pr&oacute;prio da escola permite que pais acompanhem notas, recebam comunicados, vejam o card&aacute;pio e confirmem presen&ccedil;a em eventos &mdash; moderniza a comunica&ccedil;&atilde;o e diferencia sua institui&ccedil;&atilde;o.',
         bullets=['Notas, atividades e boletins no celular dos pais',
                  'Comunicados e avisos instant&acirc;neos via push',
                  'Hor&aacute;rios, card&aacute;pio e calend&aacute;rio acad&ecirc;mico',
                  'Confirma&ccedil;&atilde;o de presen&ccedil;a em eventos e reunio&otilde;es'],
         wa='Ola! Quero um diagnostico rapido de um app escolar',
         img1='A Brazilian school teacher showing a school communication app on a tablet to happy parents at a school meeting, realistic editorial photo',
         img2='A Brazilian parent using a mobile school app on a smartphone to check their child grades and school announcements, realistic editorial photo'),
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
    print(f'[{ts()}] login OK. WAVE 6 (20/06): advocacia-sw/medico-landing/padaria-sites/contabilidade-apps/escola-apps')
    os.makedirs(DIR + 'previews', exist_ok=True)

    used_today = load_used_today()
    print(f'[{ts()}] Pre-seeded {len(used_today)} emails from w1-w5')

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
    wave = {'date': '2026-06-20', 'wave': 6, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:15} {cfg["service"]:22} | exam={examined:4} | sel={len(leads)}')
        if not leads:
            print(f'   (0 leads — pulando {cfg["seg"]}×{cfg["service"]})')
            continue

        print(f'[{ts()}]   img1...'); img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1={str(img1)[:60]}')
        print(f'[{ts()}]   img2...'); img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img2={str(img2)[:60]}')

        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w6'
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
    print(f'\n[{ts()}] Wave 6 preview saved: {total} leads')
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:22} {len(s["leads"])} leads -> cid {s["campaignId"][:8]}...')
    return wave


def do_send():
    load_env(); H = login()
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); sys.exit(1)

    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] SEND WAVE 6 - {len(wave["segments"])} segmentos')
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
                print(f'   (ja enfileirado — pulando)'); continue
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
                # Check DB directly
                import pyodbc
                conn = pyodbc.connect('DRIVER={ODBC Driver 17 for SQL Server};SERVER=sql1002.site4now.net;DATABASE=db_aaf0a8_diaxcrm;UID=db_aaf0a8_diaxcrm_admin;PWD=Alexandre10#;Encrypt=yes;TrustServerCertificate=yes;', timeout=30)
                cur = conn.cursor()
                cur.execute('SELECT COUNT(*) FROM email_queue_items WHERE campaign_id=? AND status=2', cid)
                db_sent = cur.fetchone()[0]
                conn.close()
                if db_sent > 0:
                    print(f'[{ts()}] breaker OK (DB confirms {db_sent} sent, API lag)')
                else:
                    print(f'\n[{ts()}] !!! BREAKER ABERTO - 0 enviados em 420s'); sys.exit(1)
            else:
                print(f'[{ts()}] breaker OK\n')

        if i < len(wave['segments']) - 1:
            time.sleep(15)
        total_sent += jr.get('queuedCount', 0)

    time.sleep(20)
    print(f'\n[{ts()}] === WAVE 6 QUEUED: {total_sent} ===')
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'build'
    if cmd == 'build': do_build()
    elif cmd == 'send': do_send()
    else: print('Usage: python fire_w6_20.py [build|send]')
