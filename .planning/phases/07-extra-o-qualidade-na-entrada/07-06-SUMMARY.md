---
phase: 07-extra-o-qualidade-na-entrada
plan: 06
subsystem: api
tags: [csharp, ef-core, customer-import, xunit, pagination]

# Dependency graph
requires:
  - phase: "07-05"
    provides: "Os 4 contadores de rejeicao ja gravados em CustomerImport (GeoRejectedCount/LowQualityEmailRejectedCount/NoMxRejectedCount/DuplicateRejectedCount)"
provides:
  - "ICustomerImportRepository.GetPagedAsync com filtro opcional from/to (UTC, inclusivo)"
  - "ImportHistoryResponse expondo os 4 contadores de rejeicao ao consumidor da API"
  - "GET /api/v1/customers/imports?from=...&to=... filtra por CreatedAt via IX_CustomerImports_CreatedAt"
affects: ["07-07"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Parametros novos (from/to) inseridos ANTES do CancellationToken opcional em GetPagedAsync — quebra propositalmente qualquer chamador posicional desatualizado (unico chamador real, CustomerImportService, corrigido na mesma plan)"
    - "Teste de filtro de data com EF Core InMemory: CreatedAt (AuditableEntity, protected set, sempre DateTime.UtcNow no construtor) setado deterministicamente via db.Entry(x).Property(nameof(CustomerImport.CreatedAt)).CurrentValue = data — mecanismo padrao do EF Core change tracker, nao reflexao ad-hoc"

key-files:
  created:
    - api-core/tests/Diax.Tests/Customers/ImportHistoryQueryTests.cs
  modified:
    - api-core/src/Diax.Domain/Customers/ICustomerImportRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Repositories/CustomerImportRepository.cs
    - api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs
    - api-core/src/Diax.Application/Customers/CustomerImportService.cs
    - api-core/src/Diax.Api/Controllers/V1/CustomersController.cs

key-decisions:
  - "from/to tratados como UTC e inclusivos nas duas bordas (>= from, <= to) — combina com a convencao do DiaxDbContext de converter todo DateTime para UTC no save/read"
  - "As 4 propriedades novas de ImportHistoryResponse foram adicionadas NO FINAL do record, todas com default 0 — as 9 propriedades originais mantiveram a ordem posicional, nenhum chamador existente quebrou"
  - "Filtro de periodo testado com DiaxDbContext + UseInMemoryDatabase (nao o fallback de IQueryable puro sugerido no plano) — usar Entry(...).Property(...).CurrentValue resolveu a limitacao do CreatedAt com setter protegido sem precisar abrir mao de testar contra o repositorio real"

requirements-completed: [EXTR-02]

# Metrics
duration: ~20min
completed: 2026-09-06
---

# Phase 07 Plan 06: Filtro de período no histórico de imports + contadores de rejeição na API Summary

**`GET /api/v1/customers/imports?from=...&to=...` agora filtra por período (UTC, inclusivo) usando o índice `IX_CustomerImports_CreatedAt` existente, e `ImportHistoryResponse` expõe os 4 contadores de rejeição (geo/e-mail-lixo/sem-MX/duplicado) por rodada — fechando o requisito EXTR-02 ("motivo de rejeição consultável por período").**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-06T10:10:00Z
- **Completed:** 2026-09-06T10:12:11Z
- **Tasks:** 2
- **Files modified:** 6 (1 created, 5 modified)

## Accomplishments

- `ICustomerImportRepository.GetPagedAsync` ganhou os parâmetros opcionais `DateTime? from`/`DateTime? to` (posicionados antes do `CancellationToken`, ambos default `null`) — chamar sem eles mantém o comportamento anterior byte-a-byte.
- `CustomerImportRepository.GetPagedAsync` filtra via `Where(x => x.CreatedAt >= from.Value)` / `Where(x => x.CreatedAt <= to.Value)` antes de contar/paginar, então `TotalCount` reflete o total FILTRADO, e a ordenação continua `OrderByDescending(CreatedAt)`.
- `ImportHistoryResponse` ganhou `GeoRejectedCount`, `LowQualityEmailRejectedCount`, `NoMxRejectedCount`, `DuplicateRejectedCount` no final do record (default 0 cada), mapeados em `FromEntity` a partir da entidade `CustomerImport` (que já os persiste desde 07-05).
- `CustomerImportService.GetImportHistoryAsync` e `CustomersController.GetImportHistory` repassam `from`/`to` como `[FromQuery] DateTime?` até o repositório.
- 9 novos testes em `ImportHistoryQueryTests.cs`: 5 cobrindo o filtro de período (sem filtro, só `from`, só `to`, `from`+`to` no mesmo dia com bordas inclusivas, `TotalCount` filtrado ≠ total da tabela) e 4 cobrindo `FromEntity` (mapeamento dos 4 contadores, preservação das 9 propriedades originais, default zero sem `RecordRejectionCounts`, normalização de contadores negativos).
- Suíte completa: **833 testes passando** (baseline 824 + 9), 0 falhas, build limpo em Release, 0 warnings novos.

## Task Commits

Each task was committed atomically:

1. **Task 1: Filtro de período no repositório + ImportHistoryResponse com os 4 contadores** - `8067429` (feat)
2. **Task 2: Query params from/to no GET /api/v1/customers/imports** - `7e1d09b` (feat)

**Plan metadata:** (this commit, next)

## Files Created/Modified

- `api-core/src/Diax.Domain/Customers/ICustomerImportRepository.cs` - assinatura de `GetPagedAsync` com `from`/`to` opcionais
- `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerImportRepository.cs` - `Where` condicional por `CreatedAt`, `TotalCount` calculado sobre a query já filtrada
- `api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs` - `ImportHistoryResponse` com os 4 contadores novos + `FromEntity` atualizado
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` - `GetImportHistoryAsync` repassa `from`/`to` ao repositório
- `api-core/src/Diax.Api/Controllers/V1/CustomersController.cs` - `GetImportHistory` com `[FromQuery] DateTime? from`/`to`, log atualizado
- `api-core/tests/Diax.Tests/Customers/ImportHistoryQueryTests.cs` - 9 testes novos (filtro de período via EF Core InMemory + mapeamento de `FromEntity`)

## Decisions Made

- **`from`/`to` UTC e inclusivos nas duas bordas** — combina com a conversão automática de `DateTime` para UTC feita pelo `DiaxDbContext` (ValueConverter global) e evita ambiguidade sobre qual borda é exclusiva.
- **4 contadores novos no FINAL do record `ImportHistoryResponse`**, todos com default `0` — preserva a ordem posicional das 9 propriedades originais; nenhum chamador existente (nenhum foi encontrado além do `CustomerImportService`) precisou de ajuste por causa disso.
- **Teste do filtro de período usa `DiaxDbContext` real + `UseInMemoryDatabase`, não o fallback de `IQueryable` puro** que o plano oferecia como saída caso a instanciação do DbContext fosse complicada demais. `CreatedAt` (`AuditableEntity`) tem `protected set` e só é setado como `DateTime.UtcNow` no construtor, então usei `db.Entry(import).Property(nameof(CustomerImport.CreatedAt)).CurrentValue = data` para forçar datas determinísticas nos fixtures — esse é o mecanismo padrão do EF Core change tracker para popular propriedades com setter não-público, não uma reflexão ad-hoc de teste. Isso permitiu testar o repositório real (incluindo a tradução do `Where` para o provider InMemory) em vez de só a expressão de filtro isolada.

## Deviations from Plan

None — plano executado exatamente como escrito. A única divergência é a escolha documentada acima (usar `DiaxDbContext` real em vez do fallback de `IQueryable`), que o próprio plano já previa como opção preferencial ("Preferir `DiaxDbContext`... seguindo o padrão de construção já usado em outro teste do repo").

## Issues Encountered

Nenhum blocker. Ponto de atenção antecipado pelo plano (parâmetros novos antes do `CancellationToken` quebrariam o único chamador posicional existente) foi resolvido implementando as mudanças de `CustomerImportService`/`CustomersController` (Task 2) antes de rodar o build completo, garantindo que a suíte ficasse verde ao final — os commits de Task 1 e Task 2 continuam separados por arquivo, como especificado.

## User Setup Required

None — nenhuma configuração externa necessária. Nenhuma migration foi gerada (não era escopo deste plano; a migration única de `WebsiteKind`/`ExternalId`/contadores/`mx_cache_entries` segue prevista para 07-07, decisão D-07).

## Next Phase Readiness

- EXTR-02 está funcionalmente completo de ponta a ponta: os motivos de rejeição são gravados por rodada (07-05) E consultáveis por período via `GET /api/v1/customers/imports?from=...&to=...` (este plano).
- Chamar o endpoint sem `from`/`to` mantém o comportamento anterior — o frontend atual (que ignora campos extras do JSON) não quebra.
- Nenhuma coluna física dos contadores/`WebsiteKind`/`ExternalId`/`mx_cache_entries` existe ainda no banco de produção (migration ausente por decisão D-07) — 07-07 precisa gerar essa migration antes de qualquer um desses campos funcionar fora de testes com InMemory provider.
- Suíte completa verde (833 testes, 0 falhas) em Release — sem blocker para o próximo plano da fase (07-07).

---
*Phase: 07-extra-o-qualidade-na-entrada*
*Completed: 2026-09-06*

## Self-Check: PASSED

All 6 files created/modified in this plan verified present on disk (ICustomerImportRepository.cs, CustomerImportRepository.cs, BulkImportDtos.cs, CustomerImportService.cs, CustomersController.cs, ImportHistoryQueryTests.cs). Both task commits (`8067429`, `7e1d09b`) verified in git log. Full test suite re-verified green (833 passed, 0 failed) after both tasks, `dotnet build -c Release` clean (0 errors, 0 new warnings).
