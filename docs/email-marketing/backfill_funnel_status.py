# -*- coding: utf-8 -*-
"""Backfill de status do funil (verdade dos dados): emailado->Contacted, clicou->Qualified.
Evidencia + marca rastreavel (reversivel). fetch_leads usa email_sent_count, NAO status,
entao nao afeta selecao de envio."""
import sys, json, datetime
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db as crmdb
EVID = r'D:\claude-code\diax-crm\docs\email-marketing\backfill-funnel-status-20260903.json'
cx = crmdb.connect(autocommit=False); cur = cx.cursor()

# snapshot dos afetados (id, status atual) p/ reversao
cur.execute("""SELECT id, status FROM customers
  WHERE (status=0 AND (email_sent_count>0 OR last_email_sent_at IS NOT NULL))
     OR (status<2 AND id IN (SELECT DISTINCT customer_id FROM email_events WHERE event_type=4 AND customer_id IS NOT NULL))""")
snap = [{'id': str(r[0]), 'status_before': r[1]} for r in cur.fetchall()]
with open(EVID, 'w', encoding='utf-8') as f:
    json.dump({'taken': datetime.datetime.now(datetime.UTC).isoformat(), 'count': len(snap), 'rows': snap}, f)
print(f'evidencia: {len(snap)} leads -> {EVID}')

# 1. Contacted: emailado mas status=0
cur.execute("""UPDATE customers SET status=1, updated_by='funnel-backfill-20260903', updated_at=SYSUTCDATETIME()
  WHERE status=0 AND (email_sent_count>0 OR last_email_sent_at IS NOT NULL)""")
print('-> Contacted (0->1):', cur.rowcount)

# 2. Qualified: clicou (event_type=4) e status<2
cur.execute("""UPDATE customers SET status=2, updated_by='funnel-backfill-20260903', updated_at=SYSUTCDATETIME()
  WHERE status<2 AND id IN (SELECT DISTINCT customer_id FROM email_events WHERE event_type=4 AND customer_id IS NOT NULL)""")
print('-> Qualified (<2->2, clicaram):', cur.rowcount)
cx.commit()

# verificacao
for st, lbl in [(0,'Lead'),(1,'Contacted'),(2,'Qualified'),(3,'Negotiating'),(4,'Customer')]:
    cur.execute(f"SELECT COUNT(*) FROM customers WHERE status={st}")
    print(f'  {lbl}: {cur.fetchone()[0]}')
cx.close()
