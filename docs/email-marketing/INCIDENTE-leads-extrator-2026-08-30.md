# Incidente 2026-08-30 — leads lixo do extrator entrando no funil de email

## Root cause (provado por evidência)
- O backend do extrator NÃO roda mais em container: roda no **host** da VPS 185.173.110.180
  como systemd unit `extrator-api.service` (`/opt/extrator-api/`, gunicorn em `172.17.0.1:8000`),
  snapshot de código **antigo, sem git e sem o fix `CRM_PUSH_ENABLED`** (commit `d7ef269c` do
  repo extrator-diax nunca foi deployado — vive só na branch local `semana3`).
- Esse código legado tem **auto-PUSH**: o pipeline diário (02:00 BRT) scrapeia e ao final
  sincroniza para o CRM (`journalctl`: `[DAILY] Sincronizando 54 leads com CRM...` em
  30/08 14:03 UTC → leads criados no CRM 14:04 UTC com `created_by` = admin).
  O job `daily_crm_sync` das 09:00 BRT está quebrado (`column reference "id" is ambiguous`) —
  quem empurrava era o pipeline diário.
- Qualidade: fonte `search_engine` sem filtros novos → spam-trap (`jqs@sun.com`),
  domínios estrangeiros (`blok.ai`, `overchat.ai`), cidades mal parseadas ("Janeiro"/RJ),
  source gravado como `1` (Manual) em vez de `4` (Scraping).

## Ações tomadas (30/08, ~17:30–18:00 BRT)
1. **Quarentena reversível** dos 205 leads do cohort `notes LIKE 'Origem: Extrator de Dados%'
   AND created_at >= '2026-08-26'`: `email_opt_out=1`, `updated_by='quarantine-extrator-20260830'`.
   - Evidência (estado anterior completo): `quarantine-extrator-20260830.json`
   - Reversão: `quarantine_revert_20260830.py` (`--only-good` pula lixo evidente)
   - Fila de envio estava vazia (0 itens status=0); 6 leads do cohort já tinham sido emailados antes.
2. **Push legado desligado na origem**: guard `CRM_PUSH_ENABLED` (default OFF) inserido em
   `sync_leads_batch_to_alexandrequeiroz()` no host (`/opt/extrator-api/app.py`), cobrindo TODOS
   os caminhos de push (pipeline diário, auto-sync, sync manual). `py_compile` ok, serviço
   reiniciado, health interno 200.
   - Backup pré-edit: `/opt/extrator-api/app.py.bak_pre_push_disable_20260830`
   - Rollback: restaurar o .bak OU `Environment=CRM_PUSH_ENABLED=1` no unit + restart.
3. **Bonus do restart**: backend interno voltou a responder (200 em 0.4s). O acesso EXTERNO
   (`api.extratordedados.com.br`) segue 000: Traefik roteia para o serviço Easypanel extinto;
   gunicorn só escuta na bridge `172.17.0.1`. Corrigir roteamento é pendência separada.

## Validação
- Dry-run `daily_waves.py --dry --force` pós-quarentena: plano de segunda =
  `logistica(4) + advocacia(6)`; candidatos auditados um a um no DB — todos empresas BR
  plausíveis (.com.br/.adv.br). Nenhum lead do cohort quarentenado selecionável.
- `SELECT` pós-update: 0 leads do cohort com `email_opt_out=0`; 205 marcados.

## Pendências (fora do caminho crítico de segunda)
- Deployar os 34 commits da branch `semana3` do extrator (PULL autenticado + qualidade) e
  aposentar o snapshot `/opt/extrator-api`.
- Consertar roteamento Traefik → host (externo 000) OU apontar o PULL do CRM para o
  binding correto.
- Agendar PULL automático no CRM (hoje só manual) com atribuição de source=4 correta.
- Triagem fina dos 205 quarentenados (reverter os bons com `--only-good`).
- `daily_crm_sync` legado quebrado (SQL ambíguo) — irrelevante se o push ficar off.
