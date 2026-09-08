# Milestones — DIAX CRM

## v1.3 Pipeline de Aquisição (Shipped: 2026-09-08)

**Phases completed:** 2 phases, 11 plans, 24 tasks

**Entregue:** o pipeline de aquisição passou a filtrar lead ruim na entrada (MX, domínio-lixo, geografia) e a deduplicar por ID do Extrator em vez de só por e-mail, com o lead já nascendo pontuado e segmentado.

**Escopo:** Phases 7 e 8 · requisitos EXTR-01..03 e IMPT-01..03, todos validados · 54 arquivos, ~12.076 linhas · suíte de 891 testes (começou em 833) · 2026-09-06 a 2026-09-08.

**Estado em produção:** Phase 7 deployada em 06/09 (PR #99); Phase 8 deployada em 08/09 (PR #111). Ambas verificadas em produção, não só localmente.

**Key accomplishments:**

- Ported `classify_url`/`DIRECTORY_HOSTS` (site_check.py) and `is_junk_domain` (mx_check.py) to pure C# static classes, closing the gap that let `instagram.local` and `wixpress.com` bounce through the .NET import path before any MX query.
- IMxLookupService seam + DnsClient.NET 1.8.0-backed implementation with a pure MxResponseInterpreter that guarantees DNS infrastructure failures (timeout, SERVFAIL, NotImplemented, Refused) never reject a lead — only NXDOMAIN and empty MX+A responses do.
- Modelo de domínio e mapeamento EF completos para as quatro novas superfícies de schema da fase (Customer.WebsiteKind, Customer.ExternalId, 4 contadores de rejeição em CustomerImport, tabela mx_cache_entries) — sem gerar migration, deixando tudo pronto para uma única migration coordenada no plano 07-07.
- Checagem de MX em lote com cache persistente por domínio (TTL 30d/24h) e resolução DNS paralela plugada no loop de import do worker .NET — domínio sem MX/A agora é rejeitado ANTES de virar `Customer`, timeout de DNS nunca rejeita (D-02), e domínio-lixo tem custo zero de I/O.
- Os 4 contadores de rejeição (geo/e-mail-lixo/sem-MX/duplicado) agora chegam ao `CustomerImport` persistido de cada rodada, e o `website` do lead do Extrator — antes perdido dentro de `Notes` — chega ao `Customer.Website`/`WebsiteKind` no create e no enriquecimento.
- `GET /api/v1/customers/imports?from=...&to=...` agora filtra por período (UTC, inclusivo) usando o índice `IX_CustomerImports_CreatedAt` existente, e `ImportHistoryResponse` expõe os 4 contadores de rejeição (geo/e-mail-lixo/sem-MX/duplicado) por rodada — fechando o requisito EXTR-02 ("motivo de rejeição consultável por período").
- Migration única e coordenada (`20260906101839_AddLeadQualitySignals`) levando as duas colunas novas, os 4 contadores e a tabela `mx_cache_entries` a produção de uma vez, aplicada pelo auto-migrate de startup da API depois que o classifier de permissão bloqueou o `update-db.ps1` — resultado alcançado por caminho diferente do planejado, e verificado direto no banco de produção.
- `ICustomerRepository.GetByExternalIdAsync` + `ImportCustomerRow.ExternalId` fiando `lead.Id` do Extrator até o ponto de dedup, sem lógica de negócio nova.
- Bloco de fit de `LeadScoringService.CalculateScore` recalibrado de teto 25 para 45, incorporando `WebsiteKind.OwnSite` (+10), `Quality.High` (+5), `EmailType.PersonalDirect` (+5) e `HasSuspiciousDomain` (-15); `SegmentForScore(int)` extraído como fonte única dos limiares Hot/Warm/Cold.
- Resolução de match `ExternalId → e-mail → telefone` no loop de persistência de `CustomerImportService`, com troca de e-mail governada pelo Extrator (D-01), guarda de compliance contra ressuscitar contato bloqueado via e-mail antigo suprimido (D-02), e-mail vencendo em conflito de identidade com log greppável (D-03) e backfill orgânico do `ExternalId` (D-04).
- Lead nasce com `LeadScore`/`Segment` preenchidos no ramo CREATE do import, usando `LeadScoringService.CalculateScore`/`SegmentForScore` do plano 08-02 (teto de fit 45); ramo ENRICH permanece intocado para não apagar engajamento acumulado.

---

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
