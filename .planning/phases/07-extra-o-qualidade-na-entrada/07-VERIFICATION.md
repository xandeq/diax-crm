---
phase: 07-extra-o-qualidade-na-entrada
verified: 2026-09-07T15:59:21Z
status: passed
score: 7/7 must-haves verified
---

# Phase 7: Extração — Qualidade na Entrada Verification Report

**Phase Goal:** Leads de baixa qualidade são barrados ANTES de virarem `Customer`, com motivo de
rejeição registrado e consultável, e o sinal "site próprio vs diretório de terceiro" calculado
para uso posterior no score.
**Verified:** 2026-09-07T15:59:21Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Lead com domínio de e-mail sem MX/A é rejeitado pelo worker .NET antes do import | ✓ VERIFIED | `ExtractorIntegrationService.cs:169-173` — `if (mx == MxCheckResult.NoMx) { rejectedNoMx++; continue; }`, antes de `allLeads.Add`, antes da chamada a `ImportAsync` |
| 2 | Timeout/erro de servidor DNS (SERVFAIL/NotImplemented/Refused) NUNCA rejeita o lead (D-02) | ✓ VERIFIED | `MxResponseInterpreter.cs` — só rcode 3 (NXDOMAIN) vira `NoMx`; rcodes 2/4/5 e timeout (`DnsResponseException.ConnectionTimeout` em `DnsClientMxLookupService.cs:59`) viram `Unverified`. No loop do `ExtractorIntegrationService`, `Unverified` só incrementa `mxUnverified` (linha 176) e cai em `allLeads.Add` (linha 179) — nunca em `continue` |
| 3 | Domínio-lixo/placeholder é rejeitado SEM query DNS | ✓ VERIFIED | `CachedMxCheckService.cs:75-76` — `JunkDomainFilter.IsJunk(d)` checado ANTES de `GetByDomainsAsync`/lookup; teste `CachedMxCheckServiceTests` verifica `Times.Never` na chamada ao lookup |
| 4 | Motivo de rejeição (geo/e-mail lixo/sem MX/duplicado) é registrado por rodada de import | ✓ VERIFIED | `CustomerImportService.cs:701-705` chama `import.RecordRejectionCounts(geo, lowQualityEmail, noMx, duplicate: duplicateCount)` antes de `import.Complete(...)`; `CustomerImport.cs` tem os 4 contadores com setter privado |
| 5 | Motivo de rejeição é consultável por período via API | ✓ VERIFIED | `CustomersController.cs:384-397` — `GET /api/v1/customers/imports` com `[FromQuery] DateTime? from/to`; `CustomerImportRepository.GetPagedAsync` filtra por `CreatedAt >= from` / `<= to`; `ImportHistoryResponse` expõe os 4 contadores |
| 6 | `website` do lead é classificado site-próprio vs diretório e persistido | ✓ VERIFIED | `CustomerImportService.cs:481` (`customer.SetWebsiteKind(WebsiteClassifier.Classify(row.Website))` no create) e linha 593 (recálculo no enriquecimento); `Customer.WebsiteKind` mapeado em `CustomerConfiguration.cs`, coluna `website_kind` na migration |
| 7 | O website chega ao Customer (não fica só em texto de Notes) | ✓ VERIFIED | `ExtractorIntegrationService.MapToImportRow` inclui `Website: lead.Website` no `ImportCustomerRow` (linha ~ver grep), preservando também a nota humana `"Website: {lead.Website}"` |

**Score:** 7/7 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Diax.Domain/Customers/Enums/WebsiteKind.cs` | enum Unknown/OwnSite/Directory | ✓ VERIFIED | Existe, exatamente como especificado |
| `Diax.Application/Customers/WebsiteClassification/WebsiteClassifier.cs` | Porta 1:1 de classify_url | ✓ VERIFIED | 41 DirectoryHosts verbatim, `catch (UriFormatException)`, testado |
| `Diax.Application/Customers/Services/JunkDomainFilter.cs` | Porta 1:1 de is_junk_domain | ✓ VERIFIED | JunkHosts/JunkSuffixes/JunkParts presentes, `instagram.local` pego via sufixo `.local` (sem entrada dedicada) |
| `Diax.Application/Customers/Services/IMxLookupService.cs` | enum MxCheckResult + interface | ✓ VERIFIED | Valid=0/NoMx=1/Unverified=2, `CheckAsync` |
| `Diax.Infrastructure/Dns/MxResponseInterpreter.cs` | Função pura Valid/NoMx/Unverified/fallback | ✓ VERIFIED | `IsTransientServerFailure` cobre 2/4/5; NXDOMAIN=3 isolado |
| `Diax.Infrastructure/Dns/DnsClientMxLookupService.cs` | Implementação DnsClient.NET | ✓ VERIFIED | `catch (DnsResponseException ex) when (ex.Code == DnsResponseCode.ConnectionTimeout)` → Unverified |
| `Diax.Domain/Customers/MxCacheEntry.cs` + `IMxCacheRepository.cs` | Cache persistente de MX | ✓ VERIFIED | `IsFresh`, `Refresh`, `Normalize`; sem `using Diax.Application` (Domain isolado) |
| `Diax.Application/Customers/Services/CachedMxCheckService.cs` | Cache + short-circuit junk + paralelismo | ✓ VERIFIED | 146 linhas, `JunkDomainFilter.IsJunk` antes do cache, `Parallel.ForEachAsync` |
| `Diax.Domain/Customers/CustomerImport.cs` | 4 contadores + `RecordRejectionCounts` | ✓ VERIFIED | `GeoRejectedCount`/`LowQualityEmailRejectedCount`/`NoMxRejectedCount`/`DuplicateRejectedCount`, setters privados |
| `Diax.Domain/Customers/Customer.cs` | `WebsiteKind`/`ExternalId` + setters de domínio | ✓ VERIFIED | `SetWebsiteKind`, `SetExternalId` (whitespace→null) |
| `Diax.Infrastructure/Data/Configurations/MxCacheEntryConfiguration.cs` | Tabela + índice único | ✓ VERIFIED | `ToTable("mx_cache_entries")`, `IX_MxCacheEntries_Domain` |
| `Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs` | Índice único filtrado em external_id | ✓ VERIFIED | `IX_Customers_ExternalId`, `HasFilter("[external_id] IS NOT NULL")` |
| Migration `20260906101839_AddLeadQualitySignals` | Migration única aditiva | ✓ VERIFIED | 6 AddColumn, 1 CreateTable, 3 CreateIndex no `Up()`; drops só no `Down()`; aplicada em produção (confirmado via 07-07-SUMMARY.md + auditoria de código) |
| `CustomersController.cs` — `GET /imports?from&to` | Query filtrável | ✓ VERIFIED | `[FromQuery] DateTime? from/to`, default null preserva comportamento antigo |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `ExtractorIntegrationService` | `ICachedMxCheckService` | injeção + `CheckManyAsync` no loop | ✓ WIRED | Construtor recebe `ICachedMxCheckService mxCheck`; chamado após paginação, resultado usado no filtro |
| `CachedMxCheckService` | `IMxCacheRepository` | `GetByDomainsAsync`/`AddAsync`/`UpdateAsync` | ✓ WIRED | Presente e coberto por testes de cache fresco/expirado |
| `CachedMxCheckService` | `JunkDomainFilter` | short-circuit antes de qualquer DNS | ✓ WIRED | `IsJunk` checado antes de `GetByDomainsAsync` |
| `ExtractorIntegrationService` | `MxCheckResult.NoMx` (rejeição) vs `.Unverified` (passa) | `continue` só em NoMx | ✓ WIRED — propriedade de segurança D-02 confirmada | Ver evidência da Truth #2 |
| `DependencyInjection.cs` (Infra) | `IMxLookupService`/`ILookupClient` | `AddSingleton<ILookupClient>` + `AddScoped<IMxLookupService, DnsClientMxLookupService>` | ✓ WIRED | `ThrowDnsErrors = false` confirmado (necessário para distinguir NXDOMAIN de timeout) |
| `CustomerImportService` | `CustomerImport.RecordRejectionCounts` | chamado antes de `import.Complete` | ✓ WIRED | Linha 701 antes de `Complete` |
| `CustomerImportService` | `WebsiteClassifier.Classify` | `customer.SetWebsiteKind` no create e no enriquecimento | ✓ WIRED | 2 ocorrências (linhas 481 e 593) |
| `CustomersController` | `CustomerImportService.GetImportHistoryAsync(page,pageSize,from,to,ct)` | `[FromQuery]` | ✓ WIRED | Assinatura propagada até o repositório |
| `CustomerImportRepository` | `customer_imports.created_at` | `Where(x => x.CreatedAt >= from.Value)` / `<= to.Value` | ✓ WIRED | Confirmado no código |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| EXTR-01 | 07-01, 07-02, 07-04 | Sistema rejeita lead sem MX/A antes de importar | ✓ SATISFIED | Wiring completo em `ExtractorIntegrationService` + `CachedMxCheckService` + `DnsClientMxLookupService`; D-02 confirmada no código (não só documentada) |
| EXTR-02 | 07-03, 07-05, 07-06 | Motivo de rejeição registrado e consultável por período | ✓ SATISFIED | 4 contadores persistidos em `CustomerImport`; endpoint `GET /customers/imports?from&to` filtra e expõe os contadores |
| EXTR-03 | 07-01, 07-03, 07-05 | `website` classificado site-próprio vs diretório, persistido, usável no score | ✓ SATISFIED | `WebsiteClassifier` + `Customer.WebsiteKind` persistido no create/enriquecimento; website chega ao `Customer.Website` (não só Notes) |

Nenhum requisito órfão encontrado — REQUIREMENTS.md mapeia só EXTR-01/02/03 para Phase 7 e todos os
3 aparecem no `requirements:` de pelo menos um plano.

### Anti-Patterns Found

Nenhum bloqueador ou aviso encontrado nos arquivos modificados pela fase. As únicas ocorrências de
"placeholder" são de linguagem de domínio (valores placeholder de website/e-mail vazio como
`'-'`, `'n/a'`), não código stub.

### Scope Boundary Check

Confirmado por grep que o código da fase NÃO contém:
- Lógica de dedup por `ExternalId` (`ExtractorIntegrationService.cs` só comenta a coluna como
  "recomendação para a Phase 8", sem uso funcional)
- Cálculo de `lead_score`/`LeadScoringWorker` em `CustomerImportService.cs` ou
  `ExtractorIntegrationService.cs`

Isso está de acordo com o escopo da fase (EXTR-01/02/03), deixando IMPT-01/02/03 para Phase 8.

### Human Verification Required

Nenhum item pendente de verificação humana — a checagem de infraestrutura DNS real (se
UDP/53 funciona no SmarterASP) é smoke manual pós-deploy documentado no próprio plano 07-04/07-07
e não bloqueia o veredito desta fase, pois a decisão D-02 garante que o pior cenário (DNS
bloqueado) degrada com segurança para `Unverified` sem perder leads.

### Gaps Summary

Nenhum gap encontrado. Todos os must_haves dos 7 planos (`07-01` a `07-07`) foram verificados
diretamente no código-fonte:

- Build `dotnet build -c Release`: 0 erros
- Suíte completa `dotnet test -c Release --no-build`: **833/833** passando (baseline pré-fase:
  707 — 126 testes novos desta fase, batendo com o número no 07-07-SUMMARY.md)
- 192 testes específicos da fase (WebsiteClassifier/JunkDomainFilter/MxResponseInterpreter/
  CachedMxCheckService/ExtractorIntegrationService/CustomerImportService/ImportHistoryQuery/
  CustomerImportCounters/MxCacheEntry) passando isoladamente
- Migration `20260906101839_AddLeadQualitySignals` aditiva, aplicada em produção, colunas/tabela
  confirmadas (conforme contexto de verificação fornecido e corroborado pelo `Up()` do arquivo)
- Propriedade de segurança crítica D-02 confirmada em código real (não apenas em comentário):
  `MxResponseInterpreter.IsTransientServerFailure` isola rcodes 2/4/5 de NXDOMAIN (3); o loop de
  `ExtractorIntegrationService` só executa `continue` (rejeição) para `MxCheckResult.NoMx`, nunca
  para `Unverified`
- Fora de escopo (dedup por ExternalId, score no import) corretamente ausente do código desta fase

---

_Verified: 2026-09-07T15:59:21Z_
_Verifier: Claude (gsd-verifier)_
