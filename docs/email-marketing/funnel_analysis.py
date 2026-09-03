# -*- coding: utf-8 -*-
"""Analise de funil p/ priorizar otimizacoes por impacto. Read-only."""
import sys
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db as crmdb
cx = crmdb.connect(); cur = cx.cursor()

def q1(sql):
    cur.execute(sql); return cur.fetchone()

print("=== 1. VOLUME (envios/dia vs capacidade 900) ===")
cur.execute("""SELECT CONVERT(date,sent_at), COUNT(*) FROM email_queue_items
  WHERE status=2 AND sent_at > DATEADD(day,-14,SYSUTCDATETIME()) GROUP BY CONVERT(date,sent_at) ORDER BY 1""")
for r in cur.fetchall(): print(f'  {r[0]}: {r[1]} enviados')

print("\n=== 2. DELIVERABILITY & ENGAGEMENT (envios +24h, 30d) ===")
r = q1("""SELECT COUNT(*),
  SUM(CASE WHEN opened_at IS NOT NULL THEN 1 ELSE 0 END),
  SUM(CASE WHEN read_count>0 THEN 1 ELSE 0 END),
  SUM(CASE WHEN status=3 THEN 1 ELSE 0 END)
  FROM email_queue_items WHERE sent_at < DATEADD(hour,-24,SYSUTCDATETIME()) AND sent_at > DATEADD(day,-30,SYSUTCDATETIME()) AND status IN (2,3)""")
sent, opened, read, failed = [x or 0 for x in r]
print(f'  enviados: {sent} | open: {opened} ({100*opened/max(sent,1):.1f}%) | failed/bounce: {failed}')
# bounces/suppressions totais
r = q1("SELECT COUNT(*) FROM email_suppressions")
print(f'  suppressions totais (bounces+unsub acumulados): {r[0]}')

print("\n=== 3. CONVERSAO (funil lead->customer) ===")
for st, lbl in [(0,'Lead'),(1,'Contacted'),(2,'Qualified'),(3,'Negotiating'),(4,'Customer')]:
    r = q1(f"SELECT COUNT(*) FROM customers WHERE status={st}")
    print(f'  {lbl}: {r[0]}')

print("\n=== 4. ESTOQUE (combustivel corporativo nunca-emailado) ===")
r = q1("""SELECT COUNT(*) FROM customers WHERE (email_sent_count=0 OR email_sent_count IS NULL)
  AND last_email_sent_at IS NULL AND email_opt_out=0 AND email IS NOT NULL AND email<>''
  AND email NOT LIKE '%@gmail%' AND email NOT LIKE '%@hotmail%' AND email NOT LIKE '%@outlook%' AND email LIKE '%.com.br'""")
print(f'  corporativo .com.br disponivel: {r[0]}')
r = q1("""SELECT COUNT(*) FROM customers WHERE email_opt_out=0 AND (email_sent_count=0 OR email_sent_count IS NULL)
  AND last_email_sent_at IS NULL AND email LIKE '%@gmail%'""")
print(f'  webmail nao-usado (desperdicado? envio ignora): {r[0]}')

print("\n=== 5. PERFORMANCE POR NICHO (open rate, 30d) ===")
cur.execute("""SELECT LEFT(c.name, CHARINDEX(' - ', c.name+' - ')-1) niche,
  SUM(c.sent_count), SUM(c.open_count), SUM(c.click_count)
  FROM email_campaigns c WHERE c.created_at > DATEADD(day,-30,SYSUTCDATETIME()) AND c.name LIKE 'AQ - %'
  GROUP BY LEFT(c.name, CHARINDEX(' - ', c.name+' - ')-1) HAVING SUM(c.sent_count)>0 ORDER BY 3 DESC""")
rows = cur.fetchall()
for r in rows[:20]:
    s,o,cl = r[1] or 0, r[2] or 0, r[3] or 0
    print(f'  {(r[0] or "")[:20]:20} sent={s:3} open={o} ({100*o/max(s,1):.0f}%) click={cl}')

print("\n=== 6. FOLLOW-UP & HOT LEADS ===")
r = q1("""SELECT COUNT(DISTINCT recipient_email) FROM email_queue_items q JOIN email_campaigns c ON c.id=q.campaign_id
  WHERE c.name LIKE 'AQ - %' AND q.status=2""")
print(f'  contatos unicos ja emailados (AQ): {r[0]}')
r = q1("SELECT COUNT(*) FROM email_events WHERE event_type=3")  # opened events
r2 = q1("SELECT COUNT(*) FROM email_events WHERE event_type=4")  # clicked?
print(f'  eventos open ledger: {r[0]} | click ledger: {r2[0]}')
cx.close()
