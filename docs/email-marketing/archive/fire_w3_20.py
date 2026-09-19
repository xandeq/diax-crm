# -*- coding: utf-8 -*-
"""Cold email AQ - 2026-06-20 WAVE 3. Reusa funcoes de fire_today_20.py.
Novos nichos nao usados em 19/06 nem 20/06 w1/w2:
joalheria->Sites, imobiliaria->Landing, odontologia->Apps, autoescola->Software.
Uso: python fire_w3_20.py build | send
"""
import os, sys, re, json, time, datetime, importlib.util
spec = importlib.util.spec_from_file_location('ft', 'D:/claude-code/diax-crm/docs/email-marketing/fire_today_20.py')
ft = importlib.util.module_from_spec(spec)
_argv = sys.argv; sys.argv = ['x']; spec.loader.exec_module(ft); sys.argv = _argv

base = ft.base; HEALTHY = ft.HEALTHY; DIR = ft.DIR
WAVE_FILE = DIR + 'previews/wave-2026-06-20-w3.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-20.json'
RESULT = DIR + 'result-wave3-2026-06-20.json'
def ts(): return ft.ts()

NOISE = re.compile(r'\b(melhores|guia|lista|ranking|top\s*\d|encontre|perto de mim|onde\s|os\s+\d+)\b', re.I)

SEGMENTS = [
    dict(seg='joalheria', search='joalheria', service='Sites',
         hook='Sua joalheria aparece no Google quando buscam anel ou aliança na cidade?',
         subject='A {{empresa}} aparece no Google de quem busca joias na sua cidade?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quem quer uma joia especial pesquisa no Google &mdash; uma joalheria com site profissional aparece primeiro e inspira confian&ccedil;a.',
         bullets=['Cat&aacute;logo de joias com fotos que encantam', 'Aparecer no Google de quem busca joalheria na cidade',
                  'WhatsApp integrado para orçamento direto', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para minha joalheria',
         img1='A Brazilian jewelry store owner showing a professional website on a laptop at an elegant jewelry store counter, jewelry displays, realistic editorial photo',
         img2='An elegant Brazilian jewelry store interior with sparkling displays and warm lighting, realistic editorial photo'),
    dict(seg='imobiliaria', search='imobiliaria', service='Landing pages',
         hook='Cada im&oacute;vel em lan&ccedil;amento merece uma p&aacute;gina que capta leads quentes.',
         subject='Uma landing page de lan&ccedil;amento para os im&oacute;veis da {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages que convertem. Uma p&aacute;gina dedicada por lan&ccedil;amento capta leads qualificados e transforma visita em contato direto.',
         bullets=['Landing focada em captar leads para o lan&ccedil;amento', 'Tour virtual e galeria de fotos para despertar desejo',
                  'Formul&aacute;rio e WhatsApp para atendimento direto', 'Integra com seus an&uacute;ncios e pixels de remarketing'],
         wa='Ola! Quero um diagnostico rapido de uma landing page de imovel para minha imobiliaria',
         img1='A Brazilian real estate agent showing a property landing page on a tablet in a modern real estate office, realistic editorial photo',
         img2='A modern Brazilian real estate office with agents working and property listings on screens, realistic editorial photo'),
    dict(seg='odontologia', search='odontologia', service='Apps',
         hook='E se seus pacientes agendassem a consulta e recebessem lembrete pelo app da sua cl&iacute;nica?',
         subject='Um app de agendamento com a marca da {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma cl&iacute;nica odontol&oacute;gica com app pr&oacute;prio facilita o agendamento, reduz faltas e fideliza o paciente na sua marca.',
         bullets=['Agendamento de consultas odontol&oacute;gicas direto no app', 'Lembretes autom&aacute;ticos para reduzir falta',
                  'Hist&oacute;rico e tratamentos do paciente organizados', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app para minha clinica odontologica',
         img1='A Brazilian dentist showing a patient booking app on a smartphone at a modern dental clinic reception, realistic editorial photo',
         img2='A modern bright Brazilian dental clinic interior with professional equipment and welcoming atmosphere, realistic editorial photo'),
    dict(seg='autoescola', search='autoescola', service='Software sob demanda',
         hook='Sua autoescola controla alunos, vencimentos e aulas em planilhas soltas?',
         subject='Um sistema de gest&atilde;o sob medida para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar alunos, agendamento de aulas e controle financeiro num sistema s&oacute; elimina retrabalho e melhora o faturamento.',
         bullets=['Cadastro e acompanhamento de cada aluno', 'Agendamento de aulas pr&aacute;ticas sem conflito de hor&aacute;rio',
                  'Controle de vencimentos, pagamentos e recibos', 'Feito sob medida para o seu fluxo'],
         wa='Ola! Quero um diagnostico rapido de um sistema para minha autoescola',
         img1='A Brazilian driving school manager using management software on a computer in a modern office, realistic editorial photo',
         img2='A Brazilian driving school with cars and students in a parking lot, bright daylight, realistic editorial photo'),
]

def seed_dedup():
    se, sd, sc = set(), set(), set()
    for wf in ['wave-2026-06-20-w1.json', 'wave-2026-06-20-w2.json']:
        try:
            w = json.load(open(DIR + 'previews/' + wf, encoding='utf-8'))
            for s in w['segments']:
                for l in s['leads']:
                    se.add(l['email'].lower()); sd.add(l['email'].split('@', 1)[1])
                    ck = re.sub(r'[^a-z0-9]', '', (l['company'] or '').lower())
                    if ck: sc.add(ck)
        except Exception as e:
            print('  (seed', wf, 'falhou:', e, ')')
    return se, sd, sc

def do_build():
    ft.load_env(); H = ft.login()
    print(f'[{ts()}] WAVE 3 (20/06): joalheria/imobiliaria/odontologia/autoescola (novos nichos)')
    seen_e, seen_d, seen_c = seed_dedup()
    want = ft.PER_PROVIDER * len(HEALTHY)
    wave = {'date': '2026-06-20', 'wave': 3, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = ft.fetch_leads(cfg, H, seen_e, seen_d, seen_c, want)
        clean = [l for l in leads if not NOISE.search(str(l.get('company') or ''))]
        dropped = len(leads) - len(clean)
        leads = ft.assign_providers(clean)
        print(f'[{ts()}] {cfg["seg"]:14}{cfg["service"]:20} | examinados={examined:4} | sel={len(leads)} (ruido removido={dropped})')
        if not leads:
            print('   (0 leads — pulando)'); continue
        print(f'[{ts()}]   gerando imagens...')
        img1 = ft.gen_image(cfg['img1'], H); img2 = ft.gen_image(cfg['img2'], H)
        if not img1 or not img2:
            print(f'[{ts()}]   ERRO: gen_image None (img1={img1} img2={img2}) — pulando'); continue
        html = ft.build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave20w3-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        cr = ft.requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                              json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-20 w3',
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
    print(f"\n[{ts()}] enfileirado={exp}. Monitorando worker (>=5min)...")
    for loop in range(48):
        time.sleep(10)
        tot = 0; ln = []
        for t in track:
            s, d, f = counts(t['cid'], H); t['sent'] = s; t['delivered'] = d; t['failed'] = f; tot += s
            ln.append(f"{t['seg'][:5]}={s}/{d}/{f}")
        print(f'  +{(loop+1)*10:3}s sent={tot}/{exp} | ' + ' '.join(ln), flush=True)
        if tot >= exp: print('>>> dispatch completo'); break
    ph = {p['provider']: p for p in ft.requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    out = {'date': '2026-06-20', 'wave': 3, 'total_sent': sum(t.get('sent', 0) for t in track),
           'total_delivered': sum(t.get('delivered', 0) for t in track), 'segments': track, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = json.load(open(LEDGER, encoding='utf-8')) if os.path.exists(LEDGER) else {'date': '2026-06-20'}
    led['updated_at'] = ts()
    led['providers'] = {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'], 'dailyRemaining': ph[n].get('dailyRemaining'), 'hourlyRemaining': ph[n].get('hourlyRemaining')} for n in HEALTHY}
    led['wave3'] = track
    json.dump(led, open(LEDGER, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    print('\nTOTAL enviado w3:', out['total_sent'], '| LIMITES dia:', {n: f"{ph[n]['sentToday']}/90" for n in HEALTHY})

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'build'
    (do_build if mode == 'build' else do_send)()
