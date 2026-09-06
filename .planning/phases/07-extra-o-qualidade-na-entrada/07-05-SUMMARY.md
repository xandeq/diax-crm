---
phase: 07-extra-o-qualidade-na-entrada
plan: 05
subsystem: api
tags: [csharp, customer-import, website-classification, rejection-counters, xunit]

# Dependency graph
requires: ["07-03", "07-04"]
provides:
  - "ImportRejectionCounts (record) + BulkImportRequest.RejectionCounts (opcional, novo parametro no final, default null)"
  - "ExtractorIntegrationService preenche RejectionCounts com skippedByGeo/rejectedLowQuality/rejectedNoMx"
  - "MapToImportRow propaga lead.Website para ImportCustomerRow.Website (antes se perdia dentro de Notes)"
  - "CustomerImportService grava os 4 contadores de rejeicao em CustomerImport (import.RecordRejectionCounts antes de import.Complete)"
  - "CustomerImportService classifica WebsiteKind no create (customer.SetWebsiteKind) e recalcula no enrich a partir do website FINAL"
affects: ["07-06", "07-07"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "duplicateCount++ como PRIMEIRA linha do ramo 'existingCustomer != null' — conta tanto o enrich bem-sucedido quanto a duplicata ignorada sem novidade"
    - "RecordRejectionCounts chamado com request.RejectionCounts ?? new ImportRejectionCounts() — chamadores sem contadores (CSV manual, testes existentes) continuam funcionando com os 3 primeiros contadores zerados"
    - "WebsiteKind recalculado no enrich a partir do website FINAL (existingCustomer.Website apos a logica de preservacao), nao do row.Website bruto — cobre tambem leads legados que ja tinham website mas nunca tiveram WebsiteKind calculado"

key-files:
  created: []
  modified:
    - api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs
    - api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs
    - api-core/src/Diax.Application/Customers/CustomerImportService.cs
    - api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs
    - api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs

key-decisions:
  - "ImportRejectionCounts NAO inclui duplicado — so o CustomerImportService sabe quantas linhas casaram com Customer existente (o chamador/Extrator ja filtrou antes de saber isso)"
  - "Parametro RejectionCounts adicionado no FINAL do record BulkImportRequest com default null — nenhum chamador posicional existente (CSV manual, testes) quebrou"
  - "Nota 'Website: ...' em Notes preservada (nao removida) — rastreabilidade humana ja usada em producao, mesmo com o Website agora chegando tambem ao campo estruturado"

requirements-completed: [EXTR-02, EXTR-03]

# Metrics
duration: ~15min
completed: 2026-09-06
---

# Phase 07 Plan 05: ImportRejectionCounts atravessa até o CustomerImportService + Website populado Summary

**Os 4 contadores de rejeição (geo/e-mail-lixo/sem-MX/duplicado) agora chegam ao `CustomerImport` persistido de cada rodada, e o `website` do lead do Extrator — antes perdido dentro de `Notes` — chega ao `Customer.Website`/`WebsiteKind` no create e no enriquecimento.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-09-06T10:05:24Z
- **Tasks:** 2
- **Files modified:** 5 (0 created, 5 modified)

## Accomplishments

- `ImportRejectionCounts` (record novo: `GeoRejected`/`LowQualityEmailRejected`/`NoMxRejected`) e `BulkImportRequest.RejectionCounts` (parâmetro opcional no final, default `null`) — nenhum chamador existente quebrou
- `ExtractorIntegrationService` preenche `RejectionCounts` com os 3 contadores locais já calculados (`skippedByGeo`, `rejectedLowQuality`, `rejectedNoMx`) antes de chamar `ImportAsync`
- `MapToImportRow` propaga `lead.Website` para `ImportCustomerRow.Website` (bug real corrigido: antes o valor só virava texto em `Notes` e o campo estruturado ficava sempre `null`) — a nota `"Website: ..."` continua sendo gerada
- `CustomerImportService` grava os 4 contadores em `CustomerImport` via `import.RecordRejectionCounts(...)`, chamado ANTES de `import.Complete(...)` (linha 701 vs 707) — `duplicateCount` computado localmente no ramo de enriquecimento (conta tanto o "enriqueceu" quanto o "duplicata ignorada, nada novo")
- `Customer.SetWebsiteKind(WebsiteClassifier.Classify(row.Website))` chamado no CREATE; no enriquecimento, `WebsiteKind` é recalculado a partir do website FINAL (preservado ou novo) — cobre também leads legados importados antes desta fase, que já tinham website mas nunca tiveram `WebsiteKind` calculado
- 11 novos testes (4 em `ExtractorIntegrationServiceTests`, 7 em `CustomerImportServiceTests`), suíte completa: **824 testes passando** (baseline 813 + 11), 0 falhas, build limpo em Release

## Task Commits

Each task was committed atomically:

1. **Task 1: ImportRejectionCounts no BulkImportRequest + Extractor preenche contadores e Website** - `573d066` (feat)
2. **Task 2: CustomerImportService persiste os 4 contadores e classifica o website** - `ec3a9d0` (feat)

**Plan metadata:** (this commit, next)

## Files Created/Modified

- `api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs` - `record ImportRejectionCounts` + `BulkImportRequest.RejectionCounts` opcional
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` - `RejectionCounts:` preenchido no `BulkImportRequest`; `MapToImportRow` propaga `Website: lead.Website`
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` - `using WebsiteClassification`; `duplicateCount` + `import.RecordRejectionCounts(...)` antes de `import.Complete(...)`; `SetWebsiteKind` no create e recálculo no enrich
- `api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs` - 4 testes: default `null` do `RejectionCounts`, contadores propagados ao `CustomerImport` persistido, `Website` chega ao `Customer`, `Website` ausente vira `null`
- `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` - 7 testes: contadores nulos default zero, contadores+duplicados sobrevivem ao `Complete()`, `WebsiteKind` Directory/OwnSite/Unknown no create, `WebsiteKind` recalculado no enrich (com e sem website preexistente)

## Decisions Made

- `ImportRejectionCounts` não carrega o contador de duplicados — só o `CustomerImportService` sabe quantas linhas casaram com um `Customer` já existente (o `ExtractorIntegrationService` filtra ANTES de saber disso). O `duplicateCount` é computado localmente no `CustomerImportService` e combinado com os 3 contadores recebidos via `request.RejectionCounts ?? new ImportRejectionCounts()` (chamadores sem contadores continuam com os 3 primeiros zerados, sem exceção).
- `WebsiteKind` no enriquecimento é recalculado a partir do website FINAL (`existingCustomer.Website` já com a lógica de preservação aplicada), não do `row.Website` bruto — isso garante que leads legados que já tinham website mas nunca passaram pelo classificador (criados antes desta fase) também ganhem `WebsiteKind` correto na próxima passada de enriquecimento.
- Parâmetro `RejectionCounts` adicionado no FINAL do record `BulkImportRequest`, com default `null` — nenhum chamador posicional existente (import CSV manual, ~20 testes pré-existentes de `CustomerImportServiceTests`) precisou ser tocado.

## Deviations from Plan

None — plano executado exatamente como escrito, usando os blocos de código exatos especificados em cada `<action>`. Único ajuste dentro do próprio ciclo de escrita de testes (não é uma deviation de código de produção): o primeiro rascunho do teste `Import_RecordsRejectionCountsAndDuplicateCount_SurvivesComplete` não pré-preenchia `Tags` dos `Customer` existentes mockados, o que fazia o enriquecimento sempre "achar novidade" (a tag `pilot_candidate` sendo adicionada a um `Tags` inicialmente `null` conta como mudança) e inflava `SuccessCount` em vez de `SkippedCount`. Corrigido pré-preenchendo `existing.UpdateTags("pilot_candidate")` nos dois customers mockados antes de rodar o import, isolando a asserção de duplicados de qualquer outro efeito colateral do enrich.

## Issues Encountered

Nenhum blocker.

## User Setup Required

None — nenhuma configuração externa necessária. Nenhuma migration foi gerada (a migration única de `WebsiteKind`/`ExternalId`/contadores/`mx_cache_entries` sai no plano 07-07, por decisão D-07).

## Next Phase Readiness

- EXTR-02 (persistência) e EXTR-03 (persistência) estão funcionalmente completos: toda rodada de import do Extrator grava os 4 motivos de rejeição em `CustomerImport`, e todo `Customer` criado/enriquecido pelo import nasce/é atualizado com `WebsiteKind` calculado.
- `IMxCacheRepository`/`MxCacheEntry`, `customers.website_kind`, `customers.external_id` e as 4 colunas de contador em `customer_imports` seguem sem tabela/coluna física no banco (nenhuma migration gerada em nenhum plano da fase até aqui, por D-07) — o plano **07-07** precisa gerar a migration única antes de qualquer um desses campos funcionar em produção.
- Suíte completa verde (824 testes, 0 falhas) em Release — nenhum blocker para o próximo plano da fase (07-06).

---
*Phase: 07-extra-o-qualidade-na-entrada*
*Completed: 2026-09-06*

## Self-Check: PASSED

All 3 modified production files verified present on disk (BulkImportDtos.cs, ExtractorIntegrationService.cs, CustomerImportService.cs). Both task commits (`573d066`, `ec3a9d0`) verified in git log. Full test suite re-verified green (824 passed, 0 failed) after both tasks, `dotnet build -c Release` clean (0 errors).
