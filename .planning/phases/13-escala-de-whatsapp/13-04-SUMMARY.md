---
phase: 13-escala-de-whatsapp
plan: 04
subsystem: whatsapp-outreach
tags: [whatsapp, decision-function, runbook, requirements-traceability, python]

# Dependency graph
requires:
  - phase: 13-escala-de-whatsapp
    plan: 01
    provides: "wa_estado.py — leitor unico read-only do estado real do canal (audit log + sqlite mode=ro do n8n + CRM)"
  - phase: 13-escala-de-whatsapp
    plan: 03
    provides: "taxa de resposta wa_es medida em previews/whatsapp-weekly.json, usada como referencia cruzada"
provides:
  - "wa_escalation.py — decisao pura wa_es->wa_br com limiares travados antes da medicao e gate --apply"
  - "previews/wa-escalation-state.json — veredito corrente + historico das leituras"
  - "ESCALA-WHATSAPP-runbook.md — runbook consolidado da Phase 13 (WHAT-01..04)"
  - "Rastreabilidade WHAT-01..04 fechada Complete, sem contradicao entre REQUIREMENTS.md e ROADMAP.md"
affects: [14-dashboard-de-conversao-do-funil]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "decide() funcao pura (metricas + limiares + data -> veredito+motivo, zero I/O), copiado da forma de dmarc_escalation.py (Fase 12) e aplicado a um dominio novo sem reusar codigo"
    - "portao_atual() le o codigo-fonte real (export_whatsapp_leads.py/outreach-send.py) em vez de assumir a existencia de uma trava — mesmo espirito de 13-02 (verificar o real antes de decidir o mecanismo)"
    - "limiares escritos, comentados e commitados ANTES da 1a medicao real, com o commit servindo de prova de ordem auditavel por git log/diff (mesma disciplina de honestidade da Fase 12)"

key-files:
  created:
    - docs/email-marketing/wa_escalation.py
    - docs/email-marketing/ESCALA-WHATSAPP-runbook.md
  modified:
    - docs/email-marketing/previews/wa-escalation-state.json
    - docs/email-marketing/previews/whatsapp-estado.json
    - docs/email-marketing/previews/whatsapp-weekly.json
    - .planning/REQUIREMENTS.md
    - .planning/ROADMAP.md

key-decisions:
  - "Limiares MIN_ENVIADOS_ES=60/MIN_RESPOSTAS_ES=10/TAXA_MINIMA=10%/TETO_RECUSA=8% escritos exatamente como especificados no plano e commitados (d4fedba2) ANTES de rodar --status pela primeira vez contra dados reais — nao ajustados depois de ver o resultado"
  - "metricas_segmento() usa wa_estado.ler_envios() (audit log de outreach-send.py, desde 2026-09-13) como fonte de 'enviados', nao outreach-sent.json (que tem 186 chat_ids all-time incluindo sends anteriores ao audit log, de outra era/mecanismo) — decisao literal do plano (13-04-PLAN.md linha 157), resultando em numeros mais limpos e atribuiveis ao emissor oficial declarado em 13-01"
  - "WHAT-04 fecha Complete (nao Partial/INCONCLUSIVO como o CONTEXT.md/13-RESEARCH.md previam) porque a medicao real teve amostra suficiente (121 enviados, 53 respostas) e veredito honesto AVANCAR — a previsao de amostra pequena nao se confirmou aos dados reais desta sessao, e a regra do plano e reportar o resultado medido, nao o esperado"
  - "--apply nunca liga nada de operacional mesmo com veredito AVANCAR: portao_atual() confirma que outreach-send.py:escolhe() nunca filtrou por segmento, entao nao ha portao de codigo para abrir — --apply so grava o veredito e imprime a mensagem fixa 'SEM PORTAO DE SEGMENTO'"

patterns-established:
  - "Fechamento de requisito por equivalencia-por-descoberta quando o mecanismo pedido (um 'portao' de segmento) nunca existiu: a decisao pura valida com dados reais um comportamento que o sistema ja executa por design (fila ordenada, sem exclusao), em vez de forcar a criacao de uma trava artificial so para ter algo para 'ligar' — mesmo espirito do fechamento de WHAT-01 por equivalencia funcional (13-01)"

requirements-completed: [WHAT-01, WHAT-02, WHAT-03, WHAT-04]

# Metrics
duration: ~20min
completed: 2026-09-20
---

# Phase 13 Plan 04: Escalonamento wa_es->wa_br + Runbook Consolidado + Fechamento da Rastreabilidade Summary

**`wa_escalation.py` mede a taxa de resposta real de `wa_es` (43,8%, 53/121 desde 2026-09-13) contra limiares travados antes da medição e devolve veredito honesto `AVANCAR` — descobrindo, ao ler o código-fonte real, que o "portão" wa_es→wa_br nunca existiu como trava em `outreach-send.py:escolhe()`; fecha a Phase 13 com runbook consolidado e WHAT-01..04 Complete nos dois documentos de rastreabilidade.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-20T21:38:00Z (aprox.)
- **Completed:** 2026-09-20T21:46:00Z
- **Tasks:** 3 (todas `type="auto"`, sem checkpoint)
- **Files modified:** 7 (2 criados: `wa_escalation.py`, `ESCALA-WHATSAPP-runbook.md`; 5 modificados: `wa-escalation-state.json`, `whatsapp-estado.json`, `whatsapp-weekly.json`, `REQUIREMENTS.md`, `ROADMAP.md`)

## Accomplishments

- `docs/email-marketing/wa_escalation.py` criado (355 linhas) — `metricas_segmento()`, `decide()`
  (função pura), `portao_atual()`, `mensagem_portao()`, `apply_gate()`, `commit_state()`, `selftest()`,
  `main()`. Limiares escritos e **commitados (`d4fedba2`) antes de rodar `--status` pela primeira
  vez contra dados reais** — a ordem é auditável por `git log`, não apenas afirmada.
- `--selftest` 12/12 OK (excede o mínimo de 7 exigido), cobrindo: amostra insuficiente vence antes
  de qualquer outra regra (mesmo com taxa aparente ótima); amostra insuficiente por respostas
  mesmo com envios suficientes; taxa abaixo do mínimo com amostra suficiente → `SEGURAR`; taxa OK
  mas recusa acima do teto → `SEGURAR`; caso feliz → `AVANCAR`; `enviados=0` → `taxa=None` sem
  exceção; `decide()` puro (só dicts, sem I/O); `--apply` recusa quando veredito != `AVANCAR`
  (3 casos); `mensagem_portao()` nunca finge uma ação que não existe (2 casos).
- **Medição real** (`--status`, histórico completo do audit log de `outreach-send.py` desde
  2026-09-13, quando o log começou): `wa_es` = 121 enviados, 53 respondidos (taxa **43,8%**), 7
  recusas (5,8%) — acima de todos os 4 limiares. Veredito: **`AVANCAR`**, gravado em
  `previews/wa-escalation-state.json`.
- **Achado central (não esperado pelo `13-RESEARCH.md`/`CONTEXT.md`, que previam amostra pequena
  demais):** `portao_atual()` lê o código-fonte real de `export_whatsapp_leads.py` e
  `outreach-send.py:escolhe()` e confirma que o emissor oficial (declarado em 13-01) **nunca
  filtrou por segmento** — só por STOP/blacklist/sent/janela/CAP/prioridade/encoding. O "portão"
  wa_es→wa_br que o requisito pede para abrir nunca existiu como trava de código. Por isso
  `--apply`, mesmo com `AVANCAR`, não liga nada — imprime `SEM PORTAO DE SEGMENTO: wa_br ja entra
  na mesma fila, ordenada wa_es primeiro — nada a ligar`.
- `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` publicado (317 linhas, 9 seções: mapa do canal,
  WHAT-01..04, "O que o CRM não sabe", "Armadilhas conhecidas", bateria de verificação com saída
  real colada desta sessão, "O que não é versionável"). Todo comando citado foi rodado ao vivo
  (`wa_estado.py --status`, `crm_leads_sync.py --status`, `whatsapp_weekly_report.py --dry`,
  `wa_escalation.py --selftest`/`--status`, `schtasks /query` das 4 tasks envolvidas).
- Rastreabilidade fechada: WHAT-01..04 **Complete** nos dois documentos (`REQUIREMENTS.md` e
  `ROADMAP.md`), script de conferência anti-contradição do plano confirmou `ka==kb` para os 4
  requisitos, nenhum `Pending`. Phase 13 fechada **4/4 planos**.

## Task Commits

1. **Task 1: wa_escalation.py — decisão wa_es -> wa_br com limiar documentado e gate --apply** - `d4fedba2` (feat)
2. **Task 2: Runbook consolidado ESCALA-WHATSAPP-runbook.md** - `d5b25445` (docs)
3. **Task 3: Fechar a rastreabilidade WHAT-01..04 sem contradição** - `5972179e` (docs)

**Plan metadata:** este SUMMARY + `state advance-plan` (a seguir)

## Files Created/Modified

- `docs/email-marketing/wa_escalation.py` - decisão pura wa_es→wa_br (`metricas_segmento`, `decide`,
  `portao_atual`, `apply_gate`, `commit_state`, `selftest`, `main`), CLI `--status`/`--selftest`/`--apply`
- `docs/email-marketing/previews/wa-escalation-state.json` - veredito corrente (`AVANCAR`) + histórico
- `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` - runbook consolidado da fase (WHAT-01..04)
- `docs/email-marketing/previews/whatsapp-estado.json` / `whatsapp-weekly.json` - re-gravados pela
  bateria de verificação desta sessão (mesmos dados citados no runbook, sem mudança de conteúdo
  operacional)
- `.planning/REQUIREMENTS.md` - WHAT-04 marcado `[x]` e `Complete` na tabela de Traceability, com
  evidência citável (limiares, medição real, achado do portão)
- `.planning/ROADMAP.md` - Phase 13 marcada `[x]`/`completed 2026-09-20`, Plans/Progress/Coverage
  Map atualizados, Nota de execução com as divergências vs `13-RESEARCH.md`

## Decisions Made

- **Fonte de "enviados" para `metricas_segmento()`:** `wa_estado.ler_envios()` (audit log de
  `outreach-send.py`, começou em 2026-09-13), não `outreach-sent.json` (186 chat_ids all-time,
  incluindo sends de antes do audit log existir). Decisão literal do plano (linha 157 do
  13-04-PLAN.md: "usar `wa_estado.ler_envios()`..."), resultando em números atribuíveis
  exclusivamente ao emissor oficial declarado em 13-01, não a uma era/mecanismo anterior.
- **WHAT-04 fecha `Complete`, não `Partial`/`INCONCLUSIVO`** como o `CONTEXT.md`/`13-RESEARCH.md`
  previam (amostra pequena esperada). A medição real desta sessão teve amostra suficiente (121
  enviados, 53 respostas, ambos acima dos mínimos) e veredito honesto `AVANCAR` — a regra de
  honestidade do plano é reportar o resultado medido, não o resultado esperado. Nenhum limiar foi
  ajustado para produzir esse resultado (prova: `git log` mostra o commit dos limiares antes de
  qualquer medição real).
- **`--apply` não liga nada de operacional mesmo com `AVANCAR`:** como `portao_atual()` confirma
  que nunca existiu portão de código, `--apply` só grava o veredito com `aplicado=true` e imprime
  a mensagem fixa `SEM PORTAO DE SEGMENTO` — nunca finge uma ação que não existe. Isso não foi
  executado nesta sessão (só `--status`/`--selftest`), preservando o comportamento operacional
  exatamente como estava.

## Deviations from Plan

None - plano executado exatamente como escrito, incluindo a ordem exigida (limiares commitados
antes da 1ª medição real) e o resultado honesto (`AVANCAR`, não o `INCONCLUSIVO` antecipado pelo
CONTEXT.md — reportado como medido, não forçado a bater com a expectativa).

## Issues Encountered

- O primeiro `--status` gravou o campo `observacao` de `portao_atual()` com um caractere de
  substituição (`�`) quando exibido no terminal do Bash tool (Git Bash/MSYS em codepage não-UTF-8).
  Verificado que é **puramente visual**: os bytes reais gravados em
  `previews/wa-escalation-state.json` são UTF-8 corretos (`\xe2\x80\x94`, o em-dash "—"),
  confirmado via leitura binária do arquivo. Nenhuma correção de código necessária — não é um bug
  do módulo, é a mesma peculiaridade de encoding de terminal já documentada em sessões anteriores
  deste projeto (regra global "PYTHONIOENCODING=utf-8 nos scripts Windows").

## User Setup Required

None - nenhuma configuração externa necessária.

## Next Phase Readiness

- **Phase 13 (Escala de WhatsApp) concluída 4/4 planos.** WHAT-01..04 fechados `Complete` em
  `REQUIREMENTS.md` e `ROADMAP.md`, sem contradição entre os dois documentos.
- `wa_escalation.py --status` fica disponível para reavaliação contínua sem exigir nova sessão de
  planejamento — se a fila `wa_es` se esgotar e a taxa cair, a próxima execução reflete isso
  automaticamente, sem mexer nos limiares.
- Próximo passo do roadmap v1.4: `/gsd:plan-phase 14` (Dashboard de Conversão do Funil, DASH-01).
- Trabalho não relacionado do usuário (`reply_watch.py`, `CLAUDE.md`, `crm-web/tsconfig.tsbuildinfo`)
  permanece intacto e não commitado, confirmado por `git status --porcelain` antes e depois desta task.
- Nenhum push feito — commits locais em `chore/email-automation-versioned`.

---
*Phase: 13-escala-de-whatsapp*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/wa_escalation.py
- FOUND: docs/email-marketing/previews/wa-escalation-state.json
- FOUND: docs/email-marketing/ESCALA-WHATSAPP-runbook.md
- FOUND: .planning/phases/13-escala-de-whatsapp/13-04-SUMMARY.md
- FOUND: commit d4fedba2 (Task 1)
- FOUND: commit d5b25445 (Task 2)
- FOUND: commit 5972179e (Task 3)
