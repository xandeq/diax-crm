---
gsd_state_version: 1.0
milestone: v1.2
milestone_name: Agentes de IA
status: paused
stopped_at: "PAUSED após Wave 1 (02-01 + 02-02). Waves 2-3 (02-03 orquestrador, 02-04 controller) NÃO iniciadas. Pausa solicitada — outra sessão do Claude Code CLI mexe no mesmo repo (evitar cross-data)."
last_updated: "2026-05-29T13:52:10.548Z"
progress:
  total_phases: 5
  completed_phases: 0
  total_plans: 4
  completed_plans: 2
---

# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-28)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas
**Current focus:** Phase 02 — funda-o-de-agentes

## Current Position

Phase: 02 (funda-o-de-agentes) — EXECUTING
Plan: 3 of 4

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
| Phase 02-funda-o-de-agentes P02-02 | 8m | 2 tasks | 3 files |
| Phase 02-funda-o-de-agentes P01 | 35 | 2 tasks | 13 files |

## Accumulated Context

### Infrastructure (carry forward)

- Sistema em produção em crm.alexandrequeiroz.com.br
- Backend: api.alexandrequeiroz.com.br (SmarterASP.NET)
- DB: SQL Server `db_aaf0a8_diaxcrm` em `sql1002.site4now.net`
- Deploy automático via GitHub Actions (push to main)
- SEMPRE usar `update-db.ps1` para migrations (nunca dotnet ef direto em produção)
- Frontend é static export — sem SSR, sem server actions Next.js

### Architecture

- Clean Architecture: src/Diax.Domain | Application | Infrastructure | Api | Shared
- Frontend: crm-web (Next.js 14 static export, shadcn/ui, Tailwind v4, React Query, Framer Motion)
- Existing AI infra: IAnthropicChatClient, AiChatService, AiConversation, GroupAiAccess, AiUsageTracking
- Existing services to reuse by agent: Commercial→ICustomerRepository/OutreachService/AiOutreachAbTest; Support→customer timeline/ITicketService; Personal→IAppointmentService/FinancialSummaryService/AppointmentService
- Existing ai-chat page/component to reuse in /agentes UI

### Decisions

- Init: Customer = Lead (modelo unificado) — Pipeline Kanban usa Customer.Stage, sem nova entidade
- Init: Tarefas são entidade nova independente (Task) — sem vínculo a Customer em v1.1
- Init: PDF de proposta deve ser gerado no backend .NET ou client-side (não server action)
- v1.2: Agentes reaproveitam motor de chat único (IAnthropicChatClient/AiChat); prompt+tools+escopo por tipo
- v1.2: Toda tool de escrita dos agentes exige confirmação do usuário (sem ação automática)
- v1.2: Agente Comercial já parcialmente construído fora do GSD — POST /api/v1/agents/commercial/chat, CommercialAgentService, AgentType enum, CommercialAgentPrompts, 4 testes passando, stateless, sem migration
- v1.2: UI /agentes toda em Phase 6 (uma fase única) para consistência visual e reuso máximo de componentes
- v1.2: Fases 3-5 (backends dos agentes) validáveis via API tests e /wave-qa sem necessidade de UI
- [Phase 02-funda-o-de-agentes]: CompleteWithToolsAsync is opt-in third method on IAnthropicChatClient — tools array gated by if-block, no-tools body provably identical to existing paths
- [Phase 02-funda-o-de-agentes]: AgentPendingAction stored in DB table (not in-memory/signed token) - survives restarts, auditable, consistent with EF Core patterns
- [Phase 02-funda-o-de-agentes]: Payload column is nvarchar(max) via HasColumnType fluent config to support arbitrary JSON tool inputs

### Testing Protocol

- /wave-qa obrigatório após cada phase
- Smoke tests + Playwright e2e + regression tests por wave
- Migrations via update-db.ps1 antes de qualquer teste
- git push apenas com autorização explícita do usuário (auto-deploy via GitHub Actions)

### Pending Todos

- Phase 2 Wave 2 — executar 02-03 (IAgentTool/IAgentHandler/IAgentOrchestratorService + AgentOrchestratorService + CommercialAgentHandler + DI)
- Phase 2 Wave 3 — executar 02-04 (AgentsController {type}/chat|confirm|conversations + RBAC + AiUsageTracking)
- Retomar com: `/gsd:execute-phase 2` (pula 02-01/02-02 já com SUMMARY, segue da Wave 2)

### Blockers/Concerns

⚠️ **SESSÃO PARALELA ATIVA (2026-05-29):** Outra sessão do Claude Code CLI implementa funcionalidades no MESMO repo. Phase 2 foi PAUSADA após Wave 1 para evitar cross-data/conflito.
- **Risco de migration:** a migration `20260529134701_AddAgentFoundation` JÁ FOI APLICADA ao SQL Server de PRODUÇÃO e altera o `ApplicationDbContextModelSnapshot`. Se a outra sessão criar outra migration, haverá conflito de snapshot/ordem. Coordenar: a outra sessão deve dar `add-migration` SOMENTE após integrar estes commits.
- **Arquivos já tocados pela Phase 2 (evitar editar na outra sessão):** `src/Diax.Domain/Agents/*`, `src/Diax.Application/Agents/*`, `src/Diax.Application/AiChat/IAnthropicChatClient.cs`, `src/Diax.Infrastructure/.../AnthropicChatClient.cs`, `src/Diax.Domain/AiChat/AiConversation.cs`, `IAiChatRepository`/repo, `src/Diax.Infrastructure/Data/Migrations/20260529134701_AddAgentFoundation*`, `DependencyInjection.cs`, `tests/Diax.Tests/Application/Agents/*`.
- **Nada foi pushado** — todos os commits são locais em `main` (auto-deploy só dispara no push).

## Session Continuity

Last session: 2026-05-29
Stopped at: PAUSED após Wave 1 (Phase 2). Waves 2-3 pendentes. Pausa por sessão paralela no mesmo repo.
Resume file: None
