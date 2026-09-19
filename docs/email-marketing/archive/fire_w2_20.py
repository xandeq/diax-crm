# -*- coding: utf-8 -*-
"""Cold email AQ - 2026-06-20 WAVE 2. Reusa funcoes de fire_today_20.py.
Nichos com pools grandes, servico rotacionado vs ontem (20/06 w1) e vs 19/06:
clinica->Software, contabilidade->Sites, advocacia->Apps, academia->Landing.
Uso: python fire_w2_20.py build | send
"""
import os, sys, re, json, time, datetime, importlib.util
spec = importlib.util.spec_from_file_location('ft', 'D:/claude-code/diax-crm/docs/email-marketing/fire_today_20.py')
ft = importlib.util.module_from_spec(spec)
_argv = sys.argv; sys.argv = ['x']; spec.loader.exec_module(ft); sys.argv = _argv

base = ft.base; HEALTHY = ft.HEALTHY; DIR = ft.DIR
WAVE_FILE = DIR + 'previews/wave-2026-06-20-w2.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-20.json'
RESULT = DIR + 'result-wave2-2026-06-20.json'
def ts(): return ft.ts()

SEGMENTS = [
    dict(seg='clinica', search='clinica', service='Software sob demanda',
         hook='Sua cl&iacute;nica ainda usa planilhas para controlar pacientes e consultas?',
         subject='Um sistema de gest&atilde;o sob medida para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar pacientes, agendamentos e prontu&aacute;rios num sistema s&oacute; elimina retrabalho e melhora o atendimento.',
         bullets=['Prontu&aacute;rio eletr&ocirc;nico integrado ao agendamento', 'Controle financeiro e relat&oacute;rios autom&aacute;ticos',
                  'Portal do paciente para acompanhar consultas', 'Feito sob medida para o fluxo da sua cl&iacute;nica'],
         wa='Ola! Quero um diagnostico rapido de um sistema para minha clinica',
         img1='A Brazilian clinic administrator using patient management software on a computer in a modern medical office, realistic editorial photo',
         img2='A bright modern Brazilian medical clinic with organized reception and professional staff, realistic editorial photo'),
    dict(seg='contabilidade', search='contabil', service='Sites',
         hook='Quando um empreendedor busca contador, a {{empresa}} aparece no Google?',
         subject='Um site profissional para a {{empresa}} captar clientes pelo Google?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quem quer abrir empresa ou trocar de contador pesquisa no Google &mdash; um escritório com site profissional aparece primeiro e passa confian&ccedil;a.',
         bullets=['Site que apresenta servi&ccedil;os e diferenciais com clareza', 'Aparecer no Google de quem busca contador',
                  'Formul&aacute;rio de contato e WhatsApp direto', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para meu escritorio de contabilidade',
         img1='A Brazilian accountant showing a professional website on a laptop in a modern accounting office, realistic editorial photo',
         img2='A modern Brazilian accounting office with professionals working, organized and professional environment, realistic editorial photo'),
    dict(seg='advocacia', search='advocacia', service='Apps',
         hook='E se seus clientes acompanhassem os processos pelo app do seu escrit&oacute;rio?',
         subject='Um app de acompanhamento de processos para os clientes do {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um escrit&oacute;rio com app pr&oacute;prio diferencia, melhora a comunica&ccedil;&atilde;o e fideliza o cliente com sua marca.',
         bullets=['Portal do cliente para acompanhar processos em tempo real', 'Notifica&ccedil;&otilde;es de atualiza&ccedil;&atilde;o e prazos',
                  'Mensagens seguras entre adv. e cliente no app', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app para meu escritorio de advocacia',
         img1='A Brazilian lawyer showing a client portal app on a smartphone in a modern law office, realistic editorial photo',
         img2='A modern Brazilian law office interior with lawyers working at elegant desks, realistic editorial photo'),
    dict(seg='academia', search='academia', service='Landing pages',
         hook='Cada promo&ccedil;&atilde;o da sua academia merece uma p&aacute;gina que converte matr&iacute;culas.',
         subject='Uma landing page de matr&iacute;culas para a pr&oacute;xima promo&ccedil;&atilde;o da {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages que convertem. Uma p&aacute;gina focada em matr&iacute;cula aproveita cada real investido em an&uacute;ncio e converte mais.',
         bullets=['Landing focada em gerar matr&iacute;culas (n&atilde;o s&oacute; bonita)', 'Planos e diferenciais apresentados para converter',
                  'Bot&atilde;o de WhatsApp para fechar na hora', 'Pronta para a sua pr&oacute;xima campanha'],
         wa='Ola! Quero um diagnostico rapido de uma landing page de matriculas para minha academia',
         img1='A Brazilian gym owner showing a promotional landing page on a tablet at a modern fitness center, realistic editorial photo',
         img2='A vibrant modern Brazilian gym with people working out, energetic atmosphere, realistic editorial photo'),
]

def do_build():
    ft.load_env(); H = ft.login()
    print(f'[{ts()}] WAVE 2 (20/06): clinica/contabilidade/advocacia/academia (servicos rotacionados)')
    seen_e, seen_d, seen_c = set(), set(), set()
    try:
        w1 = json.load(open(DIR + 'previews/wave-2026-06-20-w1.json', encoding='utf-8'))
        for s in w1['segments']:
            for l in s['leads']:
                seen_e.add(l['email'].lower()); seen_d.add(l['email'].split('@', 1)[1])
                ck = re.sub(r'[^a-z0-9]', '', (l['company'] or '').lower())
                if ck: seen_c.add(ck)
    except Exception as e:
        print('  (sem seed wave1:', e, ')')
    want = ft.PER_PROVIDER * len(HEALTHY)
    wave = {'date': '2026-06-20', 'wave': 2, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = ft.fetch_leads(cfg, H, seen_e, seen_d, seen_c, want)
        leads = ft.assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:14}{cfg["service"]:20} | examinados={examined:4} | selecionados={len(leads)}')
        if not leads:
            print('   (0 leads — pulando segmento)'); continue
        print(f'[{ts()}]   gerando imagens...')
        img1 = ft.gen_image(cfg['img1'], H); img2 = ft.gen_image(cfg['img2'], H)
        if not img1 or not img2:
            print(f'[{ts()}]   ERRO: gen_image None (img1={img1} img2={img2}) — pulando'); continue
        html = ft.build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave20w2-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        cr = ft.requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                              json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-20 w2',
                                    'subject': cfg['subject'], 'bodyHtml': html})
        cid = cr.json().get('id')
        if not cid:
            print(f'[{ts()}]   ERRO CREATE {cr.status_code}: {cr.text[:200]} — pulando'); continue
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
        sc = ft.requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H, timeout=20, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')})
        st = ft.requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H, json={}, timeout=60)
        print(f'[{ts()}]   campanha {cid} | schedule {sc.status_code} | send-test {st.status_code}')
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'subject': cfg['subject'],
                                 'campaignId': cid, 'img1': img1, 'img2': img2, 'leads': leads,
                                 'by_provider': {p: sum(1 for l in leads if l['assignedProvider'] == p) for p in HEALTHY}})
    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    total = sum(len(s['leads']) for s in wave['segments'])
    bp = {p: sum(s['by_provider'][p] for s in wave['segments']) for p in HEALTHY}
    print(f'\n[{ts()}] DRY OK. TOTAL={total} | ' + ', '.join(f"{s['seg']}={len(s['leads'])}" for s in wave['segments']))
    print(f'  por provider (hora): {bp} (<=22/h)')
    print('  send-test SO admin. Rode "send" para disparar.')

def counts(cid, H):
    d = ft.requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

def do_send():
    ft.load_env(); H = ft.login()
    ft.ensure_breaker_closed(H)
    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    track = []
    for s in wave['segments']:
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']} for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = ft.requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60, json={'campaignId': s['campaignId'], 'leads': leads})
        jr = json.loads(qq.text)
        print(f"[{ts()}] {s['seg']:14}{s['service']:20} | QUEUE {qq.status_code} queued={jr.get('queuedCount')} skip={jr.get('skippedCount')} | {s['campaignId']}")
        track.append({'seg': s['seg'], 'cid': s['campaignId'], 'queued': jr.get('queuedCount'), 'expected': len(leads)})
        time.sleep(2)
    exp = sum(t['expected'] for t in track)
    print(f"\n[{ts()}] enfileirado={exp}. Monitorando worker (ciclo 5min, sonda >=5min)...")
    for loop in range(45):
        time.sleep(10)
        tot = 0; ln = []
        for t in track:
            s, d, f = counts(t['cid'], H); t['sent'] = s; t['delivered'] = d; t['failed'] = f; tot += s
            ln.append(f"{t['seg'][:5]}={s}/{d}/{f}")
        print(f'  +{(loop+1)*10:3}s sent={tot}/{exp} | ' + ' '.join(ln), flush=True)
        if tot >= exp: print('>>> dispatch completo'); break
    ph = {p['provider']: p for p in ft.requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    out = {'date': '2026-06-20', 'wave': 2, 'total_sent': sum(t.get('sent', 0) for t in track),
           'total_delivered': sum(t.get('delivered', 0) for t in track), 'segments': track, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = json.load(open(LEDGER, encoding='utf-8')) if os.path.exists(LEDGER) else {'date': '2026-06-20'}
    led['updated_at'] = ts()
    led['providers'] = {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'], 'dailyRemaining': ph[n].get('dailyRemaining'), 'hourlyRemaining': ph[n].get('hourlyRemaining')} for n in HEALTHY}
    led['wave2'] = track
    json.dump(led, open(LEDGER, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    print('\nTOTAL enviado w2:', out['total_sent'], '| LIMITES dia:', {n: f"{ph[n]['sentToday']}/90" for n in HEALTHY})
    print(f'Salvo: {RESULT} + ledger')

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'build'
    (do_build if mode == 'build' else do_send)()
