# DIAX CRM

## What This Is

Sistema de controle pessoal e profissional de Alexandre Queiroz — CRM privado (single-user) com módulos de finanças, leads, e-mail marketing, WhatsApp, ferramentas IA e gestão de negócios. Não é um SaaS. Stack: Next.js 14 (static export) + .NET 8 Clean Architecture + SQL Server (SmarterASP.NET). Deploy automático via GitHub Actions.

## Core Value

Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas.

## Current State

**v1.3 Pipeline de Aquisição — SHIPPED 2026-09-08.** Phases 7 e 8 executadas, verificadas e
deployadas (PRs #99 e #111). O pipeline extração→CRM→email→WhatsApp agora filtra lead ruim na
entrada (MX, domínio-lixo, geografia), registra o motivo de cada rejeição por rodada, deduplica por
`Customer.ExternalId` com fallback e-mail/telefone, e entrega o lead já pontuado e segmentado.
Suíte de testes: 891.

**Nenhum milestone ativo.** O próximo trabalho planejado é o v1.2 (Agentes de IA), pausado desde
2026-05-29 na Phase 2 (2/4 planos) — ver nota abaixo. Alternativamente, `/gsd:new-milestone` para
abrir um escopo novo; os requisitos v2 deferidos estão em `milestones/v1.3-REQUIREMENTS.md`.

### Milestone entregue: v1.3 Pipeline de Aquisição

**Goal:** Fechar os gaps que sobraram do pipeline extração→CRM→email→WhatsApp (já em produção,
construído em 03-05/09): qualidade de dado na entrada e rastreabilidade end-to-end no import.

**Target features:**
- MX/domínio válido verificado no worker .NET antes de importar (hoje só na ponte Python manual)
- Log do motivo de rejeição (geo/email-lixo/MX/duplicado) por lead, consultável por período
- Sinal "site próprio vs diretório de terceiro" usado no import/score
- `Customer.ExternalId` (dedup robusto por ID do Extrator, não só email)
- Dedup real em `/customers/import` para `source=Scraping`
- `lead_score` calculado no momento do import (hoje só no job diário 06h BRT)

**Nota — v1.2 (Agentes de IA) segue PAUSADO em paralelo**, não abandonado: Phase 02 parou após
Wave 1 (2026-05-29) por sessão concorrente no mesmo repo; commits locais em `main`, **não
pushados**; migration `20260529134701_AddAgentFoundation` já aplicada em produção — cuidado com
ordem de migrations se retomado. Detalhe completo preservado em `STATE.md`. Retomar com
`/gsd:execute-phase 2` quando decidido — v1.3 não toca nos arquivos de `src/Diax.Domain/Agents/*`
nem correlatos.

**Target features (v1.2, referência — não descontinuadas):**
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
- ✓ EXTR-01: MX/domínio válido verificado antes de importar, com degradação segura (falha de DNS não rejeita lead) — v1.3 Phase 7
- ✓ EXTR-02: motivo de rejeição contado por rodada e consultável por período (`GET /customers/imports?from&to`) — v1.3 Phase 7
- ✓ EXTR-03: site próprio vs diretório de terceiro classificado e persistido (`Customer.WebsiteKind`) — v1.3 Phase 7
- ✓ IMPT-01: dedup do import por `Customer.ExternalId` (ID do Extrator), com fallback para email e telefone — v1.3 Phase 8
- ✓ IMPT-02: dedup real em `/customers/import` para `source=Scraping`, com paridade ao comportamento de `source=Import` — v1.3 Phase 8
- ✓ IMPT-03: `lead_score` e segmento calculados no momento do import (ramo CREATE), com o bloco de fit recalibrado para que um lead forte nasça Warm — v1.3 Phase 8

### Active

<!-- Milestone v1.3 — Pipeline de Aquisição · COMPLETO (Phases 7 e 8) -->

<!-- Nenhum requisito de v1.3 em aberto. -->

<!-- Milestone v1.2 — Agentes de IA (PAUSADO em paralelo, ver nota em Current Milestone acima) -->

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
| Falha de DNS nunca rejeita lead (só NXDOMAIN e MX+A vazios) | Timeout/SERVFAIL é problema de infraestrutura, não sinal sobre o lead | ✓ Good — v1.3 Phase 7 |
| Migration única e coordenada entre Phases 7 e 8 | Evita duas idas ao banco de produção; a coluna da Phase 8 viaja junto com a da 7 | ✓ Good — v1.3 |
| Dedup por `ExternalId` com fallback e-mail→telefone | E-mail sozinho perde o lead que troca de endereço entre passadas do scraper | ✓ Good — v1.3 Phase 8 |
| Em conflito de identidade, o e-mail vence e o conflito vira log | Regravar `ExternalId` arriscaria fundir dois clientes distintos em silêncio | ✓ Good — v1.3 Phase 8 |
| Score no import só no ramo CREATE | Repontuar cliente enriquecido passaria `engagement: null` e rebaixaria um Hot 75 para Warm 40 | ✓ Good — v1.3 Phase 8 |
| Teto do fit em 45, abaixo do `HotThreshold` 60 | Lead bom nasce Warm sem engajamento; Hot continua exigindo engajamento real | ✓ Good — v1.3 Phase 8 |
| Backfill de `ExternalId` fora de escopo | ~2300 registros antigos não têm como ser casados retroativamente com segurança | — Pending — reavaliar se a dedup por e-mail começar a falhar |

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
*Last updated: 2026-09-08 — v1.3 Pipeline de Aquisição arquivado e deployado. Nenhum milestone ativo; v1.2 (Agentes de IA) segue pausado na Phase 2 (2/4 planos). Débito técnico conhecido: o dry-run de `/customers/import` não replica a guarda in-batch de `ExternalId`, então a prévia diverge do import real nesse caso.*
