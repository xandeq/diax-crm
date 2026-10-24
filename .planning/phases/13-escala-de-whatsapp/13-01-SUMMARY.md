---
phase: 13-escala-de-whatsapp
plan: 01
subsystem: whatsapp-outreach
tags: [whatsapp, waha, n8n, sqlite, python, scheduled-task, requirements-traceability]

# Dependency graph
requires:
  - phase: 10-motor-de-cadencia-unico
    provides: "OBSV-01 watchdogs (waha-watchdog/watchdog-n8n) que ja monitoram WAHA/n8n"
provides:
  - "wa_estado.py — leitor unico e reusavel do estado real do canal WhatsApp (logs + sqlite n8n + CRM), consumido por 13-02/13-03/13-04"
  - "previews/whatsapp-estado.json — snapshot baseline datado, regeravel por comando"
  - "WHAT-01 fechado (Complete por equivalencia funcional) com decisao humana registrada e prova de execucao real"
affects: [13-02-crm-leads-sync, 13-03-relatorio-semanal, 13-04-escalonamento-wa]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Leitor read-only multi-fonte (log + sqlite mode=ro + SQL Server) com zero I/O em tempo de import, copiado do padrao dmarc_escalation.py"
    - "Checkpoint humano bloqueante ANTES de qualquer flip de active/CAP, resolvido por decisao textual citavel em vez de acao automatica"

key-files:
  created:
    - docs/email-marketing/wa_estado.py
  modified:
    - docs/email-marketing/previews/whatsapp-estado.json
    - .planning/REQUIREMENTS.md
    - .planning/ROADMAP.md

key-decisions:
  - "Checkpoint da Task 2 resolvido com `python-oficial`: outreach-send.py/task outreach-wa declarado handoff oficial de WHAT-01; waOutreach1a1AQ permanece active=0, desligado de proposito, sem qualquer alteracao"
  - "WHAT-01 fecha por equivalencia funcional (o artefato citado literalmente no requisito continua desligado) em vez de pela letra do texto — mesmo padrao ja usado em QUAL-01/02 na Phase 11"

patterns-established:
  - "Fechamento de requisito por equivalencia funcional: quando o requisito nomeia um artefato especifico mas outro caminho, comprovadamente seguro e rodando, cumpre o mesmo proposito, documentar a decisao do usuario e citar as propriedades de seguranca em vez de forcar reativacao do artefato original"

requirements-completed: [WHAT-01]

# Metrics
duration: ~25min (Task 1 executada antes do checkpoint; Task 3 resumida apos decisao do usuario, ~11min)
completed: 2026-09-20
---

# Phase 13 Plan 01: Estado Real do Canal WhatsApp + Fechamento de WHAT-01 Summary

**`wa_estado.py` como leitor único read-only do canal WhatsApp (audit log + sqlite `mode=ro` do n8n + CRM), e WHAT-01 fechado Complete por equivalência funcional após checkpoint humano decidir `python-oficial` — zero mudança operacional em qualquer emissor.**

## Performance

- **Duration:** ~25 min total (Task 1 antes do checkpoint + Task 3 nesta retomada, ~11 min)
- **Started:** 2026-09-20T17:30:00-03:00 (aprox., Task 1)
- **Completed:** 2026-09-20T18:08:40-03:00 (commit final da Task 3)
- **Tasks:** 3 (Task 1 auto, Task 2 checkpoint:decision, Task 3 auto)
- **Files modified:** 4 (`wa_estado.py` criado; `previews/whatsapp-estado.json`, `REQUIREMENTS.md`, `ROADMAP.md` modificados)

## Accomplishments

- `docs/email-marketing/wa_estado.py` criado — único leitor do estado real do canal (logs Python + sqlite `~/.n8n/database.sqlite` só-leitura + CRM SQL Server), `--selftest` 7/7 OK, `--json`/`--status` funcionais, zero rede/DB em tempo de import.
- Snapshot baseline `previews/whatsapp-estado.json` gravado e regravado ao final, com a divergência logs-vs-CRM medida em número (não estimada).
- Checkpoint humano bloqueante da Task 2 apresentado com números re-medidos na hora; usuário respondeu **`python-oficial`**.
- WHAT-01 fechado **Complete por equivalência funcional** na rastreabilidade (`REQUIREMENTS.md`, `ROADMAP.md`), com evidência citável.
- Zero mudança operacional: nenhum CAP alterado, nenhuma task do Windows tocada, `waOutreach1a1AQ` permanece `active=0` exatamente como estava.

## Task Commits

1. **Task 1: wa_estado.py — leitor único do estado real + snapshot datado** - `7d30590b` (feat)
2. **Task 2: Checkpoint — postura de WHAT-01** - sem commit (checkpoint de decisão, decisão do usuário: `python-oficial`)
3. **Task 3: Executar a decisão e provar execução real** - `ed155009` (docs)

**Plan metadata:** este SUMMARY + `state advance-plan` (a seguir)

## Files Created/Modified

- `docs/email-marketing/wa_estado.py` - leitor único do estado real do canal (logs, sqlite n8n, CRM); contrato `ler_envios/ler_sent/ler_blacklist/ler_respostas/ler_n8n/ler_crm/seg/snapshot` para 13-02/03/04 importarem
- `docs/email-marketing/previews/whatsapp-estado.json` - snapshot baseline, regravado ao final da Task 3 (`gerado: 2026-09-20T18:07:41`)
- `.planning/REQUIREMENTS.md` - WHAT-01 marcado `[x]` e Complete na tabela de Traceability, com citação da decisão, das propriedades de segurança e dos números medidos
- `.planning/ROADMAP.md` - progresso da Phase 13 (1/4 planos), tabela de Coverage Map e nota de execução atualizadas

## Decisions Made

- **Checkpoint humano (Task 2) — decisão `python-oficial`:** declarar `outreach-send.py` (task Windows `outreach-wa`, `CAP_DIA=18`) o handoff oficial de WHAT-01. `waOutreach1a1AQ` (n8n) permanece `active=0`, desligado de propósito, exatamente como estava — não reativado, não editado, CAP inalterado. Task Windows `outreach-wa`/`n8n-local` não tocadas.
- **Racional documentado:** `outreach-send.py` já roda com segurança comprovada (dedupe com releitura verificada `grava_verificado()`, blacklist como primeira checagem, kill-switch `logs/OUTREACH-STOP`, 186 envios all-time, 16/18 hoje). WHAT-01 é satisfeito por equivalência funcional (existe um handoff real, seguro e rodando) em vez de pela letra do texto do requisito original (que nomeava literalmente o workflow n8n). Mesmo padrão já usado quando a Phase 11 encontrou QUAL-01/02 satisfeitos por código diferente do esperado.
- `waOutreach1a1AQ` continua desligado por causa do incidente de 12/09: `fs.writeFileSync` não confiável no Code node do n8n → `catch` vazio engoliu o erro → dedupe cego → 7ª mensagem para um lead que já havia recusado em 10/09.

## Deviations from Plan

None - plan executado exatamente como escrito, incluindo a adaptação explícita pedida no checkpoint de resolução (ramo `python-oficial` = zero mudança operacional, apenas re-captura de snapshot final e fechamento de rastreabilidade).

## Issues Encountered

- `schtasks /query` via Git Bash reescreve `/query` como caminho (`C:/Program Files/Git/query`), quebrando o comando. Contornado com `MSYS_NO_PATHCONV=1 schtasks /query ...`. Não é um bug do plano nem do código — é peculiaridade do ambiente Git Bash no Windows, documentado aqui para replicação futura (13-02/03/04 podem precisar do mesmo contorno).

## Prova de Execução Real (citada verbatim)

Snapshot final `previews/whatsapp-estado.json` (`gerado: "2026-09-20T18:07:41"`):

- `outreach-sent.json` (all-time): **186 chat_ids**
- Enviados hoje: **16/18** (task `outreach-wa`)
- Último `ENVIADO`: `2026-09-20 17:30:13 <email-redacted>@redacted.invalid | Cominoti Construções`
- `waOutreach1a1AQ`: `active=0`, `cap=2`, `max_prio=4`, `ultima_execucao=2026-09-12 17:05:36.307` (idêntico ao medido no planejamento — nenhuma mudança)
- `8nKTlX4Y7Hy1EUEn` (bot): `active=1`, `crm_leads=1057`, `ultima_execucao=2026-09-12 17:14:17.329` (inerte, hook aponta para porta 5678 fechada — não tocado)
- Divergência logs-vs-CRM: `{'enviados_logs': 186, 'enviados_crm': 17, 'delta': 169}`
- Scheduled Tasks confirmadas via `schtasks /query`: `outreach-wa` — Status `Ready`, Next Run Time `9/21/2026 9:00:00 AM`, repetição 30min até 18h; `outreach-watch` — Status `Ready`, Next Run Time `9/20/2026 6:20:00 PM`, repetição 20min.

## Arquivos NÃO versionáveis tocados nesta task (sem commit possível)

Nenhum arquivo fora do repositório `diax-crm` foi tocado nesta task (ramo `python-oficial` não exige edição em `D:\claude-code\n8n-local\**`, `~/.n8n/database.sqlite` ou `D:\claude-code\_scripts\**` — só leitura via `wa_estado.py` e `schtasks /query`). Os únicos arquivos escritos foram os do próprio repositório, todos commitados.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `wa_estado.py` está pronto para ser importado por 13-02 (`crm_leads_sync.py`), 13-03 (`whatsapp_weekly_report.py`) e 13-04 (`wa_escalation.py`) sem reimplementar leitura de log/sqlite/CRM.
- WHAT-01 fechado, destravando o restante da Phase 13 (WHAT-02/03/04) sem dependência pendente desta fase.
- Trabalho não relacionado do usuário (`reply_watch.py`, `CLAUDE.md`, `crm-web/tsconfig.tsbuildinfo`) permanece intacto e não commitado, confirmado por `git status --porcelain` antes e depois desta task.
- Nenhum push feito — commits locais em `chore/email-automation-versioned`.

---
*Phase: 13-escala-de-whatsapp*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/wa_estado.py
- FOUND: docs/email-marketing/previews/whatsapp-estado.json
- FOUND: .planning/phases/13-escala-de-whatsapp/13-01-SUMMARY.md
- FOUND: commit 7d30590b (Task 1)
- FOUND: commit ed155009 (Task 3)
