# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-28)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas
**Current focus:** Milestone v1.2 Agentes de IA — definindo roadmap

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements / roadmap
Last activity: 2026-05-28 — Milestone v1.2 Agentes de IA iniciado (supera v1.1)

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
- v1.2: Agentes reaproveitam motor de chat único (IAnthropicChatClient/AiChat); prompt+tools+escopo por tipo
- v1.2: Toda tool de escrita dos agentes exige confirmação do usuário (sem ação automática)
- v1.2: Agente Comercial já parcialmente construído fora do GSD — POST /api/v1/agents/commercial/chat, CommercialAgentService, AgentType enum, 4 testes passando, stateless, sem migration. Build da solução OK.
- v1.2: Frontend reaproveita componente de chat de ai-chat; UI padrão /impeccable

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-05-28
Stopped at: Milestone v1.2 iniciado — PROJECT/MILESTONES/REQUIREMENTS atualizados. Criando roadmap.
Resume file: None
