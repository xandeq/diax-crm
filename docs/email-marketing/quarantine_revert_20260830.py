# -*- coding: utf-8 -*-
"""Reverte a quarentena de 30/08 (email_opt_out=1 no cohort extrator 26/08+).

Uso: python quarantine_revert_20260830.py [--only-good] [--mark b]
  (sem flag)   reverte TODOS os leads do snapshot A (205, cohort 26-30/08)
  --mark b     opera no snapshot B (75: lote 25/08 + emails '%' + dominios lixo)
  --only-good  reverte apenas leads triados: pula lixo evidente (dominio estrangeiro,
               webmail nao-corp ja excluido pelos filtros de envio, spam-trap)

So reverte leads que continuam com updated_by='quarantine-extrator-20260830'
(nao sobrescreve mudancas manuais posteriores). Idempotente.
"""
import sys, json, re
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db as crmdb

_suffix = 'b' if '--mark' in sys.argv and sys.argv[sys.argv.index('--mark') + 1:sys.argv.index('--mark') + 2] == ['b'] else ''
MARK = 'quarantine-extrator-20260830' + _suffix
EVID = rf'D:\claude-code\diax-crm\docs\email-marketing\quarantine-extrator-20260830{_suffix}.json'

BAD_PATTERNS = re.compile(r'@(sun\.com|blok\.ai|overchat\.ai)$|\.(ai|fi|io)$')

snap = json.load(open(EVID, encoding='utf-8'))
only_good = '--only-good' in sys.argv
ids = []
for ld in snap['leads']:
    if ld['email_opt_out_before']:
        continue  # ja era opt-out antes da quarentena — nao reverter
    if only_good and ld.get('email') and BAD_PATTERNS.search(ld['email'].lower()):
        continue
    ids.append(ld['id'])

print(f'{len(ids)} leads a reverter (de {snap["count"]} no snapshot)')
cx = crmdb.connect(autocommit=False); cur = cx.cursor()
n = 0
for i in range(0, len(ids), 100):
    chunk = ids[i:i+100]
    ph = ','.join('?' * len(chunk))
    cur.execute(f"""UPDATE customers SET email_opt_out=0,
                    updated_by='quarantine-revert-20260830', updated_at=SYSUTCDATETIME()
                    WHERE id IN ({ph}) AND updated_by=?""", *chunk, MARK)
    n += cur.rowcount
cx.commit()
print(f'revertidos: {n}')
cur.execute("SELECT COUNT(*) FROM customers WHERE updated_by=?", MARK)
print('ainda em quarentena:', cur.fetchone()[0])
cx.close()
