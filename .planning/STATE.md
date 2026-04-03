# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-03)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas
**Current focus:** Phase 1 — Tarefas (ready to plan)

## Current Position

Phase: 1 of 5 (Tarefas)
Plan: 0 of ? in current phase
Status: Ready to plan
Last activity: 2026-04-03 — Roadmap v1.1 criado (5 phases, 21 requirements mapped)

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

*Updated after each plan completion*

## Accumulated Context

### Infrastructure (carry forward from init)
- Sistema em produção em crm.alexandrequeiroz.com.br
- Backend: api.alexandrequeiroz.com.br (SmarterASP.NET)
- DB: SQL Server `db_aaf0a8_diaxcrm` em `sql1002.site4now.net`
- Deploy automático via GitHub Actions (push to main)
- Usar sempre `update-db.ps1` para migrations (nunca dotnet ef direto)
- Frontend é static export — sem SSR

### Decisions

- Init: Customer = Lead (modelo unificado) — Pipeline Kanban usa Customer.Stage, sem nova entidade
- Init: Tarefas são entidade nova independente (Task) — sem vínculo a Customer em v1.1
- Init: PDF de proposta deve ser gerado no backend .NET ou client-side (não server action)

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-04-03
Stopped at: Roadmap criado, STATE.md e REQUIREMENTS.md atualizados. Pronto para `/gsd:plan-phase 1`
Resume file: None
