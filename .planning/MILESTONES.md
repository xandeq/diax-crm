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

## v1.2 — Agentes de IA (paused)

**Status:** ⏸️ Pausado (desde 2026-05-29, após Phase 02 Wave 1) — não abandonado, retomar com
`/gsd:execute-phase 2`. Motivo: sessão concorrente no mesmo repo. Commits locais em `main`,
**não pushados**; migration `20260529134701_AddAgentFoundation` já aplicada em produção — coordenar
ordem antes de gerar novas migrations. Detalhe completo em `STATE.md` (preservado, não sobrescrito
pelo início do v1.3).

**Goal:** Três agentes de IA (Comercial, Suporte, Pessoal) operando sobre os dados reais do CRM, executando ações sob confirmação, reaproveitando a infra de IA existente sem quebrar nada. Orquestração compartilhada + UI bonita em /agentes (padrão /impeccable).

**Fases:** Phase 2 em andamento (2/4 plans); Phases 3-6 definidas no roadmap, não iniciadas.

---

## v1.3 — Pipeline de Aquisição (current)

**Status:** 🟢 Iniciado (2026-09-05) — requisitos definidos, roadmap em criação. Roda em paralelo ao
v1.2 sem depender dele (arquivos/dados diferentes: extração de leads, import CRM).

**Escopo (confirmado com o usuário):** Extração (A) + Import CRM (B) do backlog original.
Email e WhatsApp (C, D) ficaram em v2 — adiados, não descartados.

**Goal:** Fechar os gaps que sobraram do trabalho de 03-05/09 no pipeline extração→CRM: qualidade
de dado na entrada (MX check no worker .NET, sinal site-próprio-vs-diretório) e rastreabilidade
end-to-end (dedup por `ExternalId` em vez de email, fix do dedup em `/customers/import` p/
`source=Scraping`, score calculado no import).

**Plano de origem:** `.planning/BACKLOG-v1.3-pipeline-aquisicao.md` (mantido como referência do
backlog completo, incluindo os itens v2 C/D/E não escopados nesta rodada).

**Plano completo:** `.planning/BACKLOG-v1.3-pipeline-aquisicao.md` (5 fases: Extração, Import CRM,
Email, WhatsApp, Observabilidade). Não promovido a `ROADMAP.md`/`STATE.md` ainda — aqueles
pertencem ao v1.2 pausado; promover só quando este milestone for formalmente iniciado
(`/gsd:new-milestone`) para não colidir com o v1.2.
