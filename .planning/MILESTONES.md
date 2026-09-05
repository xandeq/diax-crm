# Milestones — DIAX CRM

## v1.0 — Plataforma Base (shipped)

**Status:** ✅ Completo (em produção antes de 2026-04-03)

**Escopo entregue:**
- Autenticação e RBAC completo
- CRM de leads/clientes com timeline e importação
- Módulo financeiro completo (contas, cartões, transações, planejador, metas)
- Campanhas de e-mail (3 fluxos: outreach, bulk, campaign composer)
- WhatsApp via Evolution API
- Ferramentas IA (humanização, imagem, prompt, HTML)
- Facebook Ads (Graph API)
- Blog, agenda, snippets, checklists
- Admin panel, audit logs, dashboard

---

## v1.1 — Produtividade Pessoal (superado)

**Status:** ⚠️ Superado (planejado 2026-04-03, nunca executado pelo GSD)

**Goal:** Morning Briefing, Tarefas, Pipeline Kanban visual e Propostas comerciais com PDF.

**Nota:** Planejado mas execução seguiu por sprints fora do GSD. Itens remanescentes em "Deferido" no PROJECT.md. Numeração de fases continua (Phase 1 = 01-tarefas).

---

## v1.2 — Agentes de IA (current)

**Status:** 🟡 Em definição (2026-05-28)

**Goal:** Três agentes de IA (Comercial, Suporte, Pessoal) operando sobre os dados reais do CRM, executando ações sob confirmação, reaproveitando a infra de IA existente sem quebrar nada. Orquestração compartilhada + UI bonita em /agentes (padrão /impeccable).

**Fases:** A definir pelo roadmapper (continua a numeração a partir da Phase 2).

---

## v1.3 — Pipeline de Aquisição (proposto)

**Status:** 🔵 Proposto (2026-09-05) — plano escrito, execução não iniciada. Roda em paralelo ao
v1.2 sem depender dele (times/dados diferentes: extração de leads, email, WhatsApp).

**Goal:** Fechar os gaps que sobraram do trabalho de 03-05/09 no pipeline extração→CRM→email→
WhatsApp (já em produção): qualidade de dado na entrada, rastreabilidade end-to-end (dedup por
`ExternalId` em vez de email), escalar volume de email seguindo deliverability, e ativar o canal
WhatsApp que está construído (endpoint `whatsapp-event`, export segmentado, workflow n8n) mas
nunca enviou uma mensagem.

**Plano completo:** `.planning/BACKLOG-v1.3-pipeline-aquisicao.md` (5 fases: Extração, Import CRM,
Email, WhatsApp, Observabilidade). Não promovido a `ROADMAP.md`/`STATE.md` ainda — aqueles
pertencem ao v1.2 pausado; promover só quando este milestone for formalmente iniciado
(`/gsd:new-milestone`) para não colidir com o v1.2.
