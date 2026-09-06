---
gsd_state_version: 1.0
milestone: v1.3
milestone_name: — Pipeline de Aquisição
status: unknown
stopped_at: Completed 07-04-PLAN.md
last_updated: "2026-09-06T09:54:49.717Z"
progress:
  total_phases: 2
  completed_phases: 0
  total_plans: 7
  completed_plans: 4
---

# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-09-05)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas
**Current focus:** Phase 07 — extra-o-qualidade-na-entrada

## Current Position

Phase: 07 (extra-o-qualidade-na-entrada) — EXECUTING
Plan: 5 of 7

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
| Phase 07 P01 | 12min | 2 tasks | 5 files |
| Phase 07 P02 | 15min | 2 tasks | 6 files |
| Phase 07 P03 | 25min | 3 tasks | 12 files |
| Phase 07 P04 | 55min | 2 tasks | 7 files |

## Accumulated Context

### Infrastructure (carry forward)

- Sistema em produção em crm.alexandrequeiroz.com.br
- Backend: api.alexandrequeiroz.com.br (SmarterASP.NET)
- DB: SQL Server `db_aaf0a8_diaxcrm` em `sql1002.site4now.net`
- Deploy automático via GitHub Actions (push to main)
- SEMPRE usar `update-db.ps1` para migrations (nunca dotnet ef direto em produção)
- Frontend é static export — sem SSR, sem server actions Next.js
- Branch `chore/email-automation-versioned` acumula o trabalho de email/WhatsApp/extração de
  03-05/09 (PRs #95-#97 merged em `main` via squash — merges seguintes nessa branch vão mostrar
  `mergeStateStatus=CONFLICTING` fantasma; resolver com `git merge origin/main` + `checkout --ours`
  depois de confirmar com `git diff` que não há divergência real de conteúdo)

### Architecture

- Clean Architecture: src/Diax.Domain | Application | Infrastructure | Api | Shared
- Frontend: crm-web (Next.js 14 static export, shadcn/ui, Tailwind v4, React Query, Framer Motion)
- Existing AI infra: IAnthropicChatClient, AiChatService, AiConversation, GroupAiAccess, AiUsageTracking
- Existing services to reuse by agent: Commercial→ICustomerRepository/OutreachService/AiOutreachAbTest; Support→customer timeline/ITicketService; Personal→IAppointmentService/FinancialSummaryService/AppointmentService
- Existing ai-chat page/component to reuse in /agentes UI
- Pipeline extração→CRM: `ExtractorPullWorker` (.NET, diário 12:00 BRT) puxa de
  `ExtractorIntegrationService` → filtra qualidade (`IsLowQualityEmail`) + geo
  (`IsOutsideTargetGeo`, `ExtractorPullOptions.AllowedStates/AllowedDdds`) → `CustomerImportService`.
  Ponte manual Python (`docs/email-marketing/import_extrator_bridge.py`) é fallback/paralelo, tem
  filtros próprios (`mx_check.py`, `waves_lib.in_target_geo`) que ainda não foram trazidos pro C#
  — é exatamente o gap do v1.3 (EXTR-01).

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
- v1.3: escopo confirmado com o usuário = Extração (A) + Import CRM (B) do backlog original;
  Email (C) e WhatsApp (D) foram pra v2 (adiados, não descartados) — ver REQUIREMENTS.md

- v1.3: dedup por email é a raiz do problema de rastreabilidade (mesmo negócio muda de email
  entre passadas do scraper) — `Customer.ExternalId` resolve na causa, não é cosmético

- [Phase 07]: WebsiteClassifier/JunkDomainFilter são cópias verbatim de site_check.py/mx_check.py (D-08) — sem reinventar as listas
- [Phase 07]: 07-02: MxResponseInterpreter isolates DNS decision logic (Valid/NoMx/Unverified) as pure function from DnsClient.NET types for offline deterministic testing
- [Phase 07]: 07-02: D-02 confirmed in code — SERVFAIL(2)/NotImplemented(4)/Refused(5) resolve to Unverified same as ConnectionTimeout, only NXDOMAIN(3) and empty MX+A resolve to NoMx
- [Phase 07]: [Phase 07] 07-03: MxCacheEntry.ResultCode fica int no Domain (nao MxCheckResult) - Domain nao referencia Application, conversao fica pro consumidor em 07-04
- [Phase 07]: [Phase 07] 07-03: zero migration gerada (D-07) - migration unica coordenando WebsiteKind+ExternalId+contadores+mx_cache_entries sai no plano 07-07
- [Phase 07]: 07-04: D-02 confirmada em codigo — MxCheckResult.Unverified nunca aciona o continue de rejeicao no loop de import; so NoMx rejeita
- [Phase 07]: 07-04: MX check roda em LOTE apos a paginacao completa (nao por lead), reaproveitando cache persistente + paralelismo configuravel (default 8)
- [Phase 07]: 07-04: JunkDomainFilter.IsJunk adicionado como short-circuit em IsLowQualityEmail — expos colisao de fixtures de teste pre-existentes (empresa.com.br/email.com/test.com), corrigidas para dominios nao-lixo

### Testing Protocol

- /wave-qa obrigatório após cada phase
- Smoke tests + Playwright e2e + regression tests por wave
- Migrations via update-db.ps1 antes de qualquer teste
- git push apenas com autorização explícita do usuário (auto-deploy via GitHub Actions)
- api-core: build/test SEMPRE com `-c Release` (Smart App Control bloqueia DLL de teste Debug)

### v1.2 — Pausado (contexto preservado, não é o milestone atual)

⚠️ **Este bloco descreve o estado do v1.2 (Agentes de IA) no momento em que v1.3 começou.
Não resolvido, não abandonado — só não é o foco agora.**

- Pausa: 2026-05-29, após Phase 2 Wave 1 (2 de 4 plans). Motivo: outra sessão do Claude Code CLI
  mexendo no mesmo repo — pausado para evitar cross-data/conflito.

- **Risco de migration**: `20260529134701_AddAgentFoundation` JÁ FOI APLICADA ao SQL Server de
  PRODUÇÃO e altera o `ApplicationDbContextModelSnapshot`. Se outra sessão criar outra migration
  antes desta ser integrada, há risco de conflito de snapshot/ordem.

- **Arquivos tocados pela Phase 2** (evitar editar em outro trabalho até v1.2 ser retomado ou
  formalmente encerrado): `src/Diax.Domain/Agents/*`, `src/Diax.Application/Agents/*`,
  `src/Diax.Application/AiChat/IAnthropicChatClient.cs`,
  `src/Diax.Infrastructure/.../AnthropicChatClient.cs`, `src/Diax.Domain/AiChat/AiConversation.cs`,
  `IAiChatRepository`/repo, `src/Diax.Infrastructure/Data/Migrations/20260529134701_AddAgentFoundation*`,
  `DependencyInjection.cs`, `tests/Diax.Tests/Application/Agents/*`.

- **Nada foi pushado** — todos os commits da Phase 2 são locais em `main` (auto-deploy só dispara
  no push). Confirmar isso continua verdadeiro antes de qualquer push de v1.3 pra `main`.

- Retomar com: `/gsd:execute-phase 2` (pula 02-01/02-02 já com SUMMARY, segue da Wave 2:
  02-03 orquestrador, 02-04 controller).

### Pending Todos

**v1.3 (atual):**

- Roadmap criado (Phase 7: Extração — Qualidade na Entrada; Phase 8: Import — Dedup e Score em Tempo Real) — próximo passo é `/gsd:plan-phase 7`

**v1.2 (pausado, preservado):**

- Phase 2 Wave 2 — executar 02-03 (IAgentTool/IAgentHandler/IAgentOrchestratorService + AgentOrchestratorService + CommercialAgentHandler + DI)
- Phase 2 Wave 3 — executar 02-04 (AgentsController {type}/chat|confirm|conversations + RBAC + AiUsageTracking)
- Retomar com: `/gsd:execute-phase 2` (pula 02-01/02-02 já com SUMMARY, segue da Wave 2)

### Blockers/Concerns

Nenhum blocker ativo para v1.3 no momento do início.

Ver "v1.2 — Pausado" acima para o blocker preservado daquele milestone (sessão concorrente,
migration não integrada, commits locais não pushados).

## Session Continuity

Last session: 2026-09-06T09:54:49.713Z
Stopped at: Completed 07-04-PLAN.md
Phase 8 (Import — Dedup e Score em Tempo Real, IMPT-01..03), 6/6 requirements mapeados. v1.2
segue pausado em paralelo (ver "v1.2 — Pausado" acima), sem alteração. Próximo passo:
`/gsd:plan-phase 7`.
Resume file: None
