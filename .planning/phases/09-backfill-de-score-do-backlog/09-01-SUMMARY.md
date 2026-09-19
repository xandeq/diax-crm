---
phase: 09-backfill-de-score-do-backlog
plan: 01
subsystem: testing
tags: [xunit, ef-core-inmemory, python, requests, lead-scoring]

# Dependency graph
requires:
  - phase: 08-import-scoring-dedup
    provides: LeadScoringService.CalculateScore/SegmentForScore com teto de fit 45 e RecomputeAllAsync como motor único de recompute
provides:
  - Teste EF-InMemory que prova, contra a query real de CustomerRepository, que GetAllLeadsAsync e GetSegmentCountsAsync excluem Status=Inactive(5)/Churned(6)
  - scripts/backfill_lead_scores.py — gatilho determinístico + captura de evidência antes/depois do backfill oficial via POST /api/v1/pipeline/recompute-scores
  - Decisão D-09-01 registrada (efeito colateral de até 10 follow-up tasks é aceito)
affects: [09-02-backfill-execution]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Script Python one-off contra produção segue o padrão de mark_junk_inactive.py: import de load_secrets/login de fetch_and_classify_leads.py, nunca reimplementa lógica de negócio"
    - "Teste de repositório EF-InMemory segue o padrão de CustomerRepositoryExternalIdTests.cs: FakeCurrentUserService + CreateDbContext + SeedCustomer + Assert.* puro (sem FluentAssertions)"

key-files:
  created:
    - api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs
    - scripts/backfill_lead_scores.py
  modified: []

key-decisions:
  - "D-09-01: o efeito colateral de criar até 10 follow-up tasks (MaxFollowUpsPerRun) via POST /recompute-scores é ACEITO sem bypass — é o comportamento de produção já vigente do LeadScoringWorker diário; construir um followUpOwnerUserId:null exigiria mudar o contrato do endpoint, fora de escopo da BFIL-01. Não reabrir esta questão no plano 09-02."

patterns-established:
  - "Backfill de dados de produção nunca reimplementa lógica de negócio em Python — sempre dispara o endpoint C# oficial e verifica via endpoints de leitura já existentes (sales-dashboard, leads)"

requirements-completed: [BFIL-01]

# Metrics
duration: 12min
completed: 2026-09-19
---

# Phase 9 Plan 1: Trava de Teste + Script de Gatilho do Backfill Summary

**Teste EF-InMemory provando exclusão Inactive/Churned na query real + script Python que dispara `POST /api/v1/pipeline/recompute-scores` e captura evidência antes/depois — zero lógica de scoring nova.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-09-19T12:13:00Z (aprox.)
- **Completed:** 2026-09-19T12:25:21Z
- **Tasks:** 2
- **Files modified:** 2 (ambos novos)

## Accomplishments
- `CustomerRepositoryLeadFilterTests.cs` prova, contra `DiaxDbContext` InMemory real (não mock), que `GetAllLeadsAsync` retorna exatamente os 4 status abertos (Lead/Contacted/Qualified/Negotiating) e exclui Customer/Inactive/Churned; e que `GetSegmentCountsAsync` (fonte dos contadores Hot/Warm/Cold do sales-dashboard) também ignora leads Inactive/Churned mesmo quando eles têm Segment atribuído.
- `scripts/backfill_lead_scores.py` criado como orquestrador HTTP puro: `--snapshot-only` (somente leitura), `--run` (dispara o backfill oficial e compara antes/depois), `--idempotency` (segunda rodada + `sys.exit(2)` se houver divergência). Zero aritmética de score em Python — confirmado por grep negativo nos acceptance criteria.
- Suíte completa `dotnet test -c Release` sobe de 891 para 894 (exatamente os 3 testes novos), 100% verde.

## Task Commits

Each task was committed atomically:

1. **Task 1: Travar por teste a exclusão de Inactive/Churned no nível da query EF real** - `32daa35` (test)
2. **Task 2: Criar o script de gatilho + captura de evidência do backfill** - `cfa60f0` (feat)

**Plan metadata:** (pendente — commit final abaixo)

## Files Created/Modified
- `api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs` - 3 `[Fact]`: exclusão Inactive/Churned em `GetAllLeadsAsync`, inclusão dos 4 status abertos, e `GetSegmentCountsAsync` ignorando Inactive/Churned mesmo com Segment atribuído
- `scripts/backfill_lead_scores.py` - script Python one-off: `capture_snapshot`, `trigger_recompute`, `compare`, `write_json`, `main` com `--snapshot-only`/`--run`/`--idempotency`/`--out`

## Decisions Made
- **D-09-01** (registrada no frontmatter do plano, ver `key-decisions` acima): efeito colateral de follow-up tasks do `RecomputeAllAsync` é aceito sem modificação — o executor do plano 09-02 não deve reabrir esta discussão, apenas ecoar `followUpTasksCreated` na evidência.

## Deviations from Plan

None - plan executado exatamente como escrito. Todos os acceptance criteria de ambas as tasks foram verificados via grep/dotnet test/python antes de cada commit.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required. As credenciais (`DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD`/`DIAX_API_BASE_URL`) já existem em `~/.claude/.secrets.env` e são lidas via `load_secrets()` já existente — nada novo a configurar.

## Next Phase Readiness

- `scripts/backfill_lead_scores.py` está pronto para o plano 09-02 executar `--run` (e opcionalmente `--idempotency`) contra produção — este plano NÃO executou nenhuma escrita em produção (nenhum arquivo `docs/lead-quality/backfill_*.json` foi gerado, por design).
- O teste de regressão `CustomerRepositoryLeadFilterTests` trava a garantia que o 09-02 depende para afirmar "os 1.065 leads Inactive nunca são repontuados" — não precisa ser reprovado, só mantido verde.
- Decisão D-09-01 já fechada — o 09-02 deve apenas registrar o `followUpTasksCreated` observado em produção na evidência, sem revisitar se o comportamento é desejado.

---
*Phase: 09-backfill-de-score-do-backlog*
*Completed: 2026-09-19*

## Self-Check: PASSED

- FOUND: api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs
- FOUND: scripts/backfill_lead_scores.py
- FOUND: commit 32daa35 (Task 1)
- FOUND: commit cfa60f0 (Task 2)
