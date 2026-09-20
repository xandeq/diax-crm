---
gsd_state_version: 1.0
milestone: v1.4
milestone_name: Funil de Outreach — Cadência e Qualidade
status: in_progress
stopped_at: Beginning Phase 12
last_updated: "2026-09-20T16:02:53.259Z"
progress:
  total_phases: 11
  completed_phases: 3
  total_plans: 18
  completed_plans: 10
---

# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-09-19)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas
**Current focus:** Phase 12 — Escala de Email

## Current Position

Phase: 12 (Escala de Email) — EXECUTING
Plan: 1 of 6

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
| Phase 07 P05 | 15min | 2 tasks | 5 files |
| Phase 07 P06 | 20min | 2 tasks | 6 files |
| Phase 08 P01 | 18min | 2 tasks | 5 files |
| Phase 08 P02 | 12min | 2 tasks | 2 files |
| Phase 08 P03 | 25min | 3 tasks | 3 files |
| Phase 08 P04 | 15min | 2 tasks | 2 files |
| Phase 09 P01 | 12min | 2 tasks | 2 files |
| Phase 09 P02 | 22min | 3 tasks | 5 files |
| Phase 10 P01 | 20min | 2 tasks | 3 files |
| Phase 10 P05 | 45min | 3 tasks | 4 files |
| Phase 10 P02 | 35min | 3 tasks | 3 files |
| Phase 10 P03 | 20min | 3 tasks | 8 files |
| Phase 10 P04 | 25min | 2 tasks | 52 files |
| Phase 11 P01 | 45min | 2 tasks | 3 files |

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
  — é exatamente o gap do v1.3 (EXTR-01, já resolvido).

- v1.4: `LeadScoringService.CalculateScore`/`SegmentForScore` (C#, teto de fit 45 desde Phase 8) é o
  motor oficial de score — Phase 9 (backfill) roda ele sobre o backlog pré-v1.3, não reescreve a
  lógica.

- v1.4: `docs/email-marketing/` concentra os 79+ scripts `fire_*.py` (alvo de consolidação da Phase
  10) e `reply_watch.py` (alvo de automação da Phase 11) — ambos scripts Python soltos, fora do
  backend .NET.

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
- [Phase 07]: 07-05: ImportRejectionCounts nao inclui duplicado - so CustomerImportService sabe quantas linhas casaram com Customer existente
- [Phase 07]: 07-05: WebsiteKind no enrich recalculado a partir do website FINAL (preservado ou novo), nao do row.Website bruto - cobre leads legados sem WebsiteKind calculado
- [Phase 07]: 07-06: from/to tratados como UTC e inclusivos nas duas bordas — combina com a conversao automatica de DateTime para UTC do DiaxDbContext
- [Phase 07]: 07-06: 4 contadores de rejeicao adicionados no FINAL de ImportHistoryResponse (default 0) para preservar a ordem posicional das 9 propriedades originais
- [Phase 07]: 07-06: teste de filtro de periodo usa DiaxDbContext real + InMemory, setando CreatedAt via Entry(...).Property(...).CurrentValue (mecanismo padrao do EF change tracker para setter protegido)
- [Phase 08]: [Phase 08] 08-01: ExternalId adicionado como ULTIMO parametro posicional de ImportCustomerRow para nao quebrar ~30 chamadores posicionais existentes
- [Phase 08]: [Phase 08] 08-01: GetByExternalIdAsync normaliza com Trim(), espelhando Customer.SetExternalId; zero migration criada (coluna/indice ja em producao desde Phase 7)
- [Phase 08]: D-05: bloco de fit do LeadScoringService incorpora sinais da Phase 7 (WebsiteKind.OwnSite +10, Quality.High +5, EmailType.PersonalDirect +5, HasSuspiciousDomain -15), teto sobe de 25 para 45 — lead de sinais fortes nasce Warm sem engajamento, Hot permanece inalcancavel so com fit
- [Phase 08]: SegmentForScore(int) extraido como fonte unica dos limiares Hot/Warm/Cold, consumido por RecomputeAllAsync e pronto para o import do plano 08-04
- [Phase 08]: [Phase 08] 08-03: resolucao ExternalId->email->telefone implementada no loop de persistencia; D-02 fecha gap orfao checando o email ANTIGO na supressao (nao so o novo); D-03 email vence conflito com log ExternalIdConflict; D-04 backfill organico sem migration/script em massa
- [Phase 08]: 08-04: score/segmento calculados no ramo CREATE do import chamando LeadScoringService.CalculateScore/SegmentForScore estaticamente, sem injetar o servico no construtor de CustomerImportService
- [Phase 08]: 08-04: ramo ENRICH permanece intencionalmente sem recalculo de score, protegido por comentario de guarda no codigo, para nao apagar engajamento acumulado de leads existentes
- v1.4 (roadmap): numeração de fases continua de v1.3 (Phase 8) — v1.4 começa em Phase 9, não reseta
- v1.4 (roadmap): Phase 9 (backfill) é independente e vem primeiro — baixo risco, não depende de nada, usa motor já existente
- v1.4 (roadmap): SCRP-01 e OBSV-01 agrupados na Phase 10 — consolidar os scripts fire_*.py num motor único e tirar o pipeline da dependência do notebook são as duas metades da mesma fundação de confiabilidade; EMAL-02 (warmup/volume) depende dela
- v1.4 (roadmap): QUAL-01/02 (Phase 11) depende da Phase 10 — automatizar reply_watch.py com segurança pressupõe pipeline confiável fora do notebook; DASH-01 (Phase 14) depende da Phase 11 — dashboard só é honesto se o sinal Qualified for automático
- v1.4 (roadmap): EMAL (Phase 12) e WHAT (Phase 13) são trilhas paralelas independentes entre si — podem ser planejadas/executadas em qualquer ordem relativa uma à outra
- [Phase 09]: D-09-01: efeito colateral de ate 10 follow-up tasks do RecomputeAllAsync eh aceito sem bypass - comportamento de producao ja vigente do LeadScoringWorker; nao reabrir no 09-02
- [Phase 09]: BFIL-01 satisfied: production backlog LeadScore/Segment recalculated by LeadScoringService.RecomputeAllAsync, proven idempotent across two runs, human-verified 2026-09-19
- [Phase 09]: 3 of 13 follow-up tasks from the backfill (RecomputeAllAsync side effect) were upstream scraping contamination (international product review, government office, generic email) — marked Done via CRM cleanup, not a code fix; same contamination pattern as 2026-09-17/19 audit, flagged for Phase 10/12 upstream filtering
- [Phase 10]: [Phase 10] 10-01: waves_smoke.py excludes fire_-prefixed import targets by prefix (not by name), so it never dynamically imports debug_campaign.py's side-effecting dependency and needs no update when the 45 dead-end fire_*.py scripts are archived in 10-04
- [Phase 10]: [Phase 10] 10-05: waha-watchdog/watchdog-n8n.ps1 estendidos additions-only com alerta Telegram por transicao de estado (alerta_telegram.py, stdlib-only); task waha-watchdog registrada (5min, lacuna encontrada ao vivo); n8n local caido/task n8n-local Disabled fica pendencia explicita fora de escopo (D-04/D-05)
- [Phase 10]: [Phase 10] 10-02: waves_core.py e waves_copy.py criados por copia verbatim (nao movimentacao) de fire_today_20/fire_w1_2707/fire_w2w3_2707; daily_waves.py religado nos modulos novos com prova de equivalencia total (md5 dos 3 fontes inalterado, 7/7 inspect.getsource identico, hash do template igual ao baseline, smoke 5/5 apos cada task, dry-run --force com rede real sem erro)
- [Phase 10]: 10-03: 7 dependentes vivos de fire_today_20.py (import_extrator_bridge.py, replenish_leads.py, rescrape_21/27/tavily_27.py, scrape_and_import.py, debug_campaign.py) religados em waves_core.py; grep from fire_|import fire_ zerado, D-03 do 10-04 satisfeito; debug_campaign.py alterado sem jamais ser importado/executado (py_compile + AST estatico)
- [Phase 10]: 10-04: 6 de 48 fire_*.py sao tracked no git (nao 3 como estimado) - mv de sistema de arquivos + git add escopado detecta rename automatico para tracked, evitando git mv forcado
- [Phase 11]: [Phase 11] 11-01: cited atendimento@listamais.com.br (not pedido@bfffitnessatacado.com.br) as QUAL-02 proof - the latter's Status 1->2 predates any reply_watch.py log entry, not attributable to this mechanism

### Testing Protocol

- /wave-qa obrigatório após cada phase
- Smoke tests + Playwright e2e + regression tests por wave
- Migrations via update-db.ps1 antes de qualquer teste
- git push apenas com autorização explícita do usuário (auto-deploy via GitHub Actions)
- api-core: build/test SEMPRE com `-c Release` (Smart App Control bloqueia DLL de teste Debug)

### v1.2 — Pausado (contexto preservado, não é o milestone atual)

⚠️ **Este bloco descreve o estado do v1.2 (Agentes de IA) no momento em que v1.3 começou.
Não resolvido, não abandonado — só não é o foco agora. v1.4 também não mexe nesses arquivos.**

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
  no push). Confirmar isso continua verdadeiro antes de qualquer push de v1.4 pra `main`.

- Retomar com: `/gsd:execute-phase 2` (pula 02-01/02-02 já com SUMMARY, segue da Wave 2:
  02-03 orquestrador, 02-04 controller).

### Pending Todos

**v1.4 (atual):**

- Phase 9 (Backfill de Score do Backlog) concluída 2026-09-19 — BFIL-01 completo, evidência em
  `docs/lead-quality/BACKFILL-EVIDENCE.md`.

- Phase 10 CONCLUÍDA (5/5 planos, 2026-09-19) — 10-05 executado fora de ordem por decisão do
  usuário, fecha OBSV-01 completo; 10-02 extraiu fire_today_20/fire_w1_2707/fire_w2w3_2707 para
  waves_core.py/waves_copy.py e religou daily_waves.py neles; 10-03 religou os 7 dependentes vivos
  restantes — import_extrator_bridge.py, replenish_leads.py, rescrape_21/27/tavily_27.py,
  scrape_and_import.py, debug_campaign.py — em waves_core.py, com grep de dependência viva zerado;
  10-04 arquivou os 48 fire_*.py em docs/email-marketing/archive/ (com README de aviso), fechou
  MIGRACAO-fire-scripts.md e marcou SCRP-01 completo — último requirement pendente da fase.
  Motor reverificado pós-arquivamento: waves_smoke.py 5/5, daily_waves.py --dry --force até FIM
  sem traceback.

- Phase 11 (Qualified Objetivo e Automático) CONCLUÍDA (1/1 plano, 2026-09-20) — 11-01 verificou ao
  vivo (sem alterar código) que `reply_watch.py` já implementa QUAL-01/QUAL-02, publicou
  `docs/email-marketing/QUALIFIED-avanco-automatico.md` com critério + prova reconferida (audit log,
  customer_id `2dcb6a14-006a-426c-bc60-b6599a8f0e64`, Status 1→2, caminho negativo sem falso
  positivo), guard MD5 de `reply_watch.py` idêntico do início ao fim (diff não commitado do usuário
  preservado). QUAL-01/QUAL-02 fechados em REQUIREMENTS.md/ROADMAP.md. Próximo passo:
  `/gsd:plan-phase 12` (Escala de Email).

**v1.3 (shipped, referência):**

- Nenhum pendente — Phases 7 e 8 concluídas, deployadas e verificadas em produção (PRs #99, #111)

**v1.2 (pausado, preservado):**

- Phase 2 Wave 2 — executar 02-03 (IAgentTool/IAgentHandler/IAgentOrchestratorService + AgentOrchestratorService + CommercialAgentHandler + DI)
- Phase 2 Wave 3 — executar 02-04 (AgentsController {type}/chat|confirm|conversations + RBAC + AiUsageTracking)
- Retomar com: `/gsd:execute-phase 2` (pula 02-01/02-02 já com SUMMARY, segue da Wave 2)

### Blockers/Concerns

**v1.4:**

- `docs/email-marketing/reply_watch.py` está com mudanças não commitadas (trabalho em andamento
  não relacionado a este milestone) no momento em que o roadmap da Phase 11 foi criado. Quem
  planejar/executar a Phase 11 (Qualified Objetivo e Automático) não deve assumir git status limpo
  nesse arquivo — precisa incorporar a lógica de QUAL-02 preservando esse trabalho em andamento, não
  descartá-lo com um checkout/reset.

- EMAL-02 (Phase 12) depende de a Phase 10 (motor único) estar pronta antes — escalar volume de
  envio sobre os scripts `fire_*.py` antigos, sem motor com warmup real, é o cenário que este
  milestone existe pra evitar.

Ver "v1.2 — Pausado" acima para o blocker preservado daquele milestone (sessão concorrente,
migration não integrada, commits locais não pushados).

## Session Continuity

Last session: 2026-09-20T13:41:29.646Z
Stopped at: Completed 11-01-PLAN.md
Phase 11 plan 01 (único plano da fase) executado — QUAL-01/QUAL-02 fechados via verificação ao vivo,
zero código de aplicação alterado (D-01). Task 1 reconferiu ao vivo (não copiou da pesquisa de
2026-09-20): Scheduled Task `DIAX-Reply-Watch` `State=Ready`/`LastTaskResult=0`/cadência PT30M-PT11H
seg-sex sem divergência, e o cross-reference contra o audit log do CRM confirmou
`atendimento@listamais.com.br` (customer_id `2dcb6a14-006a-426c-bc60-b6599a8f0e64`) avançou
`Status: 1→2` em `2026-09-07T11:00:22.9668226Z`, 14s depois da classificação `positive` no log do
script — mesma correlação da pesquisa, agora reconferida nesta sessão. Caminho negativo
(`coordenacao@atomclinica.com.br`, `recepcao@vilaaltodospinhais.com.br`) confirmado sem nenhum
evento de `Status` no audit log — sem falso positivo. Guard MD5 de `reply_watch.py` idêntico nos 3
checkpoints (antes da Task 1, depois da Task 1, depois da Task 2) — `BC93E487DDFA9DA7EC5BC4284F8F2328`
— e `git status`/`git diff` confirmam o diff não commitado do usuário (saudação WhatsApp) intacto o
tempo todo. Task 2 criou `docs/email-marketing/QUALIFIED-avanco-automatico.md` (195 linhas, 5 seções)
e fechou QUAL-01/QUAL-02 em REQUIREMENTS.md e no Coverage Map do ROADMAP.md. **Phase 11 está 100%
concluída (1/1 plano).** v1.2 segue pausado em paralelo (ver "v1.2 — Pausado" acima), sem alteração.
Próximo passo: `/gsd:plan-phase 12` (Escala de Email).
Resume file: None
