---
phase: 07-extra-o-qualidade-na-entrada
plan: 03
subsystem: api
tags: [csharp, efcore, domain-modeling, mx-cache, dedup, schema-prep]

# Dependency graph
requires: ["07-01", "07-02"]
provides:
  - "Customer.WebsiteKind (Unknown/OwnSite/Directory, default Unknown) + Customer.SetWebsiteKind(kind)"
  - "Customer.ExternalId (nullable, whitespace-normalizes to null) + Customer.SetExternalId(externalId)"
  - "CustomerImport 4 contadores de rejeicao (Geo/LowQualityEmail/NoMx/Duplicate) + RecordRejectionCounts(...)"
  - "MxCacheEntry (AuditableEntity) + IMxCacheRepository/MxCacheRepository (GetByDomainAsync/GetByDomainsAsync), registrado no DI"
  - "EF configurations: customers.website_kind, customers.external_id (indice unico filtrado IX_Customers_ExternalId), customer_imports 4 colunas int default 0, tabela mx_cache_entries + DbSet<MxCacheEntry>"
affects: ["07-04", "07-06", "07-07"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ResultCode como int em MxCacheEntry (Domain) em vez do enum MxCheckResult (Application) — Domain nunca referencia Application; conversao fica a cargo do consumidor em 07-04"
    - "Setter de dominio dedicado (SetWebsiteKind/SetExternalId) em vez de sobrecarregar UpdateContactInfo, para nao quebrar ~15 chamadas existentes"
    - "Indice unico filtrado (HasFilter '[coluna] IS NOT NULL') repetido do padrao ja usado em Document, agora tambem em ExternalId"

key-files:
  created:
    - api-core/src/Diax.Domain/Customers/MxCacheEntry.cs
    - api-core/src/Diax.Domain/Customers/IMxCacheRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Repositories/MxCacheRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Configurations/MxCacheEntryConfiguration.cs
    - api-core/tests/Diax.Tests/Customers/CustomerImportCountersTests.cs
    - api-core/tests/Diax.Tests/Customers/MxCacheEntryTests.cs
  modified:
    - api-core/src/Diax.Domain/Customers/Customer.cs
    - api-core/src/Diax.Domain/Customers/CustomerImport.cs
    - api-core/src/Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs
    - api-core/src/Diax.Infrastructure/Data/Configurations/CustomerImportConfiguration.cs
    - api-core/src/Diax.Infrastructure/Data/DiaxDbContext.cs
    - api-core/src/Diax.Infrastructure/DependencyInjection.cs

key-decisions:
  - "MxCacheEntry.ResultCode fica int, nao MxCheckResult — Diax.Domain nao pode referenciar Diax.Application (regra de Clean Architecture do projeto); a conversao para o enum acontece na camada Application no plano 07-04"
  - "Zero migration gerada neste plano por decisao D-07 — a migration unica (WebsiteKind + ExternalId + contadores + mx_cache_entries) sai no plano 07-07, depois que o modelo da fase inteira estiver estavel"
  - "IsFresh(nowUtc, validFor) recebe o momento atual como parametro em vez de usar DateTime.UtcNow internamente — permite testar janelas de TTL (30 dias, 24h) sem sleep nem reflection sobre CheckedAt"

requirements-completed: [EXTR-01, EXTR-02, EXTR-03]

# Metrics
duration: ~25min
completed: 2026-09-06
---

# Phase 07 Plan 03: Domínio + EF Config — WebsiteKind, ExternalId, contadores de rejeição, cache de MX Summary

**Modelo de domínio e mapeamento EF completos para as quatro novas superfícies de schema da fase (Customer.WebsiteKind, Customer.ExternalId, 4 contadores de rejeição em CustomerImport, tabela mx_cache_entries) — sem gerar migration, deixando tudo pronto para uma única migration coordenada no plano 07-07.**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-09-06T09:39:04Z
- **Tasks:** 3
- **Files modified:** 12 (6 created, 6 modified)

## Accomplishments

- `Customer.WebsiteKind` (default `Unknown`) e `Customer.ExternalId` (nullable, trim + whitespace→null) com setters de domínio dedicados (`SetWebsiteKind`, `SetExternalId`), sem alterar `UpdateContactInfo`
- `CustomerImport` com 4 contadores agregados de rejeição (`GeoRejectedCount`, `LowQualityEmailRejectedCount`, `NoMxRejectedCount`, `DuplicateRejectedCount`) e `RecordRejectionCounts(...)`, que normaliza negativos para 0 e nunca mexe em `Status`/`SuccessCount`/`FailedCount`
- `MxCacheEntry` (nova entidade `AuditableEntity`) + `IMxCacheRepository`/`MxCacheRepository` (`GetByDomainAsync`, `GetByDomainsAsync` em lote), registrados no DI — cache persistente por domínio com TTL avaliado em código via `IsFresh(nowUtc, validFor)`
- EF Configurations: `customers.website_kind` (int, default Unknown), `customers.external_id` (nvarchar(64), índice único filtrado `IX_Customers_ExternalId` com `[external_id] IS NOT NULL`), `customer_imports` com 4 colunas `int NOT NULL DEFAULT 0`, tabela nova `mx_cache_entries` (índice único em `domain`, índice em `checked_at`) + `DbSet<MxCacheEntry>`
- Zero migration gerada — confirmado por `git status` limpo em `Data/Migrations/` e ausência de arquivos com `websitekind`/`externalid`/`mxcache` no nome
- 15 novos testes (9 + 6), suíte completa: **796 testes passando** (baseline 781 da wave 1 + 15 deste plano), 0 falhas, build limpo em Release

## Task Commits

Each task was committed atomically:

1. **Task 1: Customer.WebsiteKind + Customer.ExternalId + contadores em CustomerImport** - `8585fee` (feat)
2. **Task 2: Entidade MxCacheEntry + IMxCacheRepository + MxCacheRepository + DI** - `537be06` (feat)
3. **Task 3: Configurations EF + DbSet (customers.website_kind, customers.external_id, contadores, mx_cache_entries)** - `33bbeb5` (feat)

**Plan metadata:** (this commit, next)

## Files Created/Modified

- `api-core/src/Diax.Domain/Customers/Customer.cs` - `WebsiteKind`/`ExternalId` properties + `SetWebsiteKind`/`SetExternalId`
- `api-core/src/Diax.Domain/Customers/CustomerImport.cs` - 4 contadores + `RecordRejectionCounts(...)`, inicialização explícita no construtor
- `api-core/src/Diax.Domain/Customers/MxCacheEntry.cs` - entidade nova, `AuditableEntity`, `ResultCode` int, `Normalize`/`Refresh`/`IsFresh`
- `api-core/src/Diax.Domain/Customers/IMxCacheRepository.cs` - `GetByDomainAsync`/`GetByDomainsAsync`
- `api-core/src/Diax.Infrastructure/Data/Repositories/MxCacheRepository.cs` - implementação EF, molde de `CustomerImportRepository`
- `api-core/src/Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs` - mapeamento `WebsiteKind`/`ExternalId` + `IX_Customers_ExternalId`
- `api-core/src/Diax.Infrastructure/Data/Configurations/CustomerImportConfiguration.cs` - 4 colunas `int NOT NULL DEFAULT 0`
- `api-core/src/Diax.Infrastructure/Data/Configurations/MxCacheEntryConfiguration.cs` - tabela nova `mx_cache_entries`
- `api-core/src/Diax.Infrastructure/Data/DiaxDbContext.cs` - `DbSet<MxCacheEntry> MxCacheEntries`
- `api-core/src/Diax.Infrastructure/DependencyInjection.cs` - `IMxCacheRepository → MxCacheRepository`
- `api-core/tests/Diax.Tests/Customers/CustomerImportCountersTests.cs` - 9 testes (contadores + WebsiteKind/ExternalId)
- `api-core/tests/Diax.Tests/Customers/MxCacheEntryTests.cs` - 6 testes (normalização, Refresh, janelas de TTL)

## Decisions Made

- `MxCacheEntry.ResultCode` é `int`, não `MxCheckResult` — `Diax.Domain` não pode referenciar `Diax.Application` (regra estabelecida do projeto); a conversão int↔enum fica para o plano 07-04, que consome o cache.
- Todas as decisões D-04, D-06, D-07 (07-CONTEXT.md) seguidas exatamente: contadores como colunas `int` (não JSON em `ErrorDetails`); `WebsiteKind` nova coluna; `ExternalId` criado nesta fase mas sem lógica de dedup (isso é Phase 8 / IMPT-01).
- Nenhuma migration gerada — cumprindo D-07 (migration única no plano 07-07, coordenando `WebsiteKind` + `ExternalId` para evitar mais um ponto de ordenação de migrations além do risco já registrado do v1.2).

## Deviations from Plan

None — plan executado exatamente como escrito, usando os blocos de código exatos especificados em cada `<action>`.

## Issues Encountered

None.

## User Setup Required

None — nenhuma configuração externa necessária. Todas as mudanças são código de domínio + mapeamento EF, sem I/O de runtime novo (nada consome os campos/tabela ainda, conforme o objetivo do plano).

## Next Phase Readiness

- `Customer.WebsiteKind`/`ExternalId`, `CustomerImport.RecordRejectionCounts`, e `IMxCacheRepository` estão prontos para serem consumidos pelo plano 07-04 (wiring em `ExtractorIntegrationService.ImportLeadsAsync`) e 07-06.
- `MxCacheEntry.ResultCode` (int) precisa de uma função de conversão explícita para/de `MxCheckResult` no consumidor da camada Application (07-04) — não existe ainda, é trabalho do próximo plano.
- O plano 07-07 deve gerar a migration única cobrindo: `customers.website_kind`, `customers.external_id` + índice, `customer_imports` (4 colunas), `mx_cache_entries` (tabela nova + 2 índices). Confirmado: nenhuma migration foi criada por este plano.
- Suíte completa verde (796 testes, 0 falhas) em Release — nenhum blocker.

---
*Phase: 07-extra-o-qualidade-na-entrada*
*Completed: 2026-09-06*

## Self-Check: PASSED

All 6 created files verified present on disk (MxCacheEntry.cs, IMxCacheRepository.cs, MxCacheRepository.cs, MxCacheEntryConfiguration.cs, CustomerImportCountersTests.cs, MxCacheEntryTests.cs). All 3 task commits (`8585fee`, `537be06`, `33bbeb5`) verified in git log. Full test suite re-verified green (796 passed, 0 failed) after all three tasks. `git status --short` on `Data/Migrations/` confirmed empty (zero new migration files).
