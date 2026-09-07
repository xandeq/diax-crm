# DIAX CRM

## What This Is

Sistema de controle pessoal e profissional de Alexandre Queiroz â€” CRM privado (single-user) com mÃ³dulos de finanÃ§as, leads, e-mail marketing, WhatsApp, ferramentas IA e gestÃ£o de negÃ³cios. NÃ£o Ã© um SaaS. Stack: Next.js 14 (static export) + .NET 8 Clean Architecture + SQL Server (SmarterASP.NET). Deploy automÃ¡tico via GitHub Actions.

## Core Value

Centralizar todas as operaÃ§Ãµes de negÃ³cio (leads, finanÃ§as, comunicaÃ§Ã£o, IA) em um Ãºnico sistema pessoal, eliminando ferramentas externas pagas.

## Current Milestone: v1.3 Pipeline de AquisiÃ§Ã£o

**Goal:** Fechar os gaps que sobraram do pipeline extraÃ§Ã£oâ†’CRMâ†’emailâ†’WhatsApp (jÃ¡ em produÃ§Ã£o,
construÃ­do em 03-05/09): qualidade de dado na entrada e rastreabilidade end-to-end no import.

**Target features:**
- MX/domÃ­nio vÃ¡lido verificado no worker .NET antes de importar (hoje sÃ³ na ponte Python manual)
- Log do motivo de rejeiÃ§Ã£o (geo/email-lixo/MX/duplicado) por lead, consultÃ¡vel por perÃ­odo
- Sinal "site prÃ³prio vs diretÃ³rio de terceiro" usado no import/score
- `Customer.ExternalId` (dedup robusto por ID do Extrator, nÃ£o sÃ³ email)
- Dedup real em `/customers/import` para `source=Scraping`
- `lead_score` calculado no momento do import (hoje sÃ³ no job diÃ¡rio 06h BRT)

**Nota â€” v1.2 (Agentes de IA) segue PAUSADO em paralelo**, nÃ£o abandonado: Phase 02 parou apÃ³s
Wave 1 (2026-05-29) por sessÃ£o concorrente no mesmo repo; commits locais em `main`, **nÃ£o
pushados**; migration `20260529134701_AddAgentFoundation` jÃ¡ aplicada em produÃ§Ã£o â€” cuidado com
ordem de migrations se retomado. Detalhe completo preservado em `STATE.md`. Retomar com
`/gsd:execute-phase 2` quando decidido â€” v1.3 nÃ£o toca nos arquivos de `src/Diax.Domain/Agents/*`
nem correlatos.

**Target features (v1.2, referÃªncia â€” nÃ£o descontinuadas):**
- Agente Comercial â€” qualifica leads, prioriza pipeline, gera outreach, atualiza status/segmento (parcialmente construÃ­do)
- Agente de Suporte â€” atende com base no histÃ³rico do cliente, sugere respostas, abre/tria tickets
- Agente Pessoal â€” resume agenda e finanÃ§as, cria compromissos
- OrquestraÃ§Ã£o compartilhada (AgentOrchestrator + framework de tools/function-calling com confirmaÃ§Ã£o)
- UI /agentes bonita no crm-web (padrÃ£o /impeccable, reaproveitando o chat de ai-chat)

## Requirements

### Validated

<!-- Funcionalidades jÃ¡ em produÃ§Ã£o â€” v1.0 -->

- âœ“ AutenticaÃ§Ã£o JWT com sessÃ£o por browser â€” v1.0
- âœ“ GestÃ£o de leads e clientes (modelo unificado Customer) â€” v1.0
- âœ“ Timeline de relacionamento com cliente â€” v1.0
- âœ“ ImportaÃ§Ã£o de leads (CSV, Extrator de Dados) â€” v1.0
- âœ“ Agenda de compromissos com importaÃ§Ã£o por texto â€” v1.0
- âœ“ MÃ³dulo financeiro completo (contas, cartÃµes, transaÃ§Ãµes, planejador, metas, recorrentes) â€” v1.0
- âœ“ Campanhas de e-mail (outreach, bulk send, campaign composer + worker Brevo) â€” v1.0
- âœ“ WhatsApp via Evolution API â€” v1.0
- âœ“ Ferramentas IA (humanizaÃ§Ã£o, geraÃ§Ã£o de imagens, prompt generator, extraÃ§Ã£o HTML) â€” v1.0
- âœ“ MÃ³dulo Facebook Ads (Graph API v21.0) â€” v1.0
- âœ“ Blog management â€” v1.0
- âœ“ Admin panel (usuÃ¡rios, grupos, RBAC, provedores IA) â€” v1.0
- âœ“ Audit logs e system logs â€” v1.0
- âœ“ Snippets e checklists domÃ©sticos â€” v1.0
- âœ“ Dashboard geral e analytics â€” v1.0
- âœ“ EXTR-01: MX/domÃ­nio vÃ¡lido verificado antes de importar, com degradaÃ§Ã£o segura (falha de DNS nÃ£o rejeita lead) â€” v1.3 Phase 7
- âœ“ EXTR-02: motivo de rejeiÃ§Ã£o contado por rodada e consultÃ¡vel por perÃ­odo (`GET /customers/imports?from&to`) â€” v1.3 Phase 7
- âœ“ EXTR-03: site prÃ³prio vs diretÃ³rio de terceiro classificado e persistido (`Customer.WebsiteKind`) â€” v1.3 Phase 7

### Active

<!-- Milestone v1.3 â€” Pipeline de AquisiÃ§Ã£o Â· Phase 7 completa, Phase 8 pendente -->

- [ ] IMPT-01: `Customer.ExternalId` â€” dedup por ID do Extrator, nÃ£o sÃ³ email (coluna jÃ¡ criada na Phase 7; falta a lÃ³gica de dedup)
- [ ] IMPT-02: dedup real em `/customers/import` para `source=Scraping`
- [ ] IMPT-03: `lead_score` calculado no momento do import

<!-- Milestone v1.2 â€” Agentes de IA (PAUSADO em paralelo, ver nota em Current Milestone acima) -->

- [ ] Agente Comercial: chat sobre o pipeline real de leads, prioriza, gera outreach e atualiza status/segmento sob confirmaÃ§Ã£o
- [ ] Agente de Suporte: atende com histÃ³rico do cliente, sugere respostas e abre/tria tickets
- [ ] Agente Pessoal: resume agenda e finanÃ§as, cria compromissos sob confirmaÃ§Ã£o
- [ ] OrquestraÃ§Ã£o de agentes (AgentOrchestrator) + framework de tools/function-calling com confirmaÃ§Ã£o do usuÃ¡rio
- [ ] PersistÃªncia/retomada de conversas dos agentes (reuso de AiConversation)
- [ ] UI /agentes no crm-web â€” seletor de agente + chat com streaming + aÃ§Ãµes confirmÃ¡veis (padrÃ£o /impeccable)

### Deferido (v1.1 â€” superado por trabalho em sprints)

<!-- Milestone GSD v1.1 "Produtividade Pessoal" foi planejado em 2026-04-03 mas nunca executado pelo GSD.
     O codebase evoluiu por sprints fora do GSD. Itens abaixo permanecem como backlog; alguns podem jÃ¡
     existir parcialmente via sprints (nÃ£o verificado neste milestone). Reavaliar em milestone futuro. -->

- [ ] Morning Briefing (agenda do dia + leads quentes + tarefas + snapshot financeiro)
- [ ] MÃ³dulo de tarefas avulsas (tÃ­tulo, prazo, prioridade, status)
- [ ] Pipeline Kanban visual com drag-and-drop
- [ ] Propostas comerciais com templates + geraÃ§Ã£o por IA + export PDF

### Out of Scope

- Portal do cliente â€” sistema Ã© single-user, clientes nÃ£o acessam
- Assinatura digital legal â€” complexidade jurÃ­dica, defer v2
- App mobile nativo â€” web-first suficiente por ora
- IntegraÃ§Ã£o Google Calendar â€” bidirecional complexo, defer v2
- Agentes que executam aÃ§Ãµes sem confirmaÃ§Ã£o do usuÃ¡rio â€” risco de escrita indevida; toda aÃ§Ã£o que grava dados exige confirmaÃ§Ã£o explÃ­cita

## Context

- Codebase em produÃ§Ã£o desde antes de 2026-04. GSD planning inicializado em 2026-04-03.
- API em SmarterASP.NET (sql1002.site4now.net), frontend em Hostgator (crm.alexandrequeiroz.com.br).
- DB: SQL Server `db_aaf0a8_diaxcrm` â€” SEMPRE usar update-db.ps1 para migrations.
- Frontend Ã© static export â€” sem SSR, sem API routes Next.js.
- Multi-tenancy via IUserOwnedEntity + query filter automÃ¡tico por UserId.

## Constraints

- **Deploy**: Static export Next.js â€” sem server-side rendering, sem server actions
- **DB**: Sempre production (SmarterASP) â€” nunca LocalDB em migrations
- **PDF**: Biblioteca client-side ou geraÃ§Ã£o no backend .NET
- **UI**: shadcn/ui + Tailwind â€” nÃ£o introduzir outras libs de componentes

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Single-user (not SaaS) | Sistema pessoal do Alexandre, nÃ£o produto comercial | âœ“ Good |
| Static export Next.js | Hospedagem simples em shared hosting sem Node.js | âœ“ Good |
| Clean Architecture .NET | SeparaÃ§Ã£o de responsabilidades, testabilidade | âœ“ Good |
| Customer = Lead (modelo unificado) | Evita duplicaÃ§Ã£o de entidade, status distingue fase | âœ“ Good |
| Agentes = motor de chat Ãºnico + prompt/tools/escopo por tipo | Reaproveita IAnthropicChatClient/AiChat; evita 3 stacks separadas | â€” Pending |
| AÃ§Ãµes de escrita dos agentes exigem confirmaÃ§Ã£o do usuÃ¡rio | SeguranÃ§a: IA nÃ£o grava dados sem aprovaÃ§Ã£o explÃ­cita | â€” Pending |
| v1.2 supera v1.1 no GSD | v1.1 nunca executado pelo GSD; cÃ³digo evoluiu via sprints | â€” Pending |

## Evolution

Este documento evolui a cada transiÃ§Ã£o de fase e milestone.

**ApÃ³s cada fase** (via `/gsd:transition`):
1. Requirements invalidados? â†’ Mover para Out of Scope com razÃ£o
2. Requirements validados? â†’ Mover para Validated com referÃªncia de fase
3. Novos requirements emergidos? â†’ Adicionar em Active
4. DecisÃµes a registrar? â†’ Adicionar em Key Decisions

**ApÃ³s cada milestone** (via `/gsd:complete-milestone`):
1. RevisÃ£o completa de todas as seÃ§Ãµes
2. Core Value ainda correto?
3. Auditoria de Out of Scope â€” razÃµes ainda vÃ¡lidas?
4. Atualizar Context com estado atual

---
*Last updated: 2026-09-07 — Phase 7 (Extração — Qualidade na Entrada) completa e verificada em produção; EXTR-01..03 validados. Phase 8 (Import — Dedup e Score) pendente. v1.2 segue pausado em paralelo.*
