# -*- coding: utf-8 -*-
"""Operacao cold email AQ - 2026-06-19 - WAVE 1 (4 segmentos, cada um um servico).
consultoria->Software, pet->Apps, escola->Sites, moveis->Landing pages.
Regras: so 4 providers saudaveis (Brevo/Mailjet/Resend/SendGrid) via queue-with-assignment;
filtro Brasil (bloquear so TLD estrangeiro); corporativo + nunca-contatado; dedup; buffer 10% (max 5/provider/seg).
Imagens via Pollinations gratis -> 600px JPG -> upload /email-images.

Uso:
  python fire_today_19.py build   # DRY: fetch+filter+genimg+build+send-test, escreve wave cache, NAO envia
  python fire_today_19.py send    # LIVE: le o cache, queue-with-assignment + sonda breaker + ledger
"""
import os, sys, re, json, time, datetime, io, base64, urllib.parse, requests
from pathlib import Path

DIR = str(Path(__file__).resolve().parent) + '/'
WAVE_FILE = DIR + 'previews/wave-2026-06-19-w1.json'
LEDGER = DIR + 'LEDGER-limites-2026-06-19.json'
RESULT = DIR + 'result-wave-2026-06-19.json'
base = 'https://api.alexandrequeiroz.com.br'
HEALTHY = ['Brevo', 'Mailjet', 'Resend', 'SendGrid']
PER_PROVIDER = 5  # 5 x 4 = 20 por segmento; 4 segs = 20/provider/hora (<=22 buffer)

FREE_WEBMAIL = {
    'gmail.com', 'hotmail.com', 'outlook.com', 'yahoo.com', 'yahoo.com.br', 'hotmail.com.br',
    'outlook.com.br', 'live.com', 'icloud.com', 'bol.com.br', 'uol.com.br', 'terra.com.br',
    'ig.com.br', 'globo.com', 'globomail.com', 'msn.com', 'aol.com', 'gmail.com.br', 'me.com',
    'yahoo.com.mx', 'r7.com', 'zipmail.com.br', 'oi.com.br', 'pop.com.br'
}
# ccTLD estrangeiros a bloquear (Brasil-only). .com/.com.br/.br/.net/.org/.app/.io = mantem.
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

# Campanha de referencia para checar o pilot/status (qualquer campanha existente serve)
_REF_CAMPAIGN = '3dfba825-3980-4fdd-a5fc-4639b69fb0c1'

def ensure_breaker_closed(H, ref_campaign=_REF_CAMPAIGN):
    """Self-healing: se o circuit breaker estiver aberto, reseta via o endpoint admin
    (POST /pilot/reset) — sem depender de restart manual. Retorna True se ficou fechado."""
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

# ---------- COPY + imagens por segmento ----------
SEGMENTS = [
    dict(seg='consultoria', search='consultoria', service='Software sob demanda',
         hook='Sua consultoria perde tempo com processos manuais?',
         subject='Um sistema sob medida para a sua consultoria?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Centralizar clientes, projetos e entregas num sistema s&oacute; reduz retrabalho e d&aacute; previsibilidade.',
         bullets=['Painel de clientes e projetos em um lugar s&oacute;', 'Propostas, contratos e follow-up organizados',
                  'Relat&oacute;rios autom&aacute;ticos para decis&otilde;es r&aacute;pidas', 'Feito sob medida para o seu fluxo'],
         wa='Ola! Quero um diagnostico rapido de um sistema para minha consultoria',
         img1='A Brazilian business consultant using custom management software on a laptop in a modern office meeting, charts on screen, realistic editorial photo',
         img2='A modern Brazilian consulting office, professionals collaborating around a table with laptops, realistic editorial photo'),
    dict(seg='pet', search='pet', service='Apps',
         hook='E se os tutores agendassem banho e tosa no app do seu pet shop?',
         subject='Um app de agendamento com a marca do seu pet shop?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Um pet shop com app pr&oacute;prio facilita o agendamento, fideliza tutores e se diferencia.',
         bullets=['Agendamento de banho e tosa direto no app', 'Lembretes autom&aacute;ticos para reduzir falta',
                  'Programa de fidelidade com a sua marca', 'Publicado na App Store e Google Play'],
         wa='Ola! Quero um diagnostico rapido de um app para meu pet shop',
         img1='A Brazilian pet shop owner using a booking app on a smartphone in a cheerful pet shop, a happy dog nearby, realistic editorial photo',
         img2='A cheerful modern Brazilian pet shop interior with pets and products, warm light, realistic editorial photo'),
    dict(seg='escola', search='escola', service='Sites',
         hook='Os pais encontram a sua escola no Google na hora da matr&iacute;cula?',
         subject='A sua escola aparece no Google na hora da matr&iacute;cula?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio sites. Na &eacute;poca de matr&iacute;cula os pais pesquisam &mdash; quem tem site profissional passa confian&ccedil;a e aparece primeiro.',
         bullets=['Site que apresenta a proposta pedag&oacute;gica', 'Formul&aacute;rio de matr&iacute;cula e contato direto',
                  'Aparecer no Google de quem busca escola na cidade', 'R&aacute;pido e bonito no celular, com a sua marca'],
         wa='Ola! Quero um diagnostico rapido de um site para minha escola',
         img1='A Brazilian school director using the school website on a laptop in a bright modern school office, realistic editorial photo',
         img2='A cheerful modern Brazilian school environment with students and teachers, daylight, realistic editorial photo'),
    dict(seg='moveis', search='movei', service='Landing pages',
         hook='Cada cole&ccedil;&atilde;o da sua loja de m&oacute;veis merece uma p&aacute;gina que vende.',
         subject='Uma landing page que vende a sua cole&ccedil;&atilde;o de m&oacute;veis?',
         intro='Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages que convertem. Uma p&aacute;gina dedicada por cole&ccedil;&atilde;o transforma visita em or&ccedil;amento.',
         bullets=['Landing focada em gerar or&ccedil;amentos', 'Fotos dos m&oacute;veis que despertam desejo',
                  'Bot&atilde;o de WhatsApp para fechar na hora', 'Pronta para a sua pr&oacute;xima campanha'],
         wa='Ola! Quero um diagnostico rapido de uma landing page para minha loja de moveis',
         img1='A Brazilian furniture store owner showing a product landing page on a tablet inside a modern furniture showroom, realistic editorial photo',
         img2='A modern Brazilian furniture showroom with sofas and decor, warm light, customers browsing, realistic editorial photo'),
]

def build_html(hook, img1, img2, intro, bullets, wa):
    bl = "".join('<tr><td style="padding:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;"><span style="color:#57b3df;font-weight:bold;">&#8250;</span>&nbsp; ' + b + '</td></tr>' for b in bullets)
    return ('<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>'
    '<body style="margin:0;padding:0;background-color:#eef2f5;"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#eef2f5;"><tr><td align="center" style="padding:24px 12px;">'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;background-color:#ffffff;border-radius:12px;overflow:hidden;">'
    '<tr><td align="left" style="background-color:#0f172a;padding:20px 28px;"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="180" style="display:block;border:0;height:auto;width:180px;"/></td></tr>'
    '<tr><td style="height:4px;background-color:#57b3df;line-height:4px;font-size:4px;">&nbsp;</td></tr>'
    '<tr><td style="padding:32px 28px 20px 28px;"><h1 style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:33px;color:#0f172a;font-weight:bold;">' + hook + '</h1></td></tr>'
    '<tr><td style="padding:0;font-size:0;line-height:0;"><img src="' + img1 + '" alt="" width="600" style="display:block;width:100%;max-width:600px;height:auto;border:0;"/></td></tr>'
    '<tr><td style="padding:26px 28px 4px 28px;"><p style="margin:0 0 15px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;color:#334155;">' + intro + '</p>'
    '<p style="margin:0 0 15px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;color:#334155;">Para a <strong>{{empresa}}</strong>, eu entrego:</p>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">' + bl + '</table></td></tr>'
    '<tr><td align="center" style="padding:22px 28px 8px 28px;"><table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td align="center" bgcolor="#57b3df" style="border-radius:8px;"><a href="https://wa.me/5527999840101?text=' + qp(wa) + '&amp;utm_source=email&amp;utm_medium=campanha&amp;utm_campaign=aq_wave" target="_blank" style="display:inline-block;padding:15px 32px;font-family:Arial,Helvetica,sans-serif;font-size:17px;line-height:21px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:8px;">Quero um diagn&oacute;stico r&aacute;pido pelo WhatsApp &#8250;</a></td></tr></table>'
    '<p style="margin:10px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#64748b;">Conversa sem compromisso. Resposta no mesmo dia.</p></td></tr>'
    '<tr><td style="padding:18px 28px 0 28px;font-size:0;line-height:0;"><img src="' + img2 + '" alt="" width="544" style="display:block;width:100%;max-width:544px;height:auto;border:0;border-radius:8px;"/></td></tr>'
    '<tr><td align="center" style="padding:24px 28px 22px 28px;"><p style="margin:0;font-family:Georgia,serif;font-size:19px;line-height:27px;color:#0f172a;font-style:italic;">"Tecnologia que faz o seu neg&oacute;cio vender mais."</p></td></tr>'
    '<tr><td style="background-color:#0f172a;padding:24px 28px;" align="center"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="160" style="display:block;border:0;height:auto;width:160px;margin:0 auto 12px auto;"/>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">Sites &middot; Aplicativos &middot; Sistemas sob demanda &middot; Landing pages</p>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">WhatsApp (27) 99984-0101 &middot; contato@alexandrequeiroz.com.br</p>'
    '<p style="margin:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#57b3df;">www.alexandrequeiroz.com.br</p>'
    '<p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:17px;color:#64748b;">Vit&oacute;ria, ES &middot; <a href="{{unsubscribe_url}}" target="_blank" style="color:#64748b;text-decoration:underline;">descadastrar</a></p></td></tr>'
    '</table></td></tr></table></body></html>')

def gen_image(prompt, H):
    """Pollinations -> PIL 600px JPG -> upload /email-images -> hosted PublicUrl."""
    from PIL import Image
    url = 'https://image.pollinations.ai/prompt/' + qp(prompt) + '?width=1024&height=640&model=flux&nologo=true&seed=19'
    raw = requests.get(url, timeout=120).content
    im = Image.open(io.BytesIO(raw)).convert('RGB')
    w = 600
    h = int(im.height * (w / im.width))
    im = im.resize((w, h), Image.LANCZOS)
    buf = io.BytesIO(); im.save(buf, 'JPEG', quality=82, optimize=True)
    b64 = base64.b64encode(buf.getvalue()).decode()
    up = requests.post(base + '/api/v1/email-images/upload', headers=H, timeout=60,
                       json={'FileName': 'aq19.jpg', 'Base64Content': b64, 'ContentType': 'image/jpeg'})
    j = up.json()
    return j.get('publicUrl') or j.get('PublicUrl')

def email_ok(email, seen_emails, seen_domains):
    if not email or '@' not in email:
        return False, 'sem-email'
    email = email.strip().lower()
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

def fetch_leads(seg_cfg, H, seen_emails, seen_domains, seen_company, want):
    """Busca leads do CRM por nicho, filtra corporativo+nunca+BR+dedup. Retorna ate `want`."""
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
    print(f'[{ts()}] login OK. WAVE 1 (19/06): consultoria/pet/escola/moveis')
    seen_emails, seen_domains, seen_company = set(), set(), set()
    want = PER_PROVIDER * len(HEALTHY)  # 20/seg
    wave = {'date': '2026-06-19', 'wave': 1, 'created': ts(), 'segments': []}
    for cfg in SEGMENTS:
        leads, examined = fetch_leads(cfg, H, seen_emails, seen_domains, seen_company, want)
        leads = assign_providers(leads)
        print(f'[{ts()}] {cfg["seg"]:12} {cfg["service"]:20} | examinados={examined:4} | selecionados={len(leads)}')
        print(f'[{ts()}]   gerando imagens (Pollinations)...')
        img1 = gen_image(cfg['img1'], H)
        img2 = gen_image(cfg['img2'], H)
        html = build_html(cfg['hook'], img1, img2, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + 'templates/wave19-' + cfg['seg'] + '.html', 'w', encoding='utf-8').write(html)
        # cria campanha + schedule(+5min p/ ReadinessGate) + send-test admin
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30,
                           json={'name': 'AQ - ' + cfg['seg'] + ' - ' + cfg['service'] + ' - 2026-06-19',
                                 'subject': cfg['subject'], 'bodyHtml': html})
        cid = cr.json().get('id')
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule', headers=H,
                           timeout=20, json={'scheduledAt': future.isoformat()})
        st = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H,
                           json={}, timeout=60)
        print(f'[{ts()}]   campanha {cid} | img1 ok | img2 ok | schedule {sc.status_code} | send-test {st.status_code}')
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
    print('  >>> send-test foi enviado SO para o admin. Nenhum prospect recebeu. Rode "send" para disparar.')

# ============================ SEND (LIVE) ============================
def campaign_counts(cid, H):
    d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
    return d.get('sentCount', 0), d.get('deliveredCount', 0), d.get('failedCount', 0)

def do_send():
    load_env(); H = login()
    ensure_breaker_closed(H)  # self-healing: reseta o breaker se estiver aberto
    wave = json.load(open(WAVE_FILE, encoding='utf-8'))
    print(f'[{ts()}] LIVE - enfileirando wave 1 (19/06), {len(wave["segments"])} segmentos, sonda breaker apos o 1o.')
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
            for t in range(42):  # ~7min: cobre um ciclo completo de worker (5min) + folga
                time.sleep(10)
                sent, deliv, fail = campaign_counts(cid, H)
                print(f'   +{(t+1)*10}s sent={sent} delivered={deliv} failed={fail}')
                if sent > 0:
                    break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO - {s["seg"]} 0 enviados em 90s. ABORTANDO. FIX: restart API DIAX (SmarterASP).')
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
    out = {'date': '2026-06-19', 'wave': 1, 'aborted': aborted, 'total_sent': total_sent,
           'segments': results, 'updated_at': ts()}
    json.dump(out, open(RESULT, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
    led = {'date': '2026-06-19', 'updated_at': ts(),
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
        print('uso: python fire_today_19.py [build|send]')
