# Phase 8: Import — Dedup e Score em Tempo Real - Context

**Gathered:** 2026-09-07
**Status:** Ready for planning

<domain>
## Phase Boundary

O import do CRM deduplica de forma robusta por ID externo do Extrator (não só e-mail) e calcula o
`lead_score` no momento em que o lead entra, sem esperar o job diário.

Requisitos: IMPT-01 (`Customer.ExternalId` com dedup), IMPT-02 (dedup real em
`/customers/import` para `source=Scraping`), IMPT-03 (`lead_score` no import).

Fora desta fase: MX check, filtro de lixo, classificação de site, contadores de rejeição — tudo
isso é Phase 7, já entregue e em produção.

</domain>

<decisions>
## Implementation Decisions

### Colisão de ExternalId (discutido com o usuário)
- **D-01:** Mesmo `ExternalId`, e-mail diferente → **atualiza o e-mail do lead existente**. O
  Extrator é fonte da verdade do contato; se o negócio trocou de e-mail, queremos falar com o novo.
- **D-02:** ⚠️ **Risco de compliance que a D-01 cria, e que o planner DEVE tratar:** se o e-mail
  ANTIGO tinha opt-out (`EmailOptOut`) ou estava em `email_suppressions`, trocar o e-mail NÃO pode
  ressuscitar o contato. O opt-out/supressão precisa ser carregado para o e-mail novo (ou o lead
  permanece bloqueado). Isso não é opcional — é o mesmo tipo de furo que o incidente de 30/08
  (leads lixo emailados) produziu.
- **D-03:** `ExternalId` casa com o lead A e o e-mail casa com o lead B (leads DIFERENTES no CRM)
  → **o e-mail vence**. Enriquece o lead B, e o conflito é registrado (contador/log) para
  inspeção posterior. Racional: o e-mail é o canal real de envio e é a chave usada por
  supressões e opt-out; deixar o `ExternalId` reescrever o e-mail de um lead com histórico de
  envio seria pior.
- **D-04:** Lead JÁ existente que casa por e-mail e traz `ExternalId` → **grava o `ExternalId`
  nele** (caminho de enriquecimento). Backfill orgânico: a rodada de 05/09 casou 362 leads por
  e-mail, então a base se popula sozinha em poucos dias, sem script de mutação em massa.

### Claude's Discretion (áreas que o usuário optou por não discutir)
O usuário escolheu discutir apenas a colisão de `ExternalId`. As decisões abaixo ficam comigo,
guiadas pelo que já foi decidido acima:

- **Backfill em massa dos 8.056 leads com `external_id` nulo:** NÃO fazer. A D-04 resolve
  organicamente e evita mutação em massa em produção. Se o planner concluir que é insuficiente,
  levantar como questão em aberto em vez de decidir sozinho por um script de backfill.
- **Escopo do fix de `source=Scraping` (IMPT-02):** o scout mostrou que o dedup de Scraping NÃO
  está ausente — o bloco de validação roda só `if (DryRun || Source == Import)`
  (`CustomerImportService.cs` ~linha 86), mas existe um caminho de enriquecimento na persistência
  que casa por e-mail (foi o que produziu os 362 updates de 05/09). Então IMPT-02 é sobre
  **unificar o comportamento**, não sobre criar dedup do zero. O planner deve mapear a divergência
  real entre os dois caminhos antes de propor a correção.
- **`lead_score` no import (IMPT-03):** ⚠️ **questionar o valor antes de implementar.** No momento
  do import não existe engajamento, e com o scoring rebalanceado da Phase 7 o bloco de fit soma no
  máximo 25 pontos — abaixo do limiar de Warm (30). Ou seja, o score calculado no import será
  SEMPRE `Cold`, para todo lead, até o `LeadScoringWorker` rodar às 06h BRT. Implementar isso
  literalmente entrega um valor constante. O planner deve avaliar se o requisito se cumpre melhor
  (a) calculando mesmo assim, para o lead nunca ficar com score nulo/zero, ou (b) reinterpretando
  o requisito. Se (b), parar e levantar a questão — não redefinir requisito sozinho.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

Projeto não usa ADRs formais. As referências são o código real e o contexto da fase anterior:

### Código que será modificado
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` — bloco de validação
  (~linha 86, gate `DryRun || Source == Import`), rejeição de duplicata (~linha 243), caminho de
  enriquecimento com `duplicateCount` (~linha 322)
- `api-core/src/Diax.Domain/Customers/Customer.cs` — `ExternalId` (linha ~86) e `SetExternalId`
  (linha ~298), criados na Phase 7 e nunca populados
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` — quem monta as
  `ImportCustomerRow` a partir de `ExtractorLead` (o `lead.Id` do Extrator vive aqui)
- `api-core/src/Diax.Application/Customers/LeadScoringService.cs` — `CalculateScore(Customer,
  CustomerEngagementSummary?, DateTime)`, estático; limiares `HotThreshold=60`/`WarmThreshold=30`

### Contexto da fase anterior (decisões que restringem esta)
- `.planning/phases/07-extra-o-qualidade-na-entrada/07-CONTEXT.md` §D-07 — a coluna `ExternalId`
  foi criada na Phase 7 justamente para esta fase consumir; migration já aplicada em produção
- `.planning/phases/07-extra-o-qualidade-na-entrada/07-VERIFICATION.md` — confirma que a Phase 7
  NÃO implementou lógica de dedup por `ExternalId` (era escopo desta fase)

### Incidente que motiva a D-02
- `docs/email-marketing/INCIDENTE-leads-extrator-2026-08-30.md` — quando o filtro de consentimento
  falha, leads suprimidos voltam a ser emailados. É o modo de falha que a troca de e-mail da D-01
  pode reintroduzir se o opt-out não for carregado junto.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Customer.SetExternalId(string?)` já existe (Phase 7) — normaliza vazio para null
- Índice único filtrado `IX_Customers_ExternalId` (`[external_id] IS NOT NULL`) já está em
  produção — múltiplos nulos convivem, mas dois `ExternalId` iguais dão erro de constraint;
  o código precisa tratar isso, não só confiar no banco
- `ICustomerRepository.GetByEmailAsync` / `GetByPhoneAsync` — padrão de lookup a seguir para um
  eventual `GetByExternalIdAsync`
- `ImportRejectionCounts` (Phase 7) — estrutura pronta caso o conflito da D-03 vire contador

### Established Patterns
- `Result<T>` em toda a camada Application
- Testes: xUnit + Moq, **sem FluentAssertions** (não está no `.csproj` — `Should()` não compila)
- Build/test **sempre** com `-c Release` (Smart App Control bloqueia DLL de teste Debug)
- Baseline da suíte ao entrar nesta fase: **833 testes passando**

### Integration Points
- `ExtractorIntegrationService.MapToImportRow` → `ImportCustomerRow` → `CustomerImportService`
- `ExtractorPullWorker` (diário 15:00 UTC / 12:00 BRT) é quem exercita esse caminho em produção

</code_context>

<specifics>
## Specific Ideas

- "Backfill orgânico" é preferido a script de migração em massa — a base se popula sozinha nas
  rodadas diárias
- O conflito da D-03 deve ser observável (contador/log), não silencioso — o usuário quer poder
  inspecionar depois

</specifics>

<deferred>
## Deferred Ideas

- **Script de backfill em massa do `external_id`** — decidido contra (D-04 resolve organicamente).
  Reavaliar só se o ritmo orgânico se mostrar insuficiente.
- **Endpoint proxy para a API do OpenRouter** em `api.alexandrequeiroz.com.br`, espelhando o proxy
  da Anthropic que já existe — pedido pelo usuário em 07/09 para ser feito "em algum momento",
  interrompendo esta fase. **Não é escopo da Phase 8**; anotado aqui para não se perder.

</deferred>

---

*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Context gathered: 2026-09-07*
