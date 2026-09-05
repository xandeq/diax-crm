# -*- coding: utf-8 -*-
"""Exporta leads do CRM prontos p/ WhatsApp 1-a-1 (consumido pela sessão/n8n que tem o WAHA).

Saída: previews/whatsapp-leads.json (+ .csv) — lista ordenada por prioridade:
  P1 respondeu email (positivo/neutro)  · P2 clicou (status Qualified)
  P3 recebeu FUP2 c/ diagnóstico (7-21d)  · P4 emailado sem sinal  · P5 nunca contatado
Segmentos: wa_es (DDD 27/28) > wa_br. Só celular BR válido (DDD+9). Exclui opt-out (email
ou WhatsApp), respostas negativas, suprimidos, já contatado por WhatsApp nos últimos 30d.
Cada item traz a mensagem pronta (hot_whatsapp_draft) com o achado real do site (cache).
Uso: python export_whatsapp_leads.py [--es-only] [--limit N] [--write-tags]
  --write-tags grava 'wa_es'/'wa_br' em customers.tags (mutação prod; default OFF).
"""
import sys, os, re, json, csv, datetime as dt
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db as crmdb
from site_check import summarize_fup, _load_cache, _host, classify_url, Finding
from reply_watch import hot_whatsapp_draft, load_state as reply_state

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
OUT = DIR + 'previews/whatsapp-leads'
ES_DDD = {'27', '28'}


def normalize_br(phone):
    """-> '55DDD9XXXXXXXX' (E.164 sem +) só p/ celular BR válido; senão None."""
    d = re.sub(r'\D', '', phone or '')
    if d.startswith('55') and len(d) >= 12: d = d[2:]
    if len(d) == 11 and d[2] == '9' and d[:2] not in ('00',): return '55' + d
    return None


def main():
    es_only = '--es-only' in sys.argv
    limit = int(sys.argv[sys.argv.index('--limit') + 1]) if '--limit' in sys.argv else 0
    write_tags = '--write-tags' in sys.argv
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT c.id, c.name, c.company_name, c.email, c.phone, c.whats_app, c.website, c.status, c.tags,
             c.email_sent_count, c.last_whats_app_sent_at, c.whats_app_opt_out, c.email_opt_out,
             (SELECT MAX(cp.name) FROM email_queue_items q JOIN email_campaigns cp ON cp.id=q.campaign_id
               WHERE q.customer_id=c.id AND q.status=2 AND cp.name LIKE 'AQ - %') camp,
             (SELECT COUNT(*) FROM email_events e WHERE e.customer_id=c.id AND e.event_type=3) clicks,
             (SELECT COUNT(*) FROM email_suppressions s WHERE LOWER(s.email)=LOWER(c.email)) suppressed
      FROM customers c
      WHERE ((c.phone<>'' AND c.phone IS NOT NULL) OR (c.whats_app<>'' AND c.whats_app IS NOT NULL))
        AND c.email_opt_out=0 AND c.whats_app_opt_out=0 AND c.status<4""")
    rows = cur.fetchall(); cx.close()
    st = reply_state().get('seen', {})
    replies = {e: v for e, v in st.items()}
    cache = _load_cache(); today = dt.date.today()
    items = []
    for r in rows:
        tel = normalize_br(r.whats_app) or normalize_br(r.phone)
        if not tel: continue
        ddd = tel[2:4]
        seg = 'wa_es' if ddd in ES_DDD else 'wa_br'
        if es_only and seg != 'wa_es': continue
        if r.suppressed: continue
        em = (r.email or '').lower()
        rep = replies.get(em)
        if rep and rep.get('intent') == 'negative': continue
        if r.last_whats_app_sent_at and (dt.datetime.utcnow() - r.last_whats_app_sent_at).days < 30: continue
        m = re.match(r'AQ - (\w+) - ([^-]+) -', r.camp or '')
        niche, service = (m.group(1), m.group(2).strip()) if m else ('', 'site profissional')
        if rep: prio, why = 1, f'respondeu email ({rep.get("intent")})'
        elif (r.clicks or 0) > 0 or (r.status or 0) >= 2: prio, why = 2, f'clicou {r.clicks}x / status {r.status}'
        elif (r.email_sent_count or 0) >= 2: prio, why = 3, 'recebeu wave + follow-up'
        elif (r.email_sent_count or 0) == 1: prio, why = 4, 'recebeu 1 email'
        else: prio, why = 5, 'nunca contatado'
        nome = (r.company_name or r.name or '').strip()
        # achado do site só do cache (sem rede aqui) — o vigia/waves já povoaram
        findings = []
        try:   # SÓ cache (previews/site-checks.json): nada de rede aqui — 5k leads = horas
            host = _host(r.website) if r.website and classify_url(r.website) == 'site' else ''
            ent = cache.get(host) if host else None
            if ent:
                fs = [Finding(*f) for f in ent['findings']]
                if fs and fs[0].code != 'inconclusive':
                    findings = summarize_fup(fs, nome or 'vocês') or []
        except Exception:
            pass
        lead = {'id': str(r.id), 'name': nome, 'email': em, 'phone_e164': tel, 'chat_id': tel + '@c.us',
                'segment': seg, 'ddd': ddd, 'priority': prio, 'why': why, 'niche': niche, 'service': service,
                'website': r.website or '', 'status': r.status, 'finding': findings[0] if findings else '',
                'message': hot_whatsapp_draft({'name': nome, 'service': service, 'findings': findings})}
        items.append(lead)
    items.sort(key=lambda x: (x['priority'], 0 if x['segment'] == 'wa_es' else 1, -(x['status'] or 0)))
    if limit: items = items[:limit]
    os.makedirs(DIR + 'previews', exist_ok=True)
    json.dump({'generated': dt.datetime.now().isoformat(timespec='seconds'), 'count': len(items),
               'rules': 'só celular BR válido; sem opt-out/negativo/suprimido; 1 msg por lead; 30d cooldown',
               'leads': items}, open(OUT + '.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    with open(OUT + '.csv', 'w', encoding='utf-8-sig', newline='') as f:
        w = csv.DictWriter(f, fieldnames=list(items[0].keys()) if items else ['id']); w.writeheader(); w.writerows(items)
    from collections import Counter
    print(f'{len(items)} leads -> {OUT}.json/.csv')
    print('por prioridade:', sorted(Counter(i["priority"] for i in items).items()))
    print('por segmento:', Counter(i['segment'] for i in items).most_common())
    print('com achado do site:', sum(1 for i in items if i['finding']))
    if write_tags and items:
        cx = crmdb.connect(autocommit=False); cur = cx.cursor(); n = 0
        for i in items:
            cur.execute("UPDATE customers SET tags = CASE WHEN tags IS NULL OR tags='' THEN ? WHEN tags LIKE ? THEN tags ELSE tags + ', ' + ? END, updated_by='wa_segment', updated_at=SYSUTCDATETIME() WHERE id=?",
                        i['segment'], f'%{i["segment"]}%', i['segment'], i['id']); n += cur.rowcount
        cx.commit(); cx.close(); print('tags gravadas:', n)


if __name__ == '__main__':
    main()
