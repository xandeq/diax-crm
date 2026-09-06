---
phase: 07-extra-o-qualidade-na-entrada
plan: 04
subsystem: api
tags: [csharp, mx-check, dns-cache, extractor-import, parallel-dns, xunit]

# Dependency graph
requires: ["07-02", "07-03"]
provides:
  - "ICachedMxCheckService (Diax.Application.Customers.Services) — batch MX resolution com pre-filtro de junk-domain, cache persistente por TTL (30d Valid/NoMx, 24h Unverified) e resolucao DNS paralela (Parallel.ForEachAsync, grau configuravel)"
  - "ExtractorPullOptions.MxCheckEnabled/MxCacheDays/MxUnverifiedCacheHours/MxLookupParallelism"
  - "MxCacheEntry construtor de 3 args (domain, resultCode, checkedAtUtc) para TTL deterministico em teste"
  - "ExtractorIntegrationService.ImportLeadsAsync filtra MxCheckResult.NoMx em lote apos a paginacao, com contadores rejectedNoMx/mxUnverified e warning de infraestrutura (Unverified > 80%)"
affects: ["07-06 (contadores de rejeicao persistidos em CustomerImport)", "07-07 (migration unica que inclui mx_cache_entries)"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Resolucao de MX em LOTE apos a paginacao completa (nao por lead dentro do foreach) — acumula (row, lead) em candidates, extrai dominios distintos, uma unica chamada a CheckManyAsync, depois filtra; evita paralelismo por-request dentro de um loop sequencial"
    - "Mock default 'tudo Valid' configurado no construtor do fixture de teste (nao em cada [Fact]) para absorver uma nova dependencia de construtor sem editar dezenas de testes pre-existentes"

key-files:
  created:
    - api-core/src/Diax.Application/Customers/Services/CachedMxCheckService.cs
    - api-core/tests/Diax.Tests/Customers/CachedMxCheckServiceTests.cs
  modified:
    - api-core/src/Diax.Application/Customers/ExtractorPullOptions.cs
    - api-core/src/Diax.Application/DependencyInjection.cs
    - api-core/src/Diax.Domain/Customers/MxCacheEntry.cs
    - api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs
    - api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs

key-decisions:
  - "D-02 (locked, 07-CONTEXT.md) confirmada em codigo no loop de import: SOMENTE MxCheckResult.NoMx aciona o `continue` de rejeicao; Unverified so incrementa um contador e o lead SEGUE para allLeads — nunca invertido"
  - "MX check roda em LOTE apos a paginacao (nao por lead), reaproveitando cache + paralelismo configuravel (default 8, paridade com check_many(workers=8) do site_check.py)"
  - "IsLowQualityEmail ganha short-circuit de JunkDomainFilter.IsJunk(domain) — dominio-lixo conta no bucket 'e-mail lixo' (rejectedLowQuality), nunca chega a entrar na lista de dominios enviada ao CachedMxCheckService (custo zero de I/O, preserva os 4 buckets de D-04)"

requirements-completed: [EXTR-01]

# Metrics
duration: ~55min
completed: 2026-09-06
---

# Phase 07 Plan 04: CachedMxCheckService + Wiring no ExtractorIntegrationService Summary

**Checagem de MX em lote com cache persistente por domínio (TTL 30d/24h) e resolução DNS paralela plugada no loop de import do worker .NET — domínio sem MX/A agora é rejeitado ANTES de virar `Customer`, timeout de DNS nunca rejeita (D-02), e domínio-lixo tem custo zero de I/O.**

## Performance

- **Duration:** ~55 min
- **Completed:** 2026-09-06T09:53:00Z
- **Tasks:** 2 completed
- **Files modified:** 7 (2 created, 5 modified)

## Accomplishments

- `ICachedMxCheckService.CheckManyAsync(domains, ct)` — resolve um lote de domínios com 4 camadas de decisão: (1) domínio lixo → `NoMx` sem I/O, (2) cache fresco → sem I/O, (3) resolução DNS real com `Parallel.ForEachAsync` (grau configurável, default 8), (4) persiste o cache (inclusive `Unverified`, com TTL curto de 24h para não martelar o mesmo domínio problemático)
- `ExtractorPullOptions` ganha 4 novos toggles/TTLs: `MxCheckEnabled` (default true), `MxCacheDays` (30), `MxUnverifiedCacheHours` (24), `MxLookupParallelism` (8)
- `MxCacheEntry` ganha construtor de 3 args (`domain, resultCode, checkedAtUtc`) documentado como uso exclusivo de seeds/testes — permite forjar idade da entrada de cache para testar janelas de TTL deterministicamente
- `ExtractorIntegrationService.ImportLeadsAsync` reestruturado: paginação acumula `(row, lead)` em `candidates` em vez de `allLeads` direto; **depois** da paginação completa, extrai domínios distintos e faz UMA chamada em lote a `CheckManyAsync`, então filtra — `NoMx` rejeita (`rejectedNoMx++`, `continue`), `Unverified` PASSA (D-02, só `mxUnverified++`)
- Warning de infraestrutura (`ALERTA DE INFRAESTRUTURA`) quando `Unverified` > 80% da rodada — detecção precoce de DNS de saída bloqueado no SmarterASP sem depender de confirmação do provedor
- `IsLowQualityEmail` ganha short-circuit `JunkDomainFilter.IsJunk(domain)` antes do `return false` final — domínio placeholder (`instagram.local`, `wixpress.com`) rejeitado no bucket "e-mail lixo", nunca chega a integrar a lista de domínios do MX check
- 17 novos testes (10 em `CachedMxCheckServiceTests`, 7 em `ExtractorIntegrationServiceTests`), suíte completa: **813 testes passando** (baseline 796 + 17), 0 falhas, build limpo em Release

## Task Commits

Each task was committed atomically:

1. **Task 1: ExtractorPullOptions (toggles/TTL) + CachedMxCheckService** - `a6fcce5` (feat)
2. **Task 2: Wiring do filtro de MX no loop do ExtractorIntegrationService + instrumentação** - `f7cba5d` (feat)

**Plan metadata:** (this commit, see final_commit below)

## Files Created/Modified

- `api-core/src/Diax.Application/Customers/Services/CachedMxCheckService.cs` - `ICachedMxCheckService`/`CachedMxCheckService`, 4 camadas de decisão (junk → cache → DNS → persiste)
- `api-core/src/Diax.Application/Customers/ExtractorPullOptions.cs` - `MxCheckEnabled`/`MxCacheDays`/`MxUnverifiedCacheHours`/`MxLookupParallelism`
- `api-core/src/Diax.Application/DependencyInjection.cs` - registra `ICachedMxCheckService → CachedMxCheckService`
- `api-core/src/Diax.Domain/Customers/MxCacheEntry.cs` - construtor de 3 args (`checkedAtUtc`) para TTL determinístico em teste
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` - injeta `ICachedMxCheckService` (5º param do construtor), resolução de MX em lote pós-paginação, contadores `rejectedNoMx`/`mxUnverified`, warning de infraestrutura, `JunkDomainFilter.IsJunk` em `IsLowQualityEmail`, helper `ExtractDomain`
- `api-core/tests/Diax.Tests/Customers/CachedMxCheckServiceTests.cs` - 10 testes (junk sem lookup, cache fresco/expirado Valid e Unverified, dedupe de domínio repetido, `MxCheckEnabled=false`, lista vazia, persistência de `Unverified`)
- `api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs` - mock default "tudo Valid" no construtor do fixture + `CreateSut` atualizado (5º param); 7 novos fatos MX check; 3 fixtures pré-existentes com domínio colidindo com `JunkDomainFilter` corrigidas (ver Deviations)

## Decisions Made

- D-02 seguida exatamente como travada: `Unverified` nunca entra no bloco de `continue` de rejeição — só `NoMx` rejeita.
- MX check em lote (não por lead) foi mantido conforme a estrutura do plano — evita paralelismo aninhado dentro de um loop sequencial de paginação.
- `_options` completo (campo com `ExtractorPullOptions` inteiro) NÃO foi adicionado ao `ExtractorIntegrationService` — o plano oferecia isso como opcional ("se ainda não houver"), e nenhum código do wiring de fato precisa da options inteira (o `MxCheckEnabled` já é resolvido dentro do `CachedMxCheckService`); adicionar um campo não lido geraria apenas um warning de campo não usado.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixtures de teste pré-existentes colidiam com o novo `JunkDomainFilter.IsJunk` em `IsLowQualityEmail`**
- **Found during:** Task 2, ao rodar `ExtractorIntegrationServiceTests` pela primeira vez após adicionar o short-circuit de domínio-lixo — 20 de 57 testes falharam.
- **Issue:** `JunkDomainFilter.JunkHosts` (criado em 07-01, cópia verbatim de `mx_check.py`) inclui os literais `"empresa.com.br"`, `"email.com"` e `"test.com"` como domínios placeholder conhecidos. Esses mesmos literais eram usados como dados de fixture em ~17 testes pré-existentes de `ExtractorIntegrationServiceTests` (filtro geográfico, dedup, contactless-guard) — não porque estivessem testando domínio-lixo, mas porque pareciam nomes de domínio "genéricos" razoáveis para um teste. Antes desta task, `IsLowQualityEmail` não conhecia `JunkDomainFilter`, então a colisão nunca havia se manifestado.
- **Fix:** Trocados os literais de fixture por domínios equivalentes fora da lista de lixo: `empresa.com.br` → `acme.com.br` (11 ocorrências, `replace_all`), `com@email.com` → `com@correio.com.br`, `duplicate@test.com`/`new@test.com` → `duplicate@test.com.br`/`new@test.com.br` (o teste de dedup por e-mail já reaproveita a mesma string na configuração do mock `GetByEmailAsync`, então as duas pontas foram atualizadas juntas). Nenhuma asserção ou intenção de teste mudou — só o dado de fixture.
- **Files modified:** `api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs`
- **Commit:** `f7cba5d`

### Notas sobre acceptance criteria do plano (não são deviations funcionais)

- O grep `grep -c "ICachedMxCheckService" ExtractorIntegrationService.cs` do plano espera ≥ 3; o código real produz 2 (declaração do campo + parâmetro do construtor) porque todo uso subsequente é via a variável `_mxCheck`, não repetindo o nome do tipo. Comportamento correto e coberto por teste; é só uma heurística de grep do plano que não bateu com o padrão de código idiomático (campo tipado + injeção, sem repetir o nome da interface no corpo do método).

## Issues Encountered

Nenhum blocker. O único obstáculo (fixtures colidindo com `JunkDomainFilter`) foi identificado e corrigido no mesmo ciclo de execução da Task 2, sem necessidade de retrabalho posterior.

## User Setup Required

None — nenhuma configuração externa necessária. Nenhuma migration foi gerada (conforme constraint do plano — migration única sai no 07-07).

## Next Phase Readiness

- EXTR-01 está funcionalmente completo: lead com domínio sem MX/A é rejeitado pelo worker .NET antes do import; timeout de DNS passa e é contado como não verificado (D-02); domínio-lixo é rejeitado sem query DNS; cache persistente evita re-consultar o mesmo domínio dentro do TTL.
- Os contadores `rejectedNoMx`/`mxUnverified` hoje só viram `_logger.LogInformation`/`LogWarning` — não persistem em `CustomerImport` ainda. Isso é o gap explícito do plano **07-06** (EXTR-02, D-04), que já tem `CustomerImport.RecordRejectionCounts(...)` pronto desde o 07-03 para receber exatamente esses valores.
- `IMxCacheRepository`/`MxCacheEntry` seguem sem tabela física no banco (nenhuma migration gerada nesta fase, por D-07) — o cache funciona apenas quando o plano **07-07** aplicar a migration única coordenando `mx_cache_entries` + `WebsiteKind` + `ExternalId` + contadores.
- Suíte completa verde (813 testes, 0 falhas) em Release — nenhum blocker para o próximo plano da fase.

---
*Phase: 07-extra-o-qualidade-na-entrada*
*Completed: 2026-09-06*

## Self-Check: PASSED

All 7 created/modified files verified present on disk. Both task commits (`a6fcce5`, `f7cba5d`) verified in git log. Full test suite re-verified green (813 passed, 0 failed) after both tasks, `dotnet build -c Release` clean (0 errors).
