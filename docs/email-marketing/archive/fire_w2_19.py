# -*- coding: utf-8 -*-
"""Cold email AQ - 2026-06-19 WAVE 2. Reusa funcoes de fire_today_19.py.
Segmentos (cada um um servico): otica->Sites, construtora->Software, restaurante2->Apps, marketing->Landing pages
(marketing substitui agencia_marketing, que tem 0 leads). Sonda breaker >=5min (corrigida).
Uso: python fire_w2_19.py build   |   python fire_w2_19.py send
"""
import os, sys, json, time, datetime, importlib.util
spec = importlib.util.spec_from_file_location('ft', 'D:/claude-code/diax-crm/docs/email-marketing/fire_today_19.py')
ft = importlib.util.module_from_spec(spec)
_argv = sys.argv; sys.argv = ['x']; spec.loader.exec_module(ft); sys.argv = _argv

base = ft.base; HEALTHY = ft.HEALTHY; DIR = ft.DIR
WAVE_FILE = DIR + 'previews/wave-2026-06-19-w2.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-19.json'
RESULT = DIR + 'result-wave2-2026-06-19.json'
def ts(): return ft.ts()

SEGMENTS = [
    dict(seg='otica', search='otica', service='Sites',
         hook='Sua &oacute;tica aparece quando procuram &oacute;culos na sua cidade?',
         subject='A {{empresa}} aparece no Google de quem procura &oacute;culos?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quem precisa de &oacute;culos pesquisa no Google &mdash; uma &oacute;tica com site profissional aparece primeiro e passa confian&ccedil;a.',
         bullets=['Cat&aacute;logo de arma&ccedil;&otilde;es e lentes sempre atualizado', 'Aparecer no Google de quem busca &oacute;tica na cidade',
                  'Agendamento de exame de vista pelo site', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para minha otica',
         img1='A Brazilian optician shop owner using a website on a laptop at the counter of a modern eyewear store, glasses on display, realistic editorial photo',
         img2='A modern Brazilian optical eyewear store interior with glasses displays and bright light, realistic editorial photo'),
    dict(seg='construtora', search='construtora', service='Software sob demanda',
         hook='Sua construtora controla obras e custos em planilhas soltas?',
         subject='Um sistema sob medida para a sua construtora?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar obras, cronogramas e custos num sistema s&oacute; reduz erro e desperd&iacute;cio.',
         bullets=['Cronograma e status de cada obra em tempo real', 'Controle de custos, materiais e fornecedores',
                  'Portal do cliente para acompanhar a obra', 'Feito sob medida para o seu fluxo'],
         wa='Ola! Quero um diagnostico rapido de um sistema para minha construtora',
         img1='A Brazilian construction company manager using project management software on a computer in a construction site office, blueprints nearby, realistic editorial photo',
         img2='A Brazilian construction site with engineers and workers in helmets reviewing plans, daylight, realistic editorial photo'),
    dict(seg='restaurante2', search='restaurante', service='Apps',
         hook='E se seus clientes pedissem pelo app do seu restaurante (sem taxa de marketplace)?',
         subject='Um app de pedidos com a marca do seu restaurante (sem comiss&atilde;o)?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um app pr&oacute;prio de pedidos elimina a comiss&atilde;o do marketplace e fideliza o cliente na sua marca.',
         bullets=['Pedidos e delivery direto, sem comiss&atilde;o de marketplace', 'Programa de fidelidade com a sua marca',
                  'Notifica&ccedil;&otilde;es de promo&ccedil;&otilde;es para o cliente', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app de pedidos para meu restaurante',
         img1='A Brazilian restaurant owner using a food ordering app on a smartphone inside a cozy modern restaurant, realistic editorial photo',
         img2='A cozy modern Brazilian restaurant interior with customers dining, warm light, realistic editorial photo'),
    dict(seg='marketing', search='marketing', service='Landing pages',
         hook='Suas campanhas merecem landing pages que convertem de verdade.',
         subject='Landing pages que convertem para as campanhas da {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages de alta convers&atilde;o. Uma p&aacute;gina feita para converter aproveita melhor cada real investido em an&uacute;ncio.',
         bullets=['Landing focada em convers&atilde;o, n&atilde;o s&oacute; bonita', 'Estrutura pronta para testes A/B',
                  'Integra&ccedil;&atilde;o com os seus an&uacute;ncios e pixels', 'Entrega r&aacute;pida para a sua pr&oacute;xima campanha'],
         wa='Ola! Quero um diagnostico rapido de uma landing page de alta conversao',
         img1='A Brazilian marketing professional reviewing a high-converting landing page on a laptop in a modern agency office, analytics on screen, realistic editorial photo',
         img2='A modern Brazilian marketing agency office, team collaborating with laptops and charts on a wall screen, realistic editorial photo'),
]

def do_build():
    ft.load_env(); H = ft.login()
    print(f'[{ts()}] WAVE 2 (19/06): otica/construtora/restaurante2/marketing')
    seen_e, seen_d, seen_c = set(), set(), set()
    # seed dedup com wave 1 (nao repetir leads/dominios)
    try:
        w1 = json.load(open(DIR + 'previews/wave-2026-06-19-w1.json', encoding='utf-8'))
        import re
        for s in w1['segments']:
            for l in s['leads']:
                seen_e.add(l['email'].lower()); seen_d.add(l['email'].split('@', 1)[1])
                ck = re.sub(r'[^a-z0-9]', '', (l['company'] or '').lower())
                if ck: seen_c.add(ck)
    except Exception as e:
        print('  (sem seed wave1:', e, ')')
    want = ft.PER_PROVIDER * len(HEALTHY)
    wave = {'date': '2026-06-19', 'wave': 2, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = ft.fetch_leads(cfg, H, seen_e, seen_d, seen_c, want)
        leads = ft.assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:14}{cfg["service"]:20} | examinados={examined:4} | selecionados={len(leads)}')
        if not leads:
            print('   (0 leads — pulando segmento)'); continue
        print(f'[{ts()}]   gerando imagens...')
        img1 = ft.gen_image(cfg['img1'], H); img2 = ft.gen_image(cfg['img2'], H)
        html = ft.build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave19w2-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        cr = ft.requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                              json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-19 w2',
                                    'subject': cfg['subject'], 'bodyHtml': html})
        cid = cr.json().get('id')
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
        sc = ft.requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H, timeout=20, json={'scheduledAt': future.isoformat()})
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
    ft.ensure_breaker_closed(H)  # self-healing
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
    for loop in range(45):  # ~7.5min
        time.sleep(10)
        tot = 0; ln = []
        for t in track:
            s, d, f = counts(t['cid'], H); t['sent'] = s; t['delivered'] = d; t['failed'] = f; tot += s
            ln.append(f"{t['seg'][:5]}={s}/{d}/{f}")
        print(f'  +{(loop+1)*10:3}s sent={tot}/{exp} | ' + ' '.join(ln), flush=True)
        if tot >= exp: print('>>> dispatch completo'); break
    ph = {p['provider']: p for p in ft.requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    out = {'date': '2026-06-19', 'wave': 2, 'total_sent': sum(t.get('sent', 0) for t in track),
           'total_delivered': sum(t.get('delivered', 0) for t in track), 'segments': track, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = json.load(open(LEDGER, encoding='utf-8')) if os.path.exists(LEDGER) else {'date': '2026-06-19'}
    led['updated_at'] = ts()
    led['providers'] = {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'], 'dailyRemaining': ph[n].get('dailyRemaining'), 'hourlyRemaining': ph[n].get('hourlyRemaining')} for n in HEALTHY}
    led['wave2'] = track
    json.dump(led, open(LEDGER, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    print('\nTOTAL enviado w2:', out['total_sent'], '| LIMITES dia:', {n: f"{ph[n]['sentToday']}/90" for n in HEALTHY})
    print(f'Salvo: {RESULT} + ledger')

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'build'
    (do_build if mode == 'build' else do_send)()
