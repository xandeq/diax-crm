# -*- coding: utf-8 -*-
"""Vigia de respostas — roda a cada 30 min (seg-sex 08-19, Task DIAX-Reply-Watch).

Resposta é a conversão real do cold email; responder em <1h fecha, em 24h esfria.
Fluxo: IMAP (UID > último) → remetente é destinatário de wave? → ignora auto-reply/bounce
→ classifica intenção → rascunho de email + WhatsApp (com o achado real do site) →
Telegram na hora + tarefa Urgente no CRM + status Qualified (positivo/neutro) ou
supressão (negativo). Dedupe por email (7 dias). `--dry` = só imprime.
"""
import os, sys, re, json, imaplib, email, datetime as dt, urllib.parse
from email.header import decode_header
import requests

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
from site_check import check_site, summarize_fup

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
STATE = DIR + 'previews/replies-state.json'
LOG = DIR + 'logs/reply-watch.log'
BASE = 'https://api.alexandrequeiroz.com.br'
DRY = '--dry' in sys.argv
MY_PHONE = '5527999840101'

AUTO_SUBJ = re.compile(r'resposta autom|auto[- ]?reply|automatic reply|out of (the )?office|fora do escrit|aus[êe]ncia|'
                       r'undeliverable|delivery (status|failure)|mail delivery|failure notice|n[ãa]o (foi )?entregue', re.I)
AUTO_FROM = re.compile(r'mailer-daemon|postmaster|no-?reply|nao-?responda|bounce', re.I)
NEG = re.compile(r'n[ãa]o (tenho|temos|tem|quero|queremos|envie|enviem|mande|mandem|precis)|sem interesse|remov|descadastr|'
                 r'retir(ar|em)|pare de|parem de|unsubscribe|spam|não me envie|nao me envie', re.I)
POS = re.compile(r'pode (enviar|mandar|explicar|ligar|chamar)|tenho interesse|temos interesse|interess|or[çc]amento|'
                 r'quanto (custa|fica|é|e o valor)|valor|pre[çc]o|vamos conversar|me liga|me chama|agend|proposta|'
                 r'gostaria de|quero (saber|entender|ver)', re.I)


def log(msg):
    os.makedirs(DIR + 'logs', exist_ok=True)
    line = f'[{dt.datetime.now():%Y-%m-%d %H:%M:%S}] {msg}'
    try: print(line)
    except UnicodeEncodeError: print(line.encode('ascii', 'ignore').decode())
    open(LOG, 'a', encoding='utf-8').write(line + '\n')


def load_env():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ.setdefault(k, v.strip().strip('"'))


# ---------- puro ----------
def is_auto_reply(subject, from_addr, headers):
    h = {k.lower(): (v or '') for k, v in (headers or {}).items()}
    if 'auto-submitted' in h and h['auto-submitted'].lower() not in ('', 'no'): return True
    if h.get('precedence', '').lower() in ('bulk', 'junk', 'auto_reply', 'list'): return True
    if 'x-autoreply' in h or 'x-autorespond' in h: return True
    return bool(AUTO_SUBJ.search(subject or '') or AUTO_FROM.search(from_addr or ''))


def classify_intent(text):
    t = (text or '')[:1500]
    if NEG.search(t): return 'negative'
    if POS.search(t): return 'positive'
    return 'neutral'


def wa_link(phone, text):
    digits = re.sub(r'\D', '', phone or '')
    if not digits: return ''
    if not digits.startswith('55'): digits = '55' + digits
    return f'https://wa.me/{digits}?text={urllib.parse.quote(text)}'


def _finding(lead):
    fs = lead.get('findings') or []
    return fs[0] if fs else ''


def build_drafts(lead, intent):
    """(email_reply, whatsapp) em pt-BR, curtos, próximo passo concreto. Escrita normal (vai p/ terceiro)."""
    nome = lead.get('name') or 'vocês'
    service = lead.get('service') or 'o site'
    f = _finding(lead)
    achado = f' Já adianto uma coisa que vi no site de vocês: {f}.' if f else ''
    if intent == 'negative':
        em = (f'Olá! Entendido, sem problema — não envio mais nada para a {nome}. '
              f'Se um dia fizer sentido, meu WhatsApp é (27) 99984-0101. Obrigado pelo retorno e bons negócios!')
        return em, ''
    if intent == 'positive':
        em = (f'Olá! Que bom, obrigado pelo retorno.{achado}\n\n'
              f'Para não tomar seu tempo, proponho 15 minutos por WhatsApp ou ligação: consigo hoje à tarde ou amanhã de manhã — '
              f'qual fica melhor para a {nome}? Nessa conversa eu mostro o que faria em {service} e quanto custa, sem enrolação.\n\n'
              f'Se preferir, me chama direto no (27) 99984-0101.\n\nAbraço,\nAlexandre Queiroz')
        wa = (f'Olá! Aqui é o Alexandre Queiroz, do email sobre {service} para a {nome}. Obrigado pelo retorno! '
              f'Consigo te mostrar em 15 min o que faria e o valor — hoje à tarde ou amanhã de manhã, qual prefere?')
        return em, wa
    em = (f'Olá! Obrigado por responder. Sou engenheiro de software aqui de Vitória e escrevi porque trabalho com {service} '
          f'para negócios como a {nome}.{achado}\n\n'
          f'Se fizer sentido, te explico em 5 minutos por WhatsApp o que eu faria e quanto custa — sem compromisso. '
          f'Meu número: (27) 99984-0101.\n\nAbraço,\nAlexandre Queiroz')
    wa = (f'Olá! Aqui é o Alexandre Queiroz, que escreveu sobre {service} para a {nome}. Vi que respondeu o email — '
          f'posso te explicar em 5 min o que eu faria e o valor?')
    return em, wa


def hot_whatsapp_draft(lead):
    """Lead que clicou (sem responder): mensagem curta p/ o Alexandre disparar 1 a 1."""
    nome = lead.get('name') or 'vocês'; service = lead.get('service') or 'o site'; f = _finding(lead)
    achado = f' Olhei o site de vocês e {f}.' if f else ''
    return (f'Olá! Aqui é o Alexandre Queiroz — mandei um email sobre {service} para a {nome}.{achado} '
            f'Se quiser, te explico em 5 minutos o que eu faria e quanto custa, sem compromisso. Pode ser agora?')


# ---------- rede/DB ----------
def load_state():
    try: return json.load(open(STATE, encoding='utf-8'))
    except Exception: return {'last_uid': 0, 'seen': {}}


def save_state(st):
    os.makedirs(os.path.dirname(STATE), exist_ok=True)
    json.dump(st, open(STATE, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)


def _dec(s):
    out = []
    for part, enc in decode_header(s or ''):
        if isinstance(part, bytes):
            try: part = part.decode(enc or 'utf-8', 'ignore')
            except LookupError: part = part.decode('latin-1', 'ignore')   # ex.: 'unknown-8bit'
        out.append(part)
    return ''.join(out)


def _body_text(msg):
    if msg.is_multipart():
        for p in msg.walk():
            if p.get_content_type() == 'text/plain' and not p.get('Content-Disposition'):
                return p.get_payload(decode=True).decode(p.get_content_charset() or 'utf-8', 'ignore')
        for p in msg.walk():
            if p.get_content_type() == 'text/html':
                return re.sub(r'<[^>]+>', ' ', p.get_payload(decode=True).decode(p.get_content_charset() or 'utf-8', 'ignore'))
        return ''
    return msg.get_payload(decode=True).decode(msg.get_content_charset() or 'utf-8', 'ignore')


def fetch_new(st):
    user = os.environ.get('HOSTGATOR_CONTATO_EMAIL'); pwd = os.environ.get('HOSTGATOR_CONTATO_PASS')
    if not user or not pwd: log('sem HOSTGATOR_CONTATO_*'); return []
    M = imaplib.IMAP4_SSL('mail.alexandrequeiroz.com.br', 993); M.login(user, pwd); M.select('INBOX', readonly=True)
    last = int(st.get('last_uid') or 0)
    if last == 0:   # primeira execução: só as últimas 48h
        since = (dt.date.today() - dt.timedelta(days=2)).strftime('%d-%b-%Y')
        _, d = M.uid('search', None, f'(SINCE {since})')
    else:
        _, d = M.uid('search', None, f'(UID {last + 1}:*)')
    uids = [int(u) for u in d[0].split() if int(u) > last]
    out = []
    for u in uids[-60:]:
        _, raw = M.uid('fetch', str(u), '(BODY.PEEK[])')
        if not raw or not raw[0]: continue
        msg = email.message_from_bytes(raw[0][1])
        frm = email.utils.parseaddr(_dec(msg.get('From', '')))[1].lower()
        hdrs = {k: msg.get(k) for k in ('Auto-Submitted', 'Precedence', 'X-Autoreply', 'X-Autorespond') if msg.get(k)}
        out.append({'uid': u, 'from': frm, 'subject': _dec(msg.get('Subject', '')), 'headers': hdrs,
                    'text': re.sub(r'\s+', ' ', _body_text(msg))[:2000]})
    M.logout()
    return out


def lead_context(emails):
    """{email: {customer_id,name,phone,website,seg,service,status}} só p/ quem recebeu wave."""
    if not emails: return {}
    import db as crmdb
    cx = crmdb.connect(); cur = cx.cursor(); marks = ','.join('?' * len(emails))
    cur.execute(f"""
      SELECT LOWER(q.recipient_email) em, MAX(CAST(q.customer_id AS varchar(36))) cid, MAX(q.recipient_name) nome,
             MAX(c.name) camp, MAX(cu.phone) phone, MAX(cu.whats_app) wa, MAX(cu.website) website, MAX(cu.status) status
      FROM email_queue_items q JOIN email_campaigns c ON c.id=q.campaign_id
      LEFT JOIN customers cu ON cu.id=q.customer_id
      WHERE q.status=2 AND c.name LIKE 'AQ - %' AND LOWER(q.recipient_email) IN ({marks})
      GROUP BY LOWER(q.recipient_email)""", *emails)
    ctx = {}
    for r in cur.fetchall():
        m = re.match(r'AQ - (\w+) - ([^-]+) -', r.camp or '')
        ctx[r.em] = {'customer_id': r.cid, 'name': r.nome or '', 'phone': (r.wa or r.phone or ''), 'website': r.website or '',
                     'seg': m.group(1) if m else '', 'service': (m.group(2).strip() if m else 'site profissional'),
                     'status': r.status or 0}
    cx.close()
    return ctx


def tg(text):
    tok = os.environ.get('TELEGRAM_BOT_TOKEN'); chat = os.environ.get('TELEGRAM_CHAT_ID')
    if not tok or not chat or DRY: return
    requests.post(f'https://api.telegram.org/bot{tok}/sendMessage', json={'chat_id': chat, 'text': text[:3900], 'parse_mode': 'HTML',
                  'disable_web_page_preview': True}, timeout=20)


def api_login():
    r = requests.post(BASE + '/api/v1/auth/login', json={'email': os.environ['DIAX_ADMIN_EMAIL'],
                      'password': os.environ['DIAX_ADMIN_PASSWORD']}, timeout=20).json()
    return {'Authorization': 'Bearer ' + (r.get('accessToken') or r.get('token')), 'Content-Type': 'application/json'}


def crm_task(H, title, desc, priority=4):
    due = (dt.datetime.now(dt.timezone.utc)).strftime('%Y-%m-%dT21:00:00Z')
    r = requests.post(BASE + '/api/v1/tasks', headers=H, timeout=30,
                      json={'title': title[:180], 'description': desc[:1900], 'priority': priority, 'dueDate': due})
    return r.status_code in (200, 201)


def crm_status(H, cid, status):
    r = requests.patch(BASE + f'/api/v1/customers/{cid}/status', headers=H, timeout=30, json={'status': status})
    return r.status_code == 200


def suppress(em):
    import db as crmdb
    cx = crmdb.connect(autocommit=False); cur = cx.cursor()
    cur.execute("""INSERT INTO email_suppressions (id, user_id, email, reason, source, suppressed_at, created_at, created_by)
      SELECT TOP 1 NEWID(), q.user_id, ?, 2, 'reply_watch_negative', SYSUTCDATETIME(), SYSUTCDATETIME(), 'reply_watch'
      FROM email_queue_items q WHERE LOWER(q.recipient_email)=? AND NOT EXISTS (SELECT 1 FROM email_suppressions s WHERE LOWER(s.email)=?)""",
                em, em, em)
    n = cur.rowcount; cx.commit(); cx.close(); return n


def main():
    load_env()
    st = load_state()
    msgs = fetch_new(st)
    if not msgs:
        return
    ctx = lead_context(sorted({m['from'] for m in msgs if '@' in m['from']}))
    H = None; handled = 0
    for m in msgs:
        st['last_uid'] = max(int(st.get('last_uid') or 0), m['uid'])
        em = m['from']
        if em not in ctx or is_auto_reply(m['subject'], em, m['headers']):
            continue
        seen = st['seen'].get(em)
        if seen and (dt.date.today() - dt.date.fromisoformat(seen['at'])).days < 7:
            continue
        lead = dict(ctx[em]); lead['email'] = em
        intent = classify_intent(m['text'])
        try:
            r = check_site(lead['website'])
            lead['findings'] = summarize_fup(r['findings'], lead['name'] or 'vocês') or []
        except Exception:
            lead['findings'] = []
        em_draft, wa_draft = build_drafts(lead, intent)
        link = wa_link(lead['phone'], wa_draft) if wa_draft else ''
        icon = {'positive': '🟢', 'negative': '🔴', 'neutral': '🟡'}[intent]
        snippet = m['text'][:300]
        tgmsg = (f'{icon} <b>RESPOSTA</b> ({intent}) — <b>{lead["name"] or em}</b>\n{em}\n'
                 f'<i>{snippet}</i>\n\n<b>Rascunho email:</b>\n{em_draft}'
                 + (f'\n\n<b>WhatsApp:</b> {link}' if link else (f'\n\n<b>WhatsApp (sem telefone no CRM):</b> {wa_draft}' if wa_draft else '')))
        log(f'{icon} {em} [{intent}] {lead["seg"]} | {snippet[:80]}')
        if DRY:
            print(tgmsg); handled += 1; continue
        tg(tgmsg)
        H = H or api_login()
        title = f'{icon} RESPONDEU ({intent}): {lead["name"] or em}'
        desc = f'{em} | {lead["seg"]} / {lead["service"]}\nDisse: {snippet}\n\nRASCUNHO EMAIL:\n{em_draft}' + (f'\n\nWHATSAPP: {link}' if link else '')
        crm_task(H, title, desc, priority=4 if intent != 'negative' else 2)
        if intent == 'negative':
            suppress(em)
        elif lead.get('customer_id') and (lead.get('status') or 0) < 2:
            crm_status(H, lead['customer_id'], 2)
        st['seen'][em] = {'at': dt.date.today().isoformat(), 'intent': intent, 'seg': lead['seg']}
        handled += 1
    if not DRY:
        save_state(st)
    log(f'{len(msgs)} msgs novas, {handled} respostas tratadas')


if __name__ == '__main__':
    main()
