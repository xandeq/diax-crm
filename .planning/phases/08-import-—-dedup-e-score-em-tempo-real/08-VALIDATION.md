---
phase: 8
slug: import-dedup-e-score-em-tempo-real
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-09-07
---

# Phase 8 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Derived from `08-RESEARCH.md` § Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.2 + Moq 4.20.72 — **no FluentAssertions** (`Should()` does not compile) |
| **Config file** | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| **Quick run command** | `cd api-core && dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release` |
| **Full suite command** | `cd api-core && dotnet test -c Release` |
| **Estimated runtime** | ~25s quick · ~2m30s full |

> `-c Release` is mandatory. Debug test DLLs are blocked by Smart App Control (`0x800711C7`).

---

## Sampling Rate

- **After every task commit:** Run the quick command
- **After every plan wave:** Run the full suite
- **Before `/gsd:verify-work`:** Full suite green (baseline **868 tests**, measured 2026-09-07 after PRs #106/#107/#109)
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 08-01 T1 | 01 | 1 | IMPT-01, IMPT-02 | build | `dotnet build -c Release` | n/a | ⬜ pending |
| 08-01 T2 | 01 | 1 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~CustomerRepositoryExternalIdTests" -c Release` | ❌ novo arquivo | ⬜ pending |
| 08-02 T1 | 02 | 1 | IMPT-03 | unit | `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests" -c Release` | ✅ arquivo existe (12 testes que NAO podem quebrar) | ⬜ pending |
| 08-02 T2 | 02 | 1 | IMPT-03 | unit | `dotnet test --filter "FullyQualifiedName~SegmentForScore_MapsThresholdsExactly" -c Release` | ❌ novos [Fact]/[Theory] | ⬜ pending |
| 08-03 T1 | 03 | 2 | IMPT-01, IMPT-02 | unit | `dotnet test --filter "FullyQualifiedName~ExtractorIntegrationServiceTests" -c Release` | ✅ regressao (ctor do SUT muda) | ⬜ pending |
| 08-03 T2 | 03 | 2 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_PreservesEmailOptOut" -c Release` | ❌ 2 novos [Fact] | ⬜ pending |
| 08-03 T3 | 03 | 2 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_UpdatesEmail" -c Release` | ❌ novo [Fact] | ⬜ pending |
| 08-03 T3 | 03 | 2 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~ExternalIdAndEmailMatchDifferentCustomers" -c Release` | ❌ novo [Fact] | ⬜ pending |
| 08-03 T3 | 03 | 2 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~BackfillsExternalIdOnExistingCustomer" -c Release` | ❌ novo [Fact] | ⬜ pending |
| 08-03 T3 | 03 | 2 | IMPT-02 | unit | `dotnet test --filter "FullyQualifiedName~Scraping_DoublePull_SameExternalId" -c Release` | ❌ novo [Fact], estende `CustomerImportServiceTests.cs:434` | ⬜ pending |
| 08-04 T1 | 04 | 3 | IMPT-03 | unit | `dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release` | ✅ regressao | ⬜ pending |
| 08-04 T2 | 04 | 3 | IMPT-03 | unit | `dotnet test --filter "FullyQualifiedName~Import_NewCustomer_GetsLeadScoreAndSegmentAtImport" -c Release` | ❌ novo [Fact] | ⬜ pending |
| 08-04 T2 | 04 | 3 | IMPT-03 | unit | `dotnet test --filter "FullyQualifiedName~Import_ExistingCustomer_LeadScoreNotRecomputedOnEnrich" -c Release` | ❌ novo [Fact] | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · ⛔ blocked*

---

## Wave 0 Requirements

None. Existing infrastructure covers the phase:
- `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` already exists with the mock-based constructor pattern
- The double-pull idempotency test (`Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport`, line 434) is already green and serves as the template
- No new framework, fixture, or config file needed — only new `[Fact]` methods plus a Moq setup for the new `GetByExternalIdAsync`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Real Extrator pull dedups by `ExternalId` against live production rows | IMPT-01 / IMPT-02 | Requires the live Extrator backend and the production DB; unit tests use mocks | After deploy, trigger `ExtractorPullWorker` twice; confirm the second run reports 0 created and N updated in `customer_imports`, and that `SELECT COUNT(*) FROM customers WHERE external_id IS NOT NULL` grows only on the first run |
| Filtered unique index tolerates many NULLs in production | IMPT-01 | Index was applied in Phase 7; behavior is a DB engine guarantee, not app logic | Confirm `IX_Customers_ExternalId` exists with `filter = [external_id] IS NOT NULL` in the production schema |

---

## Scope Decision (IMPT-03) — RESOLVIDA em 2026-09-07

`LeadScoringService.CalculateScore` (`LeadScoringService.cs:111`) limitava o subscore de fit a **25**
contra um `WarmThreshold` de **30**, o que tornava qualquer score calculado no import
**matematicamente sempre Cold**. A questão foi levada ao usuário e respondida (D-05 em
`08-CONTEXT.md`): **recalibrar o fit com os sinais da Phase 7 e subir o teto**, para que um lead de
boa qualidade nasça `Warm` já no import. Alternativas rejeitadas: "só chamar a função atual"
(entregaria `Cold` constante) e "adiar IMPT-03".

Implementação planejada: o teto do bloco de fit sobe de 25 para **45**, com três sinais novos
(`WebsiteKind.OwnSite` +10, `Quality.High` +5, `EmailType.PersonalDirect` +5) e uma penalidade
(`HasSuspiciousDomain` −15). Como `45 < HotThreshold (60)`, `Hot` permanece inalcançável sem
engajamento — intencional, não é para "consertar". A recalibração acontece DENTRO de
`CalculateScore` (fonte única, nenhuma cópia paralela no caminho de import); o import chama
`CalculateScore` e o novo helper `SegmentForScore` como invocações estáticas, de modo que o
`LeadScoringWorker` das 06:00 BRT e o import produzem o mesmo score para o mesmo `Customer`.

Aritmética do split (engajamento null, Status `Lead`):
- forte (site próprio + DDD 27 + elegível + Quality High + e-mail direto) = 5+5+5+10+10+5+5 = **45** ⇒ Warm
- fraco (diretório + DDD 27 + elegível + Quality Medium + e-mail genérico) = 5+5+5+10 = **25** ⇒ Cold
- domínio suspeito (⇒ não elegível) = 5+5+0+10+10+5+5−15 = **25** ⇒ Cold

Planos: **08-02** (recalibração + `SegmentForScore`) e **08-04** (chamada no ramo de criação do
import). IMPT-03 não está mais bloqueado.

---

## Validation Sign-Off

- [x] Wave 0 covers all MISSING references (none needed)
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] All tasks have `<automated>` verify — IMPT-03 desbloqueado pela D-05
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** aprovado em 2026-09-07 (planejamento da fase concluído: 4 planos, 3 waves)
