# -*- coding: utf-8 -*-
"""Cold email AQ - 2026-06-19 WAVE 3. Reusa funcoes de fire_today_19.py.
Pools grandes ja contatados 17/06, mas SERVICO DIFERENTE: clinica->Apps, contabilidade->Landing,
advocacia->Software, academia->Sites. Leads frescos nunca-contatados (filtro emailSentCount==0 exclui os de 17/06).
FIX no ar: 429 tolerado + endpoint de reset -> Resend DE VOLTA (4 providers). Sonda >=5min.
Uso: python fire_w3_19.py build | send
"""
import os, sys, re, json, time, datetime, importlib.util
spec = importlib.util.spec_from_file_location('ft', 'D:/claude-code/diax-crm/docs/email-marketing/fire_today_19.py')
ft = importlib.util.module_from_spec(spec)
_argv = sys.argv; sys.argv = ['x']; spec.loader.exec_module(ft); sys.argv = _argv

base = ft.base; HEALTHY = ft.HEALTHY; DIR = ft.DIR   # HEALTHY = 4 providers (Resend incluso)
WAVE_FILE = DIR + 'previews/wave-2026-06-19-w3.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-19.json'
RESULT = DIR + 'result-wave3-2026-06-19.json'
def ts(): return ft.ts()

# Ruido de scraping (listicle/diretorio) a excluir do envio
NOISE = re.compile(r'\b(melhores|guia|lista|ranking|top\s*\d|encontre|perto de mim|onde\s|os\s+\d+)\b', re.I)

SEGMENTS = [
    dict(seg='clinica', search='clinica', service='Apps',
         hook='E se seus pacientes agendassem a consulta pelo app da sua cl&iacute;nica?',
         subject='Um app de agendamento com a marca da sua cl&iacute;nica?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma cl&iacute;nica com app pr&oacute;prio facilita o agendamento, reduz faltas e fideliza o paciente na sua marca.',
         bullets=['Agendamento de consultas direto no app', 'Lembretes autom&aacute;ticos para reduzir falta',
                  'Hist&oacute;rico e ficha do paciente organizados', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app para minha clinica',
         img1='A Brazilian clinic receptionist using a booking app on a tablet at a modern medical clinic reception, realistic editorial photo',
         img2='A modern Brazilian medical clinic interior, patients in a bright waiting room, realistic editorial photo'),
    dict(seg='contabilidade', search='contabil', service='Landing pages',
         hook='Sua contabilidade capta clientes com uma p&aacute;gina que converte?',
         subject='Uma landing page que capta clientes para a {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages que convertem. Uma p&aacute;gina focada em captar (abrir empresa, trocar de contador) transforma busca em cliente.',
         bullets=['Landing focada em captar leads qualificados', 'Formul&aacute;rio e WhatsApp para contato direto',
                  'Prova social e servi&ccedil;os com clareza', 'Aparecer no Google de quem busca contador'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para minha contabilidade',
         img1='A Brazilian accountant reviewing a high-converting landing page on a laptop in a modern accounting office, realistic editorial photo',
         img2='A modern Brazilian accounting office, professionals working with documents and computers, realistic editorial photo'),
    dict(seg='advocacia', search='advocacia', service='Software sob demanda',
         hook='Seu escrit&oacute;rio de advocacia controla prazos e processos em planilhas?',
         subject='Um sistema sob medida para o seu escrit&oacute;rio de advocacia?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar processos, prazos e clientes num sistema s&oacute; reduz risco de perder prazo e retrabalho.',
         bullets=['Controle de prazos e andamento de processos', 'Agenda de audi&ecirc;ncias e compromissos',
                  'Portal do cliente para acompanhar o caso', 'Feito sob medida para o seu fluxo'],
         wa='Ola! Quero um diagnostico rapido de um sistema para meu escritorio de advocacia',
         img1='A Brazilian lawyer using legal case management software on a computer in a law office, law books on shelves, realistic editorial photo',
         img2='A modern Brazilian law office interior, lawyers working at desks, realistic editorial photo'),
    dict(seg='academia', search='academia', service='Sites',
         hook='Quem procura academia na sua regi&atilde;o encontra a sua no Google?',
         subject='A {{empresa}} aparece no Google de quem procura academia?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quem quer treinar pesquisa no Google &mdash; uma academia com site profissional aparece primeiro e converte mais matr&iacute;culas.',
         bullets=['Planos e modalidades apresentados com clareza', 'Aparecer no Google de quem busca academia na regi&atilde;o',
                  'Matr&iacute;cula e contato direto pelo site', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para minha academia',
         img1='A Brazilian gym owner showing the gym website on a tablet at a modern fitness center reception, realistic editorial photo',
         img2='A modern Brazilian gym interior with people working out, energetic light, realistic editorial photo'),
]

def seed_dedup():
    se, sd, sc = set(), set(), set()
    for wf in ['wave-2026-06-19-w1.json', 'wave-2026-06-19-w2.json']:
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
    print(f'[{ts()}] WAVE 3 (19/06): clinica/contabilidade/advocacia/academia (servicos novos vs 17/06)')
    seen_e, seen_d, seen_c = seed_dedup()
    want = ft.PER_PROVIDER * len(HEALTHY)  # 20/seg
    wave = {'date': '2026-06-19', 'wave': 3, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = ft.fetch_leads(cfg, H, seen_e, seen_d, seen_c, want)
        # filtro de ruido (listicle/diretorio)
        clean = [l for l in leads if not NOISE.search(str(l.get('company') or ''))]
        dropped = len(leads) - len(clean)
        leads = ft.assign_providers(clean)
        print(f'[{ts()}] {cfg["seg"]:14}{cfg["service"]:20} | examinados={examined:4} | sel={len(leads)} (ruido removido={dropped})')
        if not leads:
            print('   (0 leads — pulando)'); continue
        print(f'[{ts()}]   gerando imagens...')
        img1 = ft.gen_image(cfg['img1'], H); img2 = ft.gen_image(cfg['img2'], H)
        html = ft.build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave19w3-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        cr = ft.requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                              json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-19 w3',
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
    print(f"\n[{ts()}] enfileirado={exp}. Monitorando worker (>=5min)...")
    for loop in range(48):  # ~8min
        time.sleep(10)
        tot = 0; ln = []
        for t in track:
            s, d, f = counts(t['cid'], H); t['sent'] = s; t['delivered'] = d; t['failed'] = f; tot += s
            ln.append(f"{t['seg'][:5]}={s}/{d}/{f}")
        print(f'  +{(loop+1)*10:3}s sent={tot}/{exp} | ' + ' '.join(ln), flush=True)
        if tot >= exp: print('>>> dispatch completo'); break
    ph = {p['provider']: p for p in ft.requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    out = {'date': '2026-06-19', 'wave': 3, 'total_sent': sum(t.get('sent', 0) for t in track),
           'total_delivered': sum(t.get('delivered', 0) for t in track), 'segments': track, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = json.load(open(LEDGER, encoding='utf-8')) if os.path.exists(LEDGER) else {'date': '2026-06-19'}
    led['updated_at'] = ts()
    led['providers'] = {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'], 'dailyRemaining': ph[n].get('dailyRemaining'), 'hourlyRemaining': ph[n].get('hourlyRemaining')} for n in HEALTHY}
    led['wave3'] = track
    json.dump(led, open(LEDGER, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    print('\nTOTAL enviado w3:', out['total_sent'], '| LIMITES dia:', {n: f"{ph[n]['sentToday']}/90" for n in HEALTHY})

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'build'
    (do_build if mode == 'build' else do_send)()
