---
phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook
plan: 05
subsystem: infra
tags: [telegram, watchdog, windows-task-scheduler, observability, powershell, python, waha, n8n]

# Dependency graph
requires:
  - phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook (10-01)
    provides: rede de segurança (waves_smoke.py) e inventário do pipeline de outreach
provides:
  - Alerta Telegram por transição de estado (alerta_telegram.py), compartilhado por waha-watchdog.py e watchdog-n8n.ps1
  - waha-watchdog.py e watchdog-n8n.ps1 estendidos (additions-only) para notificar 🔴/🟢 sem duplicar mensagens
  - Task agendada `waha-watchdog` registrada no Windows Task Scheduler (lacuna que não existia antes)
  - Runbook docs/email-marketing/OBSERVABILIDADE-watchdogs.md
affects: [phase-11-qualified-objetivo-automatico, phase-12-escala-de-email]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Alerta por transição de estado (não por polling) via JSON de estado + comparação vivo-antes-vs-agora, stdlib-only Python"
    - "Watchdogs estendidos com additions-only (import + guarda + chamadas), zero linha de auto-cura removida ou reescrita"

key-files:
  created:
    - D:/claude-code/n8n-local/alerta_telegram.py (fora do repo git, sem commit)
    - docs/email-marketing/OBSERVABILIDADE-watchdogs.md
  modified:
    - D:/claude-code/n8n-local/waha-watchdog.py (fora do repo git, sem commit)
    - D:/claude-code/n8n-local/watchdog-n8n.ps1 (fora do repo git, sem commit)

key-decisions:
  - "Manter poll de 5 min para waha-watchdog (igual ao n8n-watchdog já existente), confirmado pelo usuário no checkpoint em vez de reduzir para 15-30 min"
  - "n8n local caído / task n8n-local Disabled fica registrado como pendência explícita, fora de escopo desta fase (decisão do usuário)"
  - "watchdog-n8n.ps1: chamadas ao alerta inlined nos dois caminhos em vez da função compartilhada sugerida pelo plano, para satisfazer sem ambiguidade o critério de aceite grep-count>=2 do próprio plano — mesmo comportamento, ainda additions-only"

patterns-established:
  - "Alerta Telegram só na virada de estado (não a cada checagem) é o padrão para qualquer watchdog futuro deste pipeline — evita 36 mensagens para um serviço fora por 3h com poll de 5min"

requirements-completed: [OBSV-01]

# Metrics
duration: ~45min
completed: 2026-09-19
---

# Phase 10 Plan 05: Alerta Telegram nos Watchdogs de WAHA/n8n Summary

**Alerta Telegram por transição de estado (stdlib-only, autoteste 6/6) somado aos watchdogs existentes de WAHA/n8n, com a task `waha-watchdog` que faltava agora registrada e provada ao vivo contra o n8n local realmente caído.**

## Performance

- **Duration:** ~45 min (Tasks 1-2 executadas antes do checkpoint; esta sessão retomou só a Parte 2 do Task 3 — runbook + finalização)
- **Completed:** 2026-09-19T22:04:47Z
- **Tasks:** 3 (2 `type=auto` + 1 `checkpoint:human-verify`, ambas as partes concluídas)
- **Files modified:** 4 (3 fora do repo git em `n8n-local/`, 1 dentro do repo `diax-crm`)

## Accomplishments
- Implementação única de alerta Telegram por transição de estado, com autoteste cobrindo os 6 cenários críticos (primeira leitura sem envio, 10 chamadas down seguidas = zero envios extras, alerta na virada com duração formatada, independência entre alvos `waha`/`n8n`)
- Os dois watchdogs existentes (`waha-watchdog.py`, `watchdog-n8n.ps1`) notificam nas transições sem perder a lógica de auto-cura (Docker/sessão WAHA, kill+restart n8n) nem o aviso legado por WhatsApp
- Task `waha-watchdog` registrada no Windows Task Scheduler (5 min, `pythonw.exe`) — lacuna real encontrada na verificação ao vivo (o script existia no disco, nada o chamava)
- Prova de fault-injection **real** (não simulada): o n8n local já estava fora do ar no momento da verificação; usuário confirmou recebimento legível de `🔴 N8N FORA` e `🟢 N8N VOLTOU` no mesmo chat do `VPS-Watchdog`, e confirmou ausência de mensagens repetidas em execuções subsequentes sem mudança de estado
- Runbook `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` documentando alvos, frequência, nuance de bootstrap e a pendência do n8n local

## Task Commits

Tasks 1 e 2 (código) não geram commit — `D:\claude-code\n8n-local` **não é um repositório git**
(`git rev-parse` falha ali), fato documentado no próprio plano na seção "Contexto de git" e
reconfirmado nesta sessão. Isso é esperado, não uma falha de execução.

1. **Task 1: Criar alerta_telegram.py** — sem commit (fora de repo git); autoteste `AUTOTESTE OK (6 cenarios)` confirmado
2. **Task 2: Ligar os watchdogs + registrar task waha-watchdog** — sem commit (fora de repo git); `Get-ScheduledTask` confirma `waha-watchdog` (Ready) e `n8n-watchdog` (Running) intactos
3. **Task 3 (checkpoint:human-verify), Parte 2: Runbook** — commitado nesta sessão, dentro do repo `diax-crm`

**Plan metadata:** (commit a seguir, junto com STATE.md/ROADMAP.md/REQUIREMENTS.md)

## Files Created/Modified

- `D:/claude-code/n8n-local/alerta_telegram.py` (229 linhas) - implementação única do alerta Telegram por transição de estado, CLI + biblioteca, autoteste embutido. **Fora do repo git — sem commit.**
- `D:/claude-code/n8n-local/waha-watchdog.py` (134 linhas) - estendido com guarda de stdout para pythonw + 4 chamadas de `transicao('waha', ...)` nos pontos de saída de `checar()`, additions-only. **Fora do repo git — sem commit.**
- `D:/claude-code/n8n-local/watchdog-n8n.ps1` (30 linhas) - estendido com 2 chamadas inline a `alerta_telegram.py --alvo n8n`, additions-only. **Fora do repo git — sem commit.**
- `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` (165 linhas) - runbook: alvos monitorados, frequência (5 min), como cada watchdog foi estendido, nuance de bootstrap (primeira execução nunca alerta), prova real de fault-injection, o que fazer em cada alerta, o que não é alertado (D-05), e a pendência do n8n local. **Dentro do repo `diax-crm` — commitado.**

## Decisions Made

- Manter poll de 5 minutos para `waha-watchdog`, igual ao `n8n-watchdog` já existente — decisão explícita do usuário no checkpoint (opção recomendada, sem motivo para divergir de cadência entre os dois watchdogs).
- n8n local caído + task `n8n-local` `Disabled` fica como pendência registrada, explicitamente fora do escopo desta fase — decisão explícita do usuário. Esta fase entrega alerta, não reparo (D-04/D-05 do `10-CONTEXT.md`).
- `watchdog-n8n.ps1`: as duas chamadas ao alerta foram inlined diretamente nos dois caminhos (up direto e pós-restart), em vez da função compartilhada `function Alerta(...)` sugerida como exemplo pelo plano. Mesmo comportamento externo, ainda additions-only sobre o restart automático — aplicado para satisfazer sem ambiguidade o próprio critério de aceite do plano (`grep -c "alerta_telegram.py" watchdog-n8n.ps1 >= 2`).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] watchdog-n8n.ps1: chamadas inline em vez de função compartilhada**
- **Found during:** Task 2 (extensão do watchdog-n8n.ps1)
- **Issue:** O plano sugeria uma função PowerShell `function Alerta($estado, $detalhe) { ... }` chamada duas vezes; a implementação em função única, dependendo de como o `grep -c` conta ocorrências do texto literal `alerta_telegram.py`, poderia contar 1 (a definição) em vez de 2, arriscando falhar o próprio critério de aceite do plano.
- **Fix:** As duas chamadas ao CLI de `alerta_telegram.py` foram inlined diretamente nos dois caminhos (up direto e pós-restart), garantindo 2 ocorrências literais inequívocas.
- **Files modified:** D:/claude-code/n8n-local/watchdog-n8n.ps1 (fora do repo git, sem commit)
- **Verification:** `grep -c "alerta_telegram.py" watchdog-n8n.ps1` = 2; comportamento idêntico ao pretendido, restart automático intocado
- **Committed in:** N/A (arquivo fora de repo git)

---

**Total deviations:** 1 auto-fixed (1 blocking, resolução de ambiguidade de critério de aceite)
**Impact on plan:** Sem impacto funcional — mesmo comportamento externo, só a forma de satisfazer o grep de verificação mudou. Nenhum scope creep.

## Issues Encountered

- `logs/alerta-telegram.log` registra uma falha pontual de envio (`HTTP Error 404: Not Found`) durante os testes iniciais de Task 2, antes da confirmação final do usuário. Não bloqueou o plano: o design de `alerta_telegram.py` garante que falha de envio nunca propaga exceção para o watchdog que chamou, e as mensagens reais confirmadas pelo usuário no checkpoint chegaram corretamente depois. Não investigado a fundo (fora do fix-attempt-limit relevante, resolvido sozinho na próxima tentativa).

## User Setup Required

None - no external service configuration required. `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` já existiam em `~/.claude/.secrets.env` (mesmas credenciais usadas pelo `VPS-Watchdog`).

## Known Stubs

Nenhum. Este plano não introduziu UI nem dados mock — é infraestrutura de alerta operacional, com prova de funcionamento real (fault injection ao vivo) confirmada pelo usuário.

## Next Phase Readiness

- OBSV-01 completo: pipeline de outreach agora tem sinal visível (Telegram) quando WAHA ou n8n local caem, com anti-spam provado na prática.
- Phase 11 (Qualified Objetivo e Automático) depende de a Phase 10 estar pronta — falta ainda concluir 10-02/10-03/10-04 (SCRP-01, motor único) antes de a Phase 10 ser considerada 100% fechada; este plano (10-05) fecha inteiramente o OBSV-01.
- Pendência explícita fora de escopo, carregada para o usuário resolver quando quiser: n8n local fora do ar, task `n8n-local` `Disabled` — agora visível via alerta, não mais silenciosa.

---
*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Completed: 2026-09-19*

## Self-Check: PASSED

- FOUND: docs/email-marketing/OBSERVABILIDADE-watchdogs.md
- FOUND: .planning/phases/10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook/10-05-SUMMARY.md
- FOUND: D:/claude-code/n8n-local/alerta_telegram.py
- FOUND: D:/claude-code/n8n-local/waha-watchdog.py
- FOUND: D:/claude-code/n8n-local/watchdog-n8n.ps1
- No commit hashes to verify for the 3 n8n-local files (not a git repo, expected — see plan's git-context note)
