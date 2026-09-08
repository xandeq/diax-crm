---
phase: 08-import-—-dedup-e-score-em-tempo-real
plan: 01
subsystem: api
tags: [ef-core, repository-pattern, dedup, customer-import]

# Dependency graph
requires:
  - phase: 07-extra-o-qualidade-na-entrada
    provides: "Customer.ExternalId coluna + SetExternalId(), indice unico filtrado IX_Customers_ExternalId (migration 20260906101839_AddLeadQualitySignals)"
provides:
  - "ICustomerRepository.GetByExternalIdAsync (contrato) + implementacao EF Core em CustomerRepository"
  - "ImportCustomerRow.ExternalId como ultimo parametro posicional nullable (nao quebra ~30 chamadores existentes)"
  - "ExtractorIntegrationService.MapToImportRow propagando lead.Id.ToString() como ExternalId"
affects: [08-03-dedup-por-externalid, 08-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GetByExternalIdAsync espelha GetByEmailAsync/GetByPhoneAsync: Trim() + FirstOrDefaultAsync, normalizacao alinhada com Customer.SetExternalId"

key-files:
  created:
    - api-core/tests/Diax.Tests/Customers/CustomerRepositoryExternalIdTests.cs
  modified:
    - api-core/src/Diax.Domain/Customers/ICustomerRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs
    - api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs
    - api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs

key-decisions:
  - "ExternalId adicionado como ULTIMO parametro posicional de ImportCustomerRow para nao quebrar chamadores posicionais existentes (ExtractorIntegrationService, ApifyIntegrationService, ~30 testes)"
  - "GetByExternalIdAsync normaliza com Trim() espelhando Customer.SetExternalId, que sempre grava trimado"
  - "Zero migration criada - coluna e indice ja aplicados em producao na Phase 7"

patterns-established:
  - "Contratos de dedup entram em plano separado (interface-first), rodando em paralelo com a logica de negocio que os consome (08-02)"

requirements-completed: [IMPT-01, IMPT-02]

duration: 18min
completed: 2026-09-07
---

# Phase 8 Plan 1: Contratos de Dedup por ExternalId Summary

**`ICustomerRepository.GetByExternalIdAsync` + `ImportCustomerRow.ExternalId` fiando `lead.Id` do Extrator até o ponto de dedup, sem lógica de negócio nova.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-09-07T21:42:00Z (aprox.)
- **Completed:** 2026-09-07T22:00:41Z
- **Tasks:** 2
- **Files modified:** 5 (4 modificados, 1 criado)

## Accomplishments
- `ICustomerRepository.GetByExternalIdAsync` + implementação EF Core em `CustomerRepository`, espelhando `GetByEmailAsync`/`GetByPhoneAsync`
- `ImportCustomerRow.ExternalId` (último parâmetro posicional, nullable, default `null`) — zero chamadores posicionais quebrados
- `ExtractorIntegrationService.MapToImportRow` agora propaga `lead.Id.ToString()` como `ExternalId` (mantendo a nota humana em `Notes`)
- 4 testes InMemory cobrindo hit, miss, trim de input e convivência de múltiplos `ExternalId` nulos

## Task Commits

Each task was committed atomically:

1. **Task 1: Contratos — GetByExternalIdAsync, ImportCustomerRow.ExternalId e fiação do MapToImportRow** - `8ebc392` (feat)
2. **Task 2: Teste InMemory de GetByExternalIdAsync** - `9aeab39` (test)

**Plan metadata:** (this commit) `docs(08-01): complete plan`

## Files Created/Modified
- `api-core/src/Diax.Domain/Customers/ICustomerRepository.cs` - adiciona contrato `GetByExternalIdAsync`
- `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs` - implementação EF Core (`Trim()` + `FirstOrDefaultAsync` sobre `ExternalId`)
- `api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs` - `ImportCustomerRow.ExternalId` como último parâmetro
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` - `MapToImportRow` propaga `lead.Id`; comentário de IDEMPOTÊNCIA atualizado (referência obsoleta a "migração em prod" removida)
- `api-core/tests/Diax.Tests/Customers/CustomerRepositoryExternalIdTests.cs` - 4 testes xUnit puro (sem FluentAssertions)

## Decisions Made
- `ExternalId` inserido como último parâmetro posicional do record — qualquer outra posição quebraria compilação em ~30 chamadores existentes (decisão já prescrita no plano, seguida à risca)
- Normalização por `Trim()` no repositório espelha `Customer.SetExternalId`, que sempre grava o valor trimado — evita mismatch de busca por espaços incidentais
- Nenhuma migration criada — coluna `customers.external_id` e índice único filtrado `IX_Customers_ExternalId` já aplicados em produção pela Phase 7

## Deviations from Plan

None - plan executado exatamente como escrito.

## Issues Encountered
- Execução paralela ao plano 08-02 (mesma working tree): dois erros transientes observados durante `dotnet build`/`dotnet test` — (1) `CS0103: SegmentForScore` porque o agente 08-02 estava mid-edit em `LeadScoringService.cs` (arquivo fora do escopo deste plano, não tocado); (2) `CS2012` de DLL de teste travada por build concorrente do outro agente. Ambos resolvidos com um simples retry, sem qualquer alteração de código — não são deviations deste plano, apenas fricção esperada de dois executores simultâneos no mesmo `api-core`.
- Usings do bloco `<interfaces>` do plano continham `Diax.Application.Common` para `ICurrentUserService`; o namespace real é `Diax.Domain.Common`. Corrigido no arquivo de teste durante a escrita (nenhuma mudança de comportamento, apenas import correto).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `GetByExternalIdAsync`, `ImportCustomerRow.ExternalId` e a fiação de `lead.Id` estão prontos para o plano 08-03 (dedup real por ExternalId em `CustomerImportService`, decisões D-01..D-04)
- Suíte completa verde (882 testes no momento desta execução, incluindo trabalho concorrente do plano 08-02 na mesma árvore; a fatia deste plano contribuiu +4 sobre o baseline de 868)
- Nenhum blocker

---
*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Completed: 2026-09-07*

## Self-Check: PASSED

All created/modified files verified present on disk; commits `8ebc392` and `9aeab39` verified present in `git log`.
