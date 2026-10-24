# DIAX CRM

## What This Is

Sistema de controle pessoal e profissional de Alexandre Queiroz — CRM privado (single-user) com módulos de finanças, leads, e-mail marketing, WhatsApp, ferramentas IA e gestão de negócios. Não é um SaaS. Stack: Next.js 14 (static export) + .NET 8 Clean Architecture + SQL Server (SmarterASP.NET). Deploy automático via GitHub Actions.

## Core Value

Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas.

## Current State

**v1.4 Funil de Outreach — Cadência e Qualidade — CONCLUÍDO 2026-09-20 (6/6 fases), ainda NÃO
mergeado em `main`.** Backlog pré-v1.3 recalculado com o motor oficial de score; 79+ scripts
`fire_*.py` consolidados num motor único (`waves_core.py`) e pipeline tirado da dependência do
notebook; Qualified passou a avançar automaticamente; ferramentas de escala de email e WhatsApp
entregues e provadas (3 dos 14 requisitos — EMAL-01/02/04 — fecharam `Partial` por decisão
deliberada de segurança/calendário, não por falha); dashboard de conversão do funil verificado em
produção. Todo o trabalho está na branch `chore/email-automation-versioned` (pushada, 209 commits
na frente de `main`) — avisar o usuário sobre a ordem de merge antes de tratar como "em produção".

## Current Milestone: v1.5 Fechar Venda

**Goal:** O CRM passa a acompanhar a venda depois do primeiro contato — lead que respondeu ganha
uma proposta de pacote com preço fixo (Rascunho → revisão do dono → envio), com follow-up
automático e painel de receita, e o bot do WhatsApp passa a conduzir a conversa até esse rascunho
(primeiro em modo sombra, nunca enviando nada até aprovação humana).

**Contexto de origem:** 45 dias de outreach (738 e-mails, ~0,1% resposta humana; 553 WhatsApps,
~34% resposta incluindo autoresposta) não geraram nenhuma proposta desde 03/07 — pipeline com R$0.
O bot do WhatsApp (`n8n-local/workflows/waha-bot-atendente.json`) responde sem histórico, sem saber
quem é o lead e sem objetivo. Decisões do usuário: gargalo = fechar venda; passo que fecha = proposta
direto (sem call); preço por pacotes fixos; abordagem em 2 fatias (CRM primeiro, bot depois, sempre
com revisão humana antes de qualquer envio).

**Target features:**
- Catálogo `service_packages` com preço fixo + CRUD via API e tela em crm-web
- "Gerar proposta" a partir de um pacote no card do lead e no pipeline (preenche título/descrição/
  valor automaticamente, validade 7 dias)
- Worker diário de follow-up de proposta (não vista 48h / vista sem aceite 48h / vencida)
- Painel de receita: R$ em proposta aberta no dashboard comercial
- Evento `proposal_draft` + endpoint de catálogo para o bot
- Bot vendedor de WhatsApp em modo sombra (decide mas nunca envia, até revisão humana)

**Fora do v1.5:** gateway de pagamento, contrato/assinatura, itens adicionais por proposta,
ativação real do bot (plano separado, só após revisão humana da sombra).

**Plano de origem:** `docs/plans/2026-10-23-fechar-venda-design.md` (design) e
`docs/plans/2026-10-23-fechar-venda.md` (plano task-by-task, Tasks 1-13). Fases 15-20 no
`ROADMAP.md`. Fase 15 (catálogo de pacotes, Tasks 1-4, PR-a) em execução por superpowers-dev no
momento em que este milestone foi registrado no GSD (2026-10-24).

**Nota — v1.2 (Agentes de IA) segue PAUSADO em paralelo**, não abandonado: Phase 02 parou após
Wave 1 (2026-05-29) por sessão concorrente no mesmo repo; commits locais em `main`, **não
pushados**; migration `20260529134701_AddAgentFoundation` já aplicada em produção — cuidado com
ordem de migrations se retomado. Detalhe completo preservado em `STATE.md`. Retomar com
`/gsd:execute-phase 2` quando decidido — v1.5 não toca nos arquivos de `src/Diax.Domain/Agents/*`
nem correlatos.

### Milestone entregue: v1.4 Funil de Outreach — Cadência e Qualidade

**Goal:** Profissionalizar o funil de outreach de ponta a ponta — limpar/pontuar o backlog que
antecede o v1.3, dar cadência real ao follow-up (email+WhatsApp), tornar "Qualified" um critério
objetivo e automático, consolidar o script sprawl de disparo, e tirar o pipeline da dependência de
um único notebook.

**Contexto de origem:** sessão de 2026-09-17/19 auditou o backlog de 5.266 leads pré-v1.3 (nunca
tinham passado pelo `LeadScoringService`/filtro MX novos) e achou ~20% de lixo confirmado (contaminação
internacional de busca BR, telefone-fantasma capturando range de ano do rodapé, multiplicação
1-lead-por-email-na-página). 1.065 já marcados `Inactive` via script ad-hoc
(`diax-crm/scripts/mark_junk_inactive.py`) e 1.728 válidos priorizados por fit score
(`outreach_priority_valid_leads.csv`) — esse fit score era um script Python improvisado, substituído
pelo backfill oficial da Phase 9.

**Phase 9 concluída (2026-09-19):** backlog de 7.242 leads abertos recalculado via
`LeadScoringService.RecomputeAllAsync` oficial — 18 Hot, 1.118 Warm, 6.106 Cold, `unsegmentedOpenLeads=0`,
idempotência provada (2 disparos idênticos). Achado durante o checkpoint: 3 das 13 tarefas de
follow-up geradas eram contaminação (página de review chinesa, contato do Ministério Público-ES,
email genérico) — mesmo padrão de ruído da auditoria de 17/09, mas em leads *novos* (pós-limpeza).
Confirma que o `LeadScoringService` oficial não filtra esse tipo de ruído — é upstream, fica
sinalizado para as Phases 10/12. Evidência completa: `docs/lead-quality/BACKFILL-EVIDENCE.md`.

**Target features:**
- Backfill: recalcular `LeadScore`/`Segment` do backlog pré-v1.3 usando `LeadScoringService` real (não o script python), reaproveitando a classificação junk/suspect/valid já feita como sinal de entrada
- EMAL-01..04: DMARC pct=25→100, volume 87→300/dia com warmup, medição real do A/B de subject, leads "webmail" (560) testados com cap+tag própria
- WHAT-01..04: handoff n8n→WAHA confirmado, bot IA desvia pra humano em vez de responder, relatório semanal do canal, escalar wa_es→wa_br
- Qualified objetivo: critério explícito de avanço Contacted→Qualified + automação via `reply_watch.py`
- Consolidar os 79+ scripts `fire_*.py` (`docs/email-marketing/`) num motor único parametrizado
- OBSV-01: pipeline (waves, vigia de respostas, n8n, WAHA) para de depender só do notebook via Task Scheduler
- Dashboard de conversão por etapa do funil (Lead→Contacted→Qualified→Customer, com %)

**Fora do v1.4:** EXTR-04 (Google Places API) — exploratório, fica no backlog de v2.

**Nota — v1.2 (Agentes de IA) segue PAUSADO em paralelo**, não abandonado: Phase 02 parou após
Wave 1 (2026-05-29) por sessão concorrente no mesmo repo; commits locais em `main`, **não
pushados**; migration `20260529134701_AddAgentFoundation` já aplicada em produção — cuidado com
ordem de migrations se retomado. Detalhe completo preservado em `STATE.md`. Retomar com
`/gsd:execute-phase 2` quando decidido — v1.4 não toca nos arquivos de `src/Diax.Domain/Agents/*`
nem correlatos.

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
- ✓ BFIL-01: Backlog pré-v1.3 recalculado via `LeadScoringService` real (LeadScore/Segment) — v1.4 Phase 9
- ✓ SCRP-01: Motor único parametrizado (`waves_core.py`) substitui os 79+ scripts `fire_*.py` — v1.4 Phase 10
- ✓ OBSV-01: Pipeline não depende só do notebook — watchdogs com alerta Telegram + Scheduled Tasks — v1.4 Phase 10
- ✓ QUAL-01/QUAL-02: Critério objetivo de avanço Contacted→Qualified documentado e automático via `reply_watch.py` — v1.4 Phase 11
- ✓ EMAL-03: A/B de subject medido por taxa de clique real (`ab_report.py`) — v1.4 Phase 12
- ✓ WHAT-01..04: handoff confirmado (equivalência funcional), bot de desvio, relatório semanal, escalonamento wa_es→wa_br — v1.4 Phase 13
- ✓ DASH-01: Dashboard de conversão por etapa do funil verificado em produção — v1.4 Phase 14
- ⚠ EMAL-01/EMAL-02/EMAL-04: mecanismo entregue e provado (DMARC drill, curva de warmup, cap webmail), degrau real segurado por decisão deliberada do usuário (bounce rate acima do próprio limiar de reversão) — v1.4 Phase 12, `Partial` — ver `milestones/v1.4-REQUIREMENTS.md`

### Active

<!-- Milestone v1.5 — Fechar Venda -->

- [ ] PKG-01: Catálogo `service_packages` (preço fixo) com CRUD via API
- [ ] PKG-02: `Proposal` ganha `PackageId`/`Origin`/`LastFollowUpStage`
- [ ] PKG-03: Gerar proposta a partir de um pacote preenche título/descrição/valor automaticamente
- [ ] PKG-04: Tela `/propostas/pacotes` em crm-web
- [ ] PKG-05: Seletor de pacote no modal "Gerar proposta" do pipeline (validade 7 dias)
- [ ] FUP-01: Critério objetivo e idempotente de follow-up de proposta por etapa
- [ ] FUP-02: Worker diário de follow-up (`job_runs`)
- [ ] REV-01: Painel de receita — R$ em proposta aberta
- [ ] BOT-01: Evento `proposal_draft` + endpoint de catálogo para o bot
- [ ] BOT-02: Bot vendedor em modo sombra (decide, nunca envia)
- [ ] BOT-03: Modo sombra conectado ao `outreach-watch.py` sem alterar comportamento atual

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
*Last updated: 2026-10-24 — v1.4 Funil de Outreach arquivado (concluído 6/6 fases 2026-09-20, ainda não mergeado em `main`); milestone v1.5 Fechar Venda iniciado, Phase 15 (catálogo de pacotes) em execução por superpowers-dev. v1.2 (Agentes de IA) segue pausado na Phase 2 (2/4 planos). Débito técnico conhecido: o dry-run de `/customers/import` não replica a guarda in-batch de `ExternalId`, então a prévia diverge do import real nesse caso.*
