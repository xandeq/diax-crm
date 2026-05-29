# DIAX CRM

## What This Is

Sistema de controle pessoal e profissional de Alexandre Queiroz — CRM privado (single-user) com módulos de finanças, leads, e-mail marketing, WhatsApp, ferramentas IA e gestão de negócios. Não é um SaaS. Stack: Next.js 14 (static export) + .NET 8 Clean Architecture + SQL Server (SmarterASP.NET). Deploy automático via GitHub Actions.

## Core Value

Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas.

## Current Milestone: v1.2 Agentes de IA

**Goal:** Transformar o CRM de repositório de dados em parceiro de execução por IA — três agentes (Comercial, Suporte, Pessoal) que conversam usando os DADOS REAIS do CRM e podem executar ações sob confirmação, reaproveitando a infra de IA existente sem quebrar nada.

**Target features:**
- Agente Comercial — qualifica leads, prioriza pipeline, gera outreach, atualiza status/segmento (parcialmente construído)
- Agente de Suporte — atende com base no histórico do cliente, sugere respostas, abre/tria tickets
- Agente Pessoal — resume agenda e finanças, cria compromissos
- Orquestração compartilhada (AgentOrchestrator + framework de tools/function-calling com confirmação)
- UI /agentes bonita no crm-web (padrão /impeccable, reaproveitando o chat de ai-chat)

## Requirements

### Validated

<!-- Funcionalidades já em produção — v1.0 -->

- ✓ Autenticação JWT com sessão por browser — v1.0
- ✓ Gestão de leads e clientes (modelo unificado Customer) — v1.0
- ✓ Timeline de relacionamento com cliente — v1.0
- ✓ Importação de leads (CSV, Extrator de Dados) — v1.0
- ✓ Agenda de compromissos com importação por texto — v1.0
- ✓ Módulo financeiro completo (contas, cartões, transações, planejador, metas, recorrentes) — v1.0
- ✓ Campanhas de e-mail (outreach, bulk send, campaign composer + worker Brevo) — v1.0
- ✓ WhatsApp via Evolution API — v1.0
- ✓ Ferramentas IA (humanização, geração de imagens, prompt generator, extração HTML) — v1.0
- ✓ Módulo Facebook Ads (Graph API v21.0) — v1.0
- ✓ Blog management — v1.0
- ✓ Admin panel (usuários, grupos, RBAC, provedores IA) — v1.0
- ✓ Audit logs e system logs — v1.0
- ✓ Snippets e checklists domésticos — v1.0
- ✓ Dashboard geral e analytics — v1.0

### Active

<!-- Milestone v1.2 — Agentes de IA -->

- [ ] Agente Comercial: chat sobre o pipeline real de leads, prioriza, gera outreach e atualiza status/segmento sob confirmação
- [ ] Agente de Suporte: atende com histórico do cliente, sugere respostas e abre/tria tickets
- [ ] Agente Pessoal: resume agenda e finanças, cria compromissos sob confirmação
- [ ] Orquestração de agentes (AgentOrchestrator) + framework de tools/function-calling com confirmação do usuário
- [ ] Persistência/retomada de conversas dos agentes (reuso de AiConversation)
- [ ] UI /agentes no crm-web — seletor de agente + chat com streaming + ações confirmáveis (padrão /impeccable)

### Deferido (v1.1 — superado por trabalho em sprints)

<!-- Milestone GSD v1.1 "Produtividade Pessoal" foi planejado em 2026-04-03 mas nunca executado pelo GSD.
     O codebase evoluiu por sprints fora do GSD. Itens abaixo permanecem como backlog; alguns podem já
     existir parcialmente via sprints (não verificado neste milestone). Reavaliar em milestone futuro. -->

- [ ] Morning Briefing (agenda do dia + leads quentes + tarefas + snapshot financeiro)
- [ ] Módulo de tarefas avulsas (título, prazo, prioridade, status)
- [ ] Pipeline Kanban visual com drag-and-drop
- [ ] Propostas comerciais com templates + geração por IA + export PDF

### Out of Scope

- Portal do cliente — sistema é single-user, clientes não acessam
- Assinatura digital legal — complexidade jurídica, defer v2
- App mobile nativo — web-first suficiente por ora
- Integração Google Calendar — bidirecional complexo, defer v2
- Agentes que executam ações sem confirmação do usuário — risco de escrita indevida; toda ação que grava dados exige confirmação explícita

## Context

- Codebase em produção desde antes de 2026-04. GSD planning inicializado em 2026-04-03.
- API em SmarterASP.NET (sql1002.site4now.net), frontend em Hostgator (crm.alexandrequeiroz.com.br).
- DB: SQL Server `db_aaf0a8_diaxcrm` — SEMPRE usar update-db.ps1 para migrations.
- Frontend é static export — sem SSR, sem API routes Next.js.
- Multi-tenancy via IUserOwnedEntity + query filter automático por UserId.

## Constraints

- **Deploy**: Static export Next.js — sem server-side rendering, sem server actions
- **DB**: Sempre production (SmarterASP) — nunca LocalDB em migrations
- **PDF**: Biblioteca client-side ou geração no backend .NET
- **UI**: shadcn/ui + Tailwind — não introduzir outras libs de componentes

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Single-user (not SaaS) | Sistema pessoal do Alexandre, não produto comercial | ✓ Good |
| Static export Next.js | Hospedagem simples em shared hosting sem Node.js | ✓ Good |
| Clean Architecture .NET | Separação de responsabilidades, testabilidade | ✓ Good |
| Customer = Lead (modelo unificado) | Evita duplicação de entidade, status distingue fase | ✓ Good |
| Agentes = motor de chat único + prompt/tools/escopo por tipo | Reaproveita IAnthropicChatClient/AiChat; evita 3 stacks separadas | — Pending |
| Ações de escrita dos agentes exigem confirmação do usuário | Segurança: IA não grava dados sem aprovação explícita | — Pending |
| v1.2 supera v1.1 no GSD | v1.1 nunca executado pelo GSD; código evoluiu via sprints | — Pending |

## Evolution

Este documento evolui a cada transição de fase e milestone.

**Após cada fase** (via `/gsd:transition`):
1. Requirements invalidados? → Mover para Out of Scope com razão
2. Requirements validados? → Mover para Validated com referência de fase
3. Novos requirements emergidos? → Adicionar em Active
4. Decisões a registrar? → Adicionar em Key Decisions

**Após cada milestone** (via `/gsd:complete-milestone`):
1. Revisão completa de todas as seções
2. Core Value ainda correto?
3. Auditoria de Out of Scope — razões ainda válidas?
4. Atualizar Context com estado atual

---
*Last updated: 2026-05-28 — milestone v1.2 Agentes de IA iniciado (supera v1.1)*
