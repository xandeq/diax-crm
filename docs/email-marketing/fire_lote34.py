# -*- coding: utf-8 -*-
import os, requests, json, datetime, re, urllib.parse
with open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8') as f:
    for line in f:
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ[k] = v
base = 'https://api.alexandrequeiroz.com.br'
r = requests.post(base + '/api/v1/auth/login', json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']}, timeout=15)
tok = r.json().get('accessToken') or r.json().get('token')
H = {'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'}

def q(s):
    return urllib.parse.quote(s)

def build(hook, img1, img2, intro, bullets, wa):
    bl = "".join('<tr><td style="padding:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;"><span style="color:#57b3df;font-weight:bold;">&#8250;</span>&nbsp; ' + b + '</td></tr>' for b in bullets)
    return ('<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>'
    '<body style="margin:0;padding:0;background-color:#eef2f5;"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#eef2f5;"><tr><td align="center" style="padding:24px 12px;">'
    '<table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background-color:#ffffff;border-radius:12px;overflow:hidden;">'
    '<tr><td align="left" style="background-color:#0f172a;padding:20px 28px;"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="180" height="48" style="display:block;border:0;height:48px;width:180px;"/></td></tr>'
    '<tr><td style="height:4px;background-color:#57b3df;line-height:4px;font-size:4px;">&nbsp;</td></tr>'
    '<tr><td style="padding:32px 28px 20px 28px;"><h1 style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:33px;color:#0f172a;font-weight:bold;">' + hook + '</h1></td></tr>'
    '<tr><td style="padding:0;font-size:0;line-height:0;"><img src="' + img1 + '" alt="" width="600" style="display:block;width:100%;max-width:600px;height:auto;border:0;"/></td></tr>'
    '<tr><td style="padding:26px 28px 4px 28px;"><p style="margin:0 0 15px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;color:#334155;">' + intro + '</p>'
    '<p style="margin:0 0 15px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;color:#334155;">Para a <strong>{{empresa}}</strong>, eu entrego:</p>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">' + bl + '</table></td></tr>'
    '<tr><td align="center" style="padding:22px 28px 8px 28px;"><table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td align="center" bgcolor="#57b3df" style="border-radius:8px;"><a href="https://wa.me/5527999840101?text=' + q(wa) + '" target="_blank" style="display:inline-block;padding:15px 32px;font-family:Arial,Helvetica,sans-serif;font-size:17px;line-height:21px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:8px;">Quero um diagn&oacute;stico r&aacute;pido pelo WhatsApp &#8250;</a></td></tr></table>'
    '<p style="margin:10px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#64748b;">Conversa sem compromisso. Resposta no mesmo dia.</p></td></tr>'
    '<tr><td style="padding:18px 28px 0 28px;font-size:0;line-height:0;"><img src="' + img2 + '" alt="" width="544" style="display:block;width:100%;max-width:544px;height:auto;border:0;border-radius:8px;"/></td></tr>'
    '<tr><td align="center" style="padding:24px 28px 22px 28px;"><p style="margin:0;font-family:Georgia,serif;font-size:19px;line-height:27px;color:#0f172a;font-style:italic;">"Tecnologia que faz o seu neg&oacute;cio vender mais."</p></td></tr>'
    '<tr><td style="background-color:#0f172a;padding:24px 28px;" align="center"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="160" height="43" style="display:block;border:0;height:43px;width:160px;margin:0 auto 12px auto;"/>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">Sites &middot; Aplicativos &middot; Sistemas sob demanda &middot; Landing pages</p>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">WhatsApp (27) 99984-0101 &middot; contato@alexandrequeiroz.com.br</p>'
    '<p style="margin:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#57b3df;">www.alexandrequeiroz.com.br</p>'
    '<p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:17px;color:#64748b;">Vit&oacute;ria, ES &middot; <a href="{{unsubscribe_url}}" target="_blank" style="color:#64748b;text-decoration:underline;">descadastrar</a></p></td></tr>'
    '</table></td></tr></table></body></html>')

im = json.load(open('D:/claude-code/diax-crm/docs/email-marketing/previews/lote34-images.json', encoding='utf-8'))
b = json.load(open('D:/claude-code/diax-crm/docs/email-marketing/previews/batches-2026-06-17.json', encoding='utf-8'))

def ts():
    return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

jobs = [
 ('academia', 'Apps', "E se os alunos treinassem com o app da {{empresa}}?", im['academia'],
   "Ol&aacute;! Sou o Alexandre Queiroz e desenvolvo aplicativos. Uma academia com app pr&oacute;prio fideliza aluno, organiza treinos e se diferencia.",
   ["App pr&oacute;prio de treino e check-in com a sua marca", "Agendamento de aulas e avalia&ccedil;&otilde;es", "Comunica&ccedil;&atilde;o e avisos direto com os alunos", "Publicado na App Store e Google Play"],
   "Ola! Quero um diagnostico rapido de um app para minha academia",
   "Um app de treino com a marca da {{empresa}}?"),
 ('imobiliaria', 'Landing pages', "Cada im&oacute;vel da {{empresa}} merece uma p&aacute;gina que vende sozinha.", im['imobiliaria'],
   "Ol&aacute;! Sou o Alexandre Queiroz e crio landing pages. Para im&oacute;veis e lan&ccedil;amentos, uma p&aacute;gina dedicada captura o interessado na hora.",
   ["P&aacute;gina por im&oacute;vel ou lan&ccedil;amento, pronta para an&uacute;ncio", "Formul&aacute;rio que captura o lead e cai no seu WhatsApp", "Fotos, tour e mapa que convencem", "R&aacute;pida no celular, com a sua marca"],
   "Ola! Quero uma landing page para meus imoveis",
   "Uma pagina que captura interessado em cada imovel?"),
]
EMAIL_RE = re.compile(r'^[^@\s]+@[^@\s]+\.[a-z]{2,}$', re.I)
for seg, serv, hook, imgs, intro, bullets, wa, subject in jobs:
    html = build(hook, imgs['img1'], imgs['img2'], intro, bullets, wa)
    open('D:/claude-code/diax-crm/docs/email-marketing/templates/lote-' + seg + '.html', 'w', encoding='utf-8').write(html)
    cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H, timeout=30, json={"name": "AQ - " + seg + " - " + serv + " - 2026-06-17", "subject": subject, "bodyHtml": html})
    cid = cr.json().get('id'); b[seg]['campaignId'] = cid
    st = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/send-test', headers=H, json={}, timeout=60)
    ok = [{'customerId': x['id'], 'assignedProvider': x['assignedProvider']} for x in b[seg]['leads'] if x['assignedProvider'] in ('Brevo', 'Mailjet', 'Resend', 'SendGrid')]
    qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60, json={"campaignId": cid, "leads": ok})
    jr = json.loads(qq.text)
    print('[' + ts() + '] ' + seg + ' -> ' + serv + ': draft ' + str(cr.status_code) + ' | test ' + str(st.status_code) + ' | QUEUE ' + str(qq.status_code) + ' ' + str(jr.get('queuedCount')) + ' enfileirados/skip ' + str(jr.get('skippedCount')) + ' | cid ' + str(cid))
json.dump(b, open('D:/claude-code/diax-crm/docs/email-marketing/previews/batches-2026-06-17.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)

ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
led = json.load(open('D:/claude-code/diax-crm/docs/email-marketing/LEDGER-limites-2026-06-17.json', encoding='utf-8'))
led['updated_at'] = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).isoformat()
led['providers'] = {n: {'dailyLimit': ph[n]['dailyLimit'], 'sentToday': ph[n]['sentToday'], 'dailyRemaining': ph[n]['dailyRemaining'], 'hourlyRemaining': ph[n]['hourlyRemaining']} for n in ['Brevo', 'Mailjet', 'Resend', 'SendGrid']}
for seg, serv, *_ in jobs:
    led['batches_done'].append({'seg': seg, 'service': serv, 'sent': len(b[seg]['leads']), 'time': ts(), 'campaignId': b[seg]['campaignId']})
json.dump(led, open('D:/claude-code/diax-crm/docs/email-marketing/LEDGER-limites-2026-06-17.json', 'w', encoding='utf-8'), indent=2, ensure_ascii=False)
print('\n=== MONITOR (todos os lotes de hoje) ===')
for seg in ['contabilidade', 'advocacia', 'academia', 'imobiliaria']:
    g = requests.get(base + '/api/v1/email-campaigns/campaigns/' + b[seg]['campaignId'], headers=H, timeout=20).json()
    print('  ' + seg.ljust(13) + ' recip=' + str(g.get('totalRecipients')) + ' sent=' + str(g.get('sentCount')) + ' deliv=' + str(g.get('deliveredCount')) + ' fail=' + str(g.get('failedCount')) + ' bounce=' + str(g.get('bounceCount')) + ' spam=' + str(g.get('spamCount')) + ' unsub=' + str(g.get('unsubscribeCount')))
print('\nLEDGER uso hoje: ' + str({n: str(ph[n]['sentToday']) + '/' + str(ph[n]['dailyLimit']) for n in ['Brevo', 'Mailjet', 'Resend', 'SendGrid']}))
