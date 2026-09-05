# -*- coding: utf-8 -*-
"""Reporta ao CRM um evento de WhatsApp (o CRM é quem controla o outreach).

Uso: python wa_event.py <customerId|telefone> sent|reply|optout|failed ["texto"] [--message-id X]
Ex.: python wa_event.py 3F2A...-... sent --message-id true_5527999990000@c.us_ABCD
     python wa_event.py 5527999990000 reply "Pode me ligar amanhã?"
Chave: DIAX_SEND_EMAIL_KEY do ~/.claude/.secrets.env (mesma do send-email). Sem login.
"""
import sys, os, re, json, requests

BASE = os.environ.get('DIAX_API_BASE', 'https://api.alexandrequeiroz.com.br')


def load_key():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        if line.startswith('DIAX_SEND_EMAIL_KEY='):
            return line.split('=', 1)[1].strip().strip('"')
    raise SystemExit('DIAX_SEND_EMAIL_KEY não encontrada no .secrets.env')


def report(target, event, text='', message_id='', provider='waha'):
    body = {'event': event, 'provider': provider}
    if re.fullmatch(r'[0-9a-fA-F-]{36}', target): body['customerId'] = target
    else: body['phone'] = re.sub(r'\D', '', target)
    if text: body['text'] = text[:1500]
    if message_id: body['messageId'] = message_id
    r = requests.post(BASE + '/api/v1/integrations/whatsapp-event', json=body, timeout=30,
                      headers={'X-Integration-Key': load_key(), 'Content-Type': 'application/json'})
    return r.status_code, (r.json() if r.headers.get('content-type', '').startswith('application/json') else r.text)


if __name__ == '__main__':
    a = [x for x in sys.argv[1:] if not x.startswith('--')]
    mid = sys.argv[sys.argv.index('--message-id') + 1] if '--message-id' in sys.argv else ''
    if len(a) < 2 or a[1] not in ('sent', 'reply', 'optout', 'failed'):
        raise SystemExit(__doc__)
    code, res = report(a[0], a[1], a[2] if len(a) > 2 else '', mid)
    print(code, json.dumps(res, ensure_ascii=False) if not isinstance(res, str) else res)
    sys.exit(0 if code == 200 else 1)
