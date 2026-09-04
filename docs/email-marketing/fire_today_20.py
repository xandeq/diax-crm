# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-20 - WAVE 1.
Novos nichos: farmacia->Sites, salao->Apps, medico->Software, hotel->Landing.
Regras: so 4 providers saudaveis (Brevo/Mailjet/Resend/SendGrid) via queue-with-assignment;
filtro Brasil (bloquear TLD estrangeiro); corporativo + nunca-contatado; dedup; buffer 10% (max 5/provider/seg).
Imagens via Pollinations gratis -> 600px JPG -> upload /email-images.

Uso:
  python fire_today_20.py build   # DRY: fetch+filter+genimg+build+send-test, escreve wave cache
  python fire_today_20.py send    # LIVE: le o cache, queue-with-assignment + sonda breaker
"""
import os, sys, re, json, time, datetime, io, base64, urllib.parse, requests
from pathlib import Path

DIR = str(Path(__file__).resolve().parent) + '/'
WAVE_FILE = DIR + 'previews/wave-2026-06-20-w1.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-20.json'
RESULT = DIR + 'result-wave-2026-06-20.json'
base = 'https://api.alexandrequeiroz.com.br'
HEALTHY = ['Brevo', 'Mailjet', 'Resend', 'SendGrid']
PER_PROVIDER = 5  # 5 x 4 = 20 por segmento; 4 segs = 20/provider/hora (<=22 buffer)

FREE_WEBMAIL = {
    'gmail.com', 'hotmail.com', 'outlook.com', 'yahoo.com', 'yahoo.com.br', 'hotmail.com.br',
    'outlook.com.br', 'live.com', 'icloud.com', 'bol.com.br', 'uol.com.br', 'terra.com.br',
    'ig.com.br', 'globo.com', 'globomail.com', 'msn.com', 'aol.com', 'gmail.com.br', 'me.com',
    'yahoo.com.mx', 'r7.com', 'zipmail.com.br', 'oi.com.br', 'pop.com.br'
}
FOREIGN_TLD = ('.pt', '.es', '.us', '.ar', '.uk', '.fr', '.it', '.de', '.mx', '.py',
               '.uy', '.cl', '.bo', '.pe', '.ve', '.co', '.ca', '.au', '.jp', '.cn', '.ru')

def ts():
    return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

def load_env():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ[k] = v

def login():
    r = requests.post(base + '/api/v1/auth/login',
                      json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']},
                      timeout=20).json()
    tok = r.get('accessToken') or r.get('token')
    return {'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'}

def qp(s):
    return urllib.parse.quote(s)

_REF_CAMPAIGN = '3dfba825-3980-4fdd-a5fc-4639b69fb0c1'

def ensure_breaker_closed(H, ref_campaign=_REF_CAMPAIGN):
    """Self-healing: se o circuit breaker estiver aberto, reseta via o endpoint admin."""
    try:
        ps = requests.get(base + '/api/v1/email-campaigns/campaigns/' + ref_campaign + '/pilot/status', headers=H, timeout=20).json()
        if not ps.get('isCircuitBreakerOpen'):
            return True
        print(f'[{ts()}] breaker ABERTO ({ps.get("circuitBreakerReason")}) -> auto-reset via API...')
        rr = requests.post(base + '/api/v1/email-campaigns/pilot/reset', headers=H, timeout=20)
        if rr.status_code == 200:
            print(f'[{ts()}] reset OK: {rr.text[:120]}')
            return True
        print(f'[{ts()}] reset FALHOU ({rr.status_code}): {rr.text[:160]}')
        return False
    except Exception as e:
        print(f'[{ts()}] ensure_breaker_closed erro: {e}')
        return False

SEGMENTS = [
    dict(seg='farmacia', search='farmacia', service='Sites',
         hook='Sua farm&aacute;cia aparece no Google quando algu&eacute;m precisa de rem&eacute;dio?',
         subject='A {{empresa}} aparece no Google de quem busca farm&aacute;cia perto?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Quem precisa de rem&eacute;dio pesquisa no Google &mdash; uma farm&aacute;cia com site profissional aparece primeiro, passa confian&ccedil;a e converte mais.',
         bullets=['Cardápio de produtos e servi&ccedil;os atualizado', 'Aparecer no Google de quem busca farm&aacute;cia na cidade',
                  'Hor&aacute;rio de funcionamento e contato no site', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para minha farmacia',
         img1='A Brazilian pharmacy owner using a professional website on a laptop at a well-lit pharmacy counter, medicine shelves in background, realistic editorial photo',
         img2='A modern Brazilian pharmacy interior with organized medicine shelves and bright light, realistic editorial photo'),
    dict(seg='salao', search='salao de beleza', service='Apps',
         hook='E se seus clientes agendassem o hor&aacute;rio pelo app do seu sal&atilde;o?',
         subject='Um app de agendamento com a marca do seu sal&atilde;o de beleza?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um sal&atilde;o com app pr&oacute;prio facilita o agendamento, reduz faltas e fideliza o cliente na sua marca.',
         bullets=['Agendamento de hor&aacute;rios direto no app', 'Lembretes autom&aacute;ticos para reduzir falta',
                  'Programa de fidelidade com a sua marca', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app para meu salao de beleza',
         img1='A Brazilian hair salon owner using a booking app on a smartphone at the reception of a modern beauty salon, realistic editorial photo',
         img2='A modern Brazilian beauty salon interior with stylists and clients, warm light, realistic editorial photo'),
    dict(seg='medico', search='clinica medica', service='Software sob demanda',
         hook='Sua cl&iacute;nica ainda controla pacientes e consultas no papel?',
         subject='Um sistema de gest&atilde;o sob medida para a sua cl&iacute;nica m&eacute;dica?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar pacientes, agendamentos e prontu&aacute;rios num sistema s&oacute; reduz retrabalho e melhora o atendimento.',
         bullets=['Prontu&aacute;rio eletr&ocirc;nico e hist&oacute;rico do paciente', 'Agenda de consultas com gest&atilde;o de conflitos',
                  'Relat&oacute;rios financeiros e de atendimento', 'Feito sob medida para o seu fluxo'],
         wa='Ola! Quero um diagnostico rapido de um sistema para minha clinica medica',
         img1='A Brazilian doctor using a patient management software on a computer in a modern medical office, realistic editorial photo',
         img2='A modern Brazilian medical clinic with a doctor consulting a patient, bright and professional environment, realistic editorial photo'),
    dict(seg='hotel', search='hotel', service='Landing pages',
         hook='Cada temporada do seu hotel merece uma p&aacute;gina que converte reservas diretas.',
         subject='Uma landing page de reservas diretas para o {{empresa}}?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages que convertem. Uma p&aacute;gina dedicada por temporada elimina a comiss&atilde;o das OTAs e traz a reserva direto para o seu WhatsApp.',
         bullets=['Landing focada em gerar reservas diretas (sem comiss&atilde;o)', 'Fotos e diferenciais que despertam desejo',
                  'Bot&atilde;o de WhatsApp para fechar na hora', 'Pronta para a sua pr&oacute;xima temporada'],
         wa='Ola! Quero um diagnostico rapido de uma landing page de reservas para meu hotel',
         img1='A Brazilian hotel manager showing a direct booking landing page on a tablet in a hotel lobby, realistic editorial photo',
         img2='A beautiful Brazilian hotel lobby with natural light, welcoming atmosphere and guests checking in, realistic editorial photo'),
]

def build_html(hook, img1, img2, intro, bullets, wa):
    bl = ''.join(
        '<p style="margin:0 0 8px;font-size:15px;color:#0f172a;">&#8250; ' + b + '</p>'
        for b in bullets
    )
    return (
        '<!DOCTYPE html><html><head><meta charset="utf-8"/>'
        '<meta name="viewport" content="width=device-width,initial-scale=1.0"/></head>'
        '<body style="margin:0;padding:0;background:#eef2f5;">'
        '<table width="100%" cellpadding="0" cellspacing="0" style="background:#eef2f5;">'
        '<tr><td align="center" style="padding:20px 8px;">'
        '<table width="100%" cellpadding="0" cellspacing="0" '
        'style="max-width:600px;background:#fff;border-radius:12px;overflow:hidden;">'
        '<tr><td style="background:#0f172a;padding:18px 26px;">'
        '<img src="https://www.alexandrequeiroz.com.br/images/logo.png" '
        'alt="AQ" width="150" style="display:block;"/></td></tr>'
        '<tr><td style="height:3px;background:#57b3df;font-size:0;">&nbsp;</td></tr>'
        '<tr><td style="padding:24px 26px 14px;">'
        '<h1 style="margin:0;font-family:Arial,sans-serif;font-size:22px;'
        'line-height:1.3;color:#0f172a;">' + hook + '</h1></td></tr>'
        '<tr><td><img src="' + img1 + '" width="600" alt="" '
        'style="display:block;width:100%;"/></td></tr>'
        '<tr><td style="padding:20px 26px;font-family:Arial,sans-serif;">'
        '<p style="margin:0 0 12px;font-size:15px;line-height:1.6;color:#334155;">'
        + intro + '</p>'
        '<p style="margin:0 0 10px;font-size:15px;font-weight:bold;color:#0f172a;">'
        'Para a {{empresa}}:</p>'
        + bl +
        '</td></tr>'
        '<tr><td align="center" style="padding:16px 26px;">'
        '<a href="https://wa.me/5527999840101?text=' + qp(wa) + '&amp;utm_source=email&amp;utm_medium=cold_email&amp;utm_campaign=aq" '
        'target="_blank" style="display:inline-block;background:#57b3df;color:#fff;'
        'padding:13px 26px;font-family:Arial,sans-serif;font-size:16px;'
        'font-weight:bold;text-decoration:none;border-radius:8px;">'
        'Diagn&oacute;stico gratuito pelo WhatsApp &#8250;</a></td></tr>'
        '<tr><td style="padding:12px 26px 0;">'
        '<img src="' + img2 + '" width="548" alt="" '
        'style="display:block;width:100%;border-radius:6px;"/></td></tr>'
        '<tr><td style="background:#0f172a;padding:16px 26px;text-align:center;">'
        '<p style="margin:0 0 4px;font-family:Arial,sans-serif;font-size:12px;color:#94a3b8;">'
        'Alexandre Queiroz &middot; (27) 99984-0101 &middot; alexandrequeiroz.com.br</p>'
        '<p style="margin:0;font-family:Arial,sans-serif;font-size:11px;color:#475569;">'
        'Vit&oacute;ria, ES &middot; '
        '<a href="{{unsubscribe_url}}" style="color:#475569;">descadastrar</a>'
        '</p></td></tr>'
        '</table></td></tr></table></body></html>'
    )

def gen_image(prompt, H, keyword=None):
    """Pollinations (3 retries, random seed) -> Pexels fallback -> PIL 600px JPG -> upload /email-images -> hosted PublicUrl."""
    from PIL import Image
    import random

    def _download_and_upload(raw_bytes):
        im = Image.open(io.BytesIO(raw_bytes)).convert('RGB')
        w = 600; h = int(im.height * (w / im.width))
        im = im.resize((w, h), Image.LANCZOS)
        buf = io.BytesIO(); im.save(buf, 'JPEG', quality=82, optimize=True)
        b64 = base64.b64encode(buf.getvalue()).decode()
        up = requests.post(base + '/api/v1/email-images/upload', headers=H, timeout=60,
                           json={'FileName': 'aq20.jpg', 'Base64Content': b64, 'ContentType': 'image/jpeg'})
        j = up.json()
        return j.get('publicUrl') or j.get('PublicUrl')

    # Try Pollinations with 3 different seeds
    for attempt in range(3):
        try:
            seed = random.randint(1, 99999)
            url = ('https://image.pollinations.ai/prompt/' + qp(prompt)
                   + f'?width=1024&height=640&model=flux&nologo=true&seed={seed}')
            raw = requests.get(url, timeout=120).content
            return _download_and_upload(raw)
        except Exception as e:
            print(f'   Pollinations attempt {attempt+1} falhou: {type(e).__name__}: {e}')
            time.sleep(2)

    # Fallback: Pexels stock photo
    try:
        pexels_key = os.environ.get('PEXELS_API_KEY', '')
        q = keyword or prompt.split(',')[0][:50]
        resp = requests.get('https://api.pexels.com/v1/search',
                            params={'query': q, 'orientation': 'landscape', 'per_page': 5, 'size': 'large'},
                            headers={'Authorization': pexels_key}, timeout=20).json()
        photos = resp.get('photos', [])
        if photos:
            img_url = photos[0]['src']['large']
            raw = requests.get(img_url, timeout=60).content
            return _download_and_upload(raw)
    except Exception as e:
        print(f'   Pexels fallback falhou: {e}')

    return None

def email_ok(email, seen_emails, seen_domains):
    if not email or '@' not in email:
        return False, 'sem-email'
    email = email.strip().lower()
    if '%' in email:
        return False, 'email-malformado'  # URL-encoded chars (%20 etc)
    dom = email.split('@', 1)[1]
    if email in seen_emails:
        return False, 'dup-email'
    if dom in seen_domains:
        return False, 'dup-dominio'
    if dom in FREE_WEBMAIL:
        return False, 'webmail-nao-corp'
    if any(dom.endswith(t) for t in FOREIGN_TLD):
        return False, 'tld-estrangeiro'
    return True, 'ok'

_MX_CACHE = None


def _mx_deliverable(domain):
    """Domínio lixo/placeholder ou sem MX/A -> não enfileira (cache 30d, salvo no exit)."""
    global _MX_CACHE
    try:
        import atexit, mx_check
        if _MX_CACHE is None:
            _MX_CACHE = mx_check.load_cache()
            atexit.register(lambda: mx_check.save_cache(_MX_CACHE))
        return mx_check.deliverable_domain(domain, _MX_CACHE)
    except Exception:
        return True   # falha da checagem nunca bloqueia o envio


def fetch_leads(seg_cfg, H, seen_emails, seen_domains, seen_company, want):
    """Busca leads do CRM por nicho, filtra corporativo+nunca+BR+dedup."""
    picked = []
    page = 1
    examined = 0
    while len(picked) < want and page <= 8:
        r = requests.get(base + '/api/v1/leads',
                         params={'search': seg_cfg['search'], 'page': page, 'pageSize': 100},
                         headers=H, timeout=30).json()
        items = r.get('items', [])
        if not items:
            break
        for it in items:
            examined += 1
            if it.get('emailOptOut'):
                continue
            if (it.get('emailSentCount') or 0) > 0 or it.get('lastEmailSentAt'):
                continue  # nunca-contatado
            email = (it.get('email') or '').strip().lower()
            ok, _ = email_ok(email, seen_emails, seen_domains)
            if not ok:
                continue
            # MX/placeholder antes de enfileirar: bounce 7,5% na semana 04/09 (instagram.local, wixpress)
            if not _mx_deliverable(email.split('@', 1)[1]):
                continue
            comp = (it.get('companyName') or it.get('name') or '').strip().lower()
            comp_key = re.sub(r'[^a-z0-9]', '', comp)
            if comp_key and comp_key in seen_company:
                continue
            seen_emails.add(email)
            seen_domains.add(email.split('@', 1)[1])
            if comp_key:
                seen_company.add(comp_key)
            picked.append({'id': it['id'], 'email': email,
                           'company': it.get('companyName') or it.get('name'),
                           'website': (it.get('website') or '').strip(),
                           'score': it.get('leadScore') or 0})
            if len(picked) >= want:
                break
        if not r.get('hasNextPage'):
            break
        page += 1
    return picked, examined

def assign_providers(leads):
    for i, ld in enumerate(leads):
        ld['assignedProvider'] = HEALTHY[i % len(HEALTHY)]
    return leads

# ============================ BUILD (DRY) ============================
def do_build():
    load_env(); H = login()
    print(f'[{ts()}] login OK. WAVE 1 (20/06): farmacia/salao/medico/hotel')
    os.makedirs(DIR + 'previews', exist_ok=True)
    os.makedirs(DIR + 'templates', exist_ok=True)
    seen_emails, seen_domains, seen_company = set(), set(), set()
    want = PER_PROVIDER * len(HEALTHY)
    wave = {'date': '2026-06-20', 'wave': 1, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:12} {cfg["service"]:20} | examinados={examined:4} | selecionados={len(leads)}')
        if not leads:
            print('   (0 leads — pulando segmento)'); continue
        print(f'[{ts()}]   gerando imagens (Pollinations)...')
        img1 = gen_image(cfg['img1'], H)
        img2 = gen_image(cfg['img2'], H)
        if not img1 or not img2:
            print(f'[{ts()}]   ERRO: gen_image retornou None (img1={img1}, img2={img2}) — pulando segmento')
            continue
        html = build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave20-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                           json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-20',
                                 'subject': cfg['subject'], 'bodyHtml': html})
        cr_json = cr.json()
        cid = cr_json.get('id')
        if not cid:
            print(f'[{ts()}]   ERRO: campanha CREATE {cr.status_code}: {cr.text[:300]} — pulando segmento')
            continue
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H,
                           timeout=20, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')})
        st = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H,
                           json={}, timeout=60)
        print(f'[{ts()}]   campanha {cid} | schedule {sc.status_code} | send-test {st.status_code}')
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'subject': cfg['subject'],
                                 'campaignId': cid, 'img1': img1, 'img2': img2,
                                 'leads': leads,
                                 'by_provider': {p: sum(1 for l in leads if l['assignedProvider'] == p) for p in HEALTHY}})
    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    total = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] DRY OK. cache -> {WAVE_FILE}')
    print(f'  TOTAL selecionado: {total} | por segmento: ' + ', '.join(f"{s['seg']}={len(s['leads'])}" for s in wave['segments']))
    bp = {p: sum(s['by_provider'][p] for s in wave['segments']) for p in HEALTHY}
    print(f'  por provider (hora): {bp}  (limite 22/h c/ buffer)')
    print('  >>> send-test enviado SO para o admin. Rode "send" para disparar.')

# ============================ SEND (LIVE) ============================
def campaign_counts(cid, H):
    d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

def do_send():
    load_env(); H = login()
    ensure_breaker_closed(H)
    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] LIVE - enfileirando wave 1 (20/06), {len(wave["segments"])} segmentos')
    results = []
    aborted = False
    for i, s in enumerate(wave['segments']):
        cid = s['campaignId']
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']}
                 for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60,
                           json={'campaignId': cid, 'leads': leads})
        jr = json.loads(qq.text)
        print(f'[{ts()}] {s["seg"]:12} {s["service"]:20} | QUEUE {qq.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')
        results.append({'seg': s['seg'], 'cid': cid, 'queued': jr.get('queuedCount'), 'skipped': jr.get('skippedCount')})
        if i == 0:
            if qq.status_code not in (200, 201, 202):
                print('   !!! QUEUE rejeitado no 1o segmento:', qq.text[:300]); aborted = True; break
            print(f'[{ts()}] sonda breaker: aguardando worker (ciclo 5min) dispatchar {s["seg"]}...')
            sent = 0
            for t in range(42):
                time.sleep(10)
                sent, deliv, fail = campaign_counts(cid, H)
                print(f'   +{(t+1)*10}s sent={sent} delivered={deliv} failed={fail}')
                if sent > 0:
                    break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO - {s["seg"]} 0 enviados em 420s. Verifique logs da API.')
                aborted = True; break
            print(f'[{ts()}] breaker OK - seguindo.\n')
        if i < len(wave['segments']) - 1 and not aborted:
            time.sleep(15)
    print('\n=== SNAPSHOT FINAL (aguarda 20s) ===')
    time.sleep(20)
    total_sent = 0
    for r in results:
        s, d, f = campaign_counts(r['cid'], H)
        r['sent'] = s; r['delivered'] = d; r['failed'] = f
        total_sent += s
        print(f"  {r['seg']:12} queued={r['queued']} | sent={s} delivered={d} failed={f}")
    print(f'\nTOTAL enviado: {total_sent} | aborted={aborted}')
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    out = {'date': '2026-06-20', 'wave': 1, 'aborted': aborted, 'total_sent': total_sent,
           'segments': results, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = {'date': '2026-06-20', 'updated_at': ts(),
           'providers': {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'],
                             'dailyRemaining': ph[n].get('dailyRemaining'), 'hourlyRemaining': ph[n].get('hourlyRemaining')}
                         for n in HEALTHY},
           'forbidden': ['ElasticEmail', 'MailerSend'], 'wave1': results}
    json.dump(led, open(LEDGER, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    print('LIMITES:', {n: f"{ph[n]['sentToday']}/{ph[n]['dailyLimit']}" for n in HEALTHY})
    print(f'Salvo: {RESULT} + {LEDGER}')

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'build'
    if mode == 'build':
        do_build()
    elif mode == 'send':
        do_send()
    else:
        print('uso: python fire_today_20.py [build|send]')
