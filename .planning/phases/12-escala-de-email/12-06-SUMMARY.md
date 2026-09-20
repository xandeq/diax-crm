---
phase: 12-escala-de-email
plan: 06
subsystem: email-marketing
tags: [dmarc, warmup, ab-testing, webmail, traceability, checkpoint-resolution]

# Dependency graph
requires:
  - phase: 12-01..12-05
    provides: dmarc_escalation.py, waves_core.py warmup curve, ab_report.py, build_webmail_wave(), DMARC-escalonamento.md
provides:
  - "docs/email-marketing/ESCALA-EMAIL-runbook.md — runbook consolidado dos 4 mecanismos da Phase 12"
  - "EMAL-01..04 fechados na rastreabilidade (REQUIREMENTS.md + ROADMAP.md) com status honesto e evidência citável"
  - "Nota de execução na Phase 12 do ROADMAP.md explicando a leitura dos 4 critérios de sucesso à luz da decisão aguardar"
affects: [13-escala-de-whatsapp, 14-dashboard-de-conversao-do-funil]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Runbook consolidado por fase (mesmo padrão de OBSERVABILIDADE-watchdogs.md/QUALIFIED-avanco-automatico.md): o que é, onde roda, prova verbatim, limites, próximos fios"
    - "Checkpoint humano com resolução aguardar: quando a única alternativa era forçar --force fora de calendário, a resposta correta é deixar a execução agendada natural confirmar, e documentar exatamente o que checar quando ela rodar"

key-files:
  created:
    - docs/email-marketing/ESCALA-EMAIL-runbook.md
  modified:
    - .planning/REQUIREMENTS.md
    - .planning/ROADMAP.md
    - .planning/STATE.md

key-decisions:
  - "Checkpoint da Task 2 resolvido com aguardar: usuário optou por não forçar (--force) uma execução real de daily_waves.py num domingo (2026-09-20, fora do trigger seg-sex 08:40 BRT) — a task agendada DIAX-Email-Waves-Daily confirma sozinha na segunda-feira 2026-09-21 08:40 BRT sem precisar de --force"
  - "Regra de honestidade do plano aplicada literalmente: EMAL-02 e EMAL-04 corrigidos de Complete (sem evidência citável) para Partial, porque o resultado depende de uma execução real que ainda não aconteceu"
  - "EMAL-03 fecha Complete (não Partial) porque a medição de A/B já roda sobre dados reais de produção existentes (936 envios/30d), sem depender de nenhum envio futuro — o requisito pede que a medição exista e seja consultável, não que o veredito seja conclusivo"
  - "Phase 12 marcada completa (6/6 planos) no ROADMAP.md apesar de 3 dos 4 requisitos ficarem Partial — decisão explícita de que 'trabalho de mecanismo 100% entregue' é um estado de fase legítimo, distinto de 'requisito 100% atingido'; documentado na Nota de execução"

patterns-established: []

requirements-completed: [EMAL-01, EMAL-02, EMAL-03, EMAL-04]

# Metrics
duration: 20min
completed: 2026-09-20
---

# Phase 12 Plan 06: Fechamento da Phase 12 — Runbook Consolidado + Checkpoint Aguardar Summary

**Runbook consolidado ESCALA-EMAIL-runbook.md publicado e rastreabilidade de EMAL-01..04 fechada honestamente após o usuário responder `aguardar` ao checkpoint de execução real do motor.**

## Performance

- **Duration:** ~20 min (retomada pós-checkpoint, só Task 3 + fechamento)
- **Completed:** 2026-09-20
- **Tasks:** 3 (Task 1 executada em sessão anterior — commit `7a0a6dd6`; Task 2 checkpoint resolvido `aguardar`; Task 3 executada nesta sessão)
- **Files modified:** 4 (`ESCALA-EMAIL-runbook.md`, `REQUIREMENTS.md`, `ROADMAP.md`, `STATE.md`)

## Accomplishments

- Runbook consolidado (`docs/email-marketing/ESCALA-EMAIL-runbook.md`, 336+ linhas, 9 seções) publicado na Task 1 (sessão anterior), documentando os 4 mecanismos da Phase 12 com números medidos ao vivo e a seção obrigatória de realidade de estoque.
- Checkpoint humano da Task 2 resolvido: usuário respondeu `aguardar` — não forçar execução real fora de calendário num domingo; confirmação fica para a execução agendada natural de segunda-feira.
- Rastreabilidade honesta fechada: EMAL-02/EMAL-04 corrigidos de "Complete" (sem prova citável) para "Partial" (mecanismo entregue e provado, confirmação real pendente); EMAL-03 fechado "Complete" com evidência completa; contradição ROADMAP.md vs REQUIREMENTS.md em EMAL-02 (achada por `12-VERIFICATION.md`) corrigida — os dois documentos agora concordam.
- Runbook atualizado com nota explícita de "o que conferir na segunda-feira 2026-09-21" para fechar EMAL-02/04 sem precisar de nova sessão de planejamento.
- Phase 12 marcada `6/6 planos` completa no ROADMAP.md, com Nota de execução explicando como ler os 4 critérios de sucesso à luz da decisão `aguardar`.

## Checkpoint da Task 2 — resposta do usuário (verbatim, via checkpoint_resolution)

> **Checkpoint:** Task 2, human-verify — forçar uma execução real (não-dry) hoje (domingo,
> fora de calendário, exigiria `--force`) ou aguardar a execução agendada natural de amanhã
> (segunda-feira 2026-09-21, 08:40 BRT).
>
> **Resposta do usuário: `aguardar`** — não forçar execução real hoje. A task agendada
> `DIAX-Email-Waves-Daily` roda de verdade amanhã sem precisar de `--force`, já que o guard de
> fim de semana passa naturalmente em dia útil.

Nenhum arquivo do repositório foi alterado por essa task (é um gate de verificação puro, sem `--force`
nem envio real disparado por este agente, conforme instruído). O checkpoint apresentou (na sessão
anterior, antes da pausa): estado da Scheduled Task, e as quatro linhas de prova capturadas do
`--dry --force` (reproduzidas abaixo, também em `ESCALA-EMAIL-runbook.md` §7):

```
WARMUP dia 1/8 — alvo 87/dia · 4 provider(s) · MAX_PER_SEG=8
DRY: alvo do dia = 87 (warmup dia 1)
DRY: plano = [('marketing', 4), ('consultoria', 4)]
DRY: webmail = 12 leads (cap 15, já hoje 0)
```

**nslookup final do `_dmarc`** (reproduzido em `ESCALA-EMAIL-runbook.md` §6, medido na Task 1):

```
$ python dmarc_escalation.py --status
Zona WHM: pct=25 (Line 125)
  txtdata: v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r
State file: dmarc_pct=25 dmarc_changed_at=2026-09-20
  Divergencia zona vs state: nenhuma
```

## Task Commits

1. **Task 1: Prova integrada em dry-run + runbook consolidado** — `7a0a6dd6` (docs, sessão anterior)
2. **Task 2: Checkpoint humano** — sem commit (gate de verificação, nenhum arquivo alterado); resolvido `aguardar`
3. **Task 3: Fechar EMAL-01..04 na rastreabilidade** — `96ac7b5` (docs)

**Plan metadata:** commit deste SUMMARY + STATE.md (a seguir)

## Files Created/Modified

- `docs/email-marketing/ESCALA-EMAIL-runbook.md` — runbook consolidado dos 4 mecanismos da fase (criado na Task 1, complementado na Task 3 com a nota de resolução do checkpoint e o que conferir segunda-feira)
- `.planning/REQUIREMENTS.md` — EMAL-02/04 corrigidos para `Partial` com evidência citável; EMAL-03 corrigido para `Complete` com evidência citável
- `.planning/ROADMAP.md` — Phase 12 marcada completa (6/6 planos), Coverage Map corrigido (EMAL-02 não diverge mais entre documentos), Nota de execução adicionada
- `.planning/STATE.md` — posição atualizada (Phase 12 completa), decisão registrada, Session Continuity atualizada

## Decisions Made

- Checkpoint da Task 2 resolvido com `aguardar` (ver acima) — decisão do usuário, não do agente.
- EMAL-02/EMAL-04 fecham `Partial` (não `Complete`) porque o critério literal do requisito (volume real entregue / campanha real com destinatários confirmados) depende de uma execução que ainda não aconteceu.
- EMAL-03 fecha `Complete` porque a medição em si (não o veredito) é o que o requisito pede, e ela já roda sobre dados reais de produção sem depender de nenhum envio futuro.
- Phase 12 marcada completa (6/6 planos) no ROADMAP.md mesmo com 3/4 requisitos `Partial` — entendida como "trabalho de mecanismo 100% entregue", um estado de fase legítimo e distinto de "requisito 100% atingido", documentado explicitamente na Nota de execução para não confundir leitores futuros.

## Deviations from Plan

**Adaptação explícita instruída pelo orquestrador para o desfecho `aguardar`** (não é um desvio das Rules 1-4, é a execução da Task 3 conforme a própria regra do plano para este cenário — ver 12-06-PLAN.md linhas 337-338): nenhum envio real foi disparado; Task 3 executada como "fechamento honesto pendente de confirmação" em vez de "fechamento com prova de execução real", exatamente como o plano já previa para o caso `aguardar`.

Nenhum outro desvio — plano executado conforme escrito para o cenário `aguardar`.

## Issues Encountered

None.

## User Setup Required

None - nenhuma configuração externa necessária. Ação humana pendente (não é setup): conferir a mensagem do Telegram e a campanha `auto wm` no CRM na segunda-feira 2026-09-21, conforme documentado em `ESCALA-EMAIL-runbook.md` §1 (nota do checkpoint).

## Next Phase Readiness

- Phase 12 (Escala de Email) está 100% concluída no nível de entrega de mecanismo (6/6 planos). Pendência observacional (não de trabalho): confirmar EMAL-02/EMAL-04 como `Complete` após a execução agendada de 2026-09-21 08:40 BRT — instruções exatas em `ESCALA-EMAIL-runbook.md` §1.
- Próximo passo: `/gsd:plan-phase 13` (Escala de WhatsApp) — trilha independente, não bloqueada por nenhuma pendência da Phase 12.
- `reply_watch.py`, `CLAUDE.md` e `crm-web/tsconfig.tsbuildinfo` permanecem com o trabalho não relacionado do usuário intacto (não tocados nesta sessão).

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: `docs/email-marketing/ESCALA-EMAIL-runbook.md`
- FOUND: `.planning/phases/12-escala-de-email/12-06-SUMMARY.md`
- FOUND: `.planning/REQUIREMENTS.md`
- FOUND: `.planning/ROADMAP.md`
- FOUND: `.planning/STATE.md`
- FOUND commit: `7a0a6dd6` (Task 1: runbook consolidado)
- FOUND commit: `96ac7b5` (Task 3: rastreabilidade fechada)
