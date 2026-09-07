---
phase: 08-import-—-dedup-e-score-em-tempo-real
verified: 2026-09-07T23:30:00Z
status: passed
score: 4/4 success criteria verified
---

# Phase 8: Import — Dedup e Score em Tempo Real Verification Report

**Phase Goal:** O import do CRM deduplica de forma robusta por ID externo do Extrator (não só e-mail) e calcula o `lead_score` no momento em que o lead entra no sistema, sem esperar o job diário do `LeadScoringWorker` (06h BRT).

**Verified:** 2026-09-07
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (Success Criteria from ROADMAP.md)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `Customer.ExternalId` + índice único; reimport com e-mail trocado atualiza in-place | ✓ VERIFIED | `Customer.ExternalId`/`SetExternalId` (Phase 7, `Customer.cs:298-301`); `ICustomerRepository.GetByExternalIdAsync` + EF impl (`CustomerRepository.cs:35-40`, `c.ExternalId == normalized`); resolution order ExternalId→email→phone in `CustomerImportService.cs:412-483`; D-01 email swap at `:668-677`; test `Import_ExternalIdMatch_UpdatesEmail_WhenEmailChanged` passes |
| 2 | `POST /customers/import` com `source=Scraping` deduplica de verdade, double-import não duplica | ✓ VERIFIED | Shared resolution path in `ImportAsync` used for all `LeadSource` values (validation-only block at lines 90-317 is gated to `Import`/`DryRun`, persistence loop at 328+ is universal); tests `Import_DoublePull_IsIdempotent_...` (pre-existing, still passes) and `Import_Scraping_DoublePull_SameExternalId_DifferentEmail_NoDuplicateCreated` (new) both green |
| 3 | Lead recém-importado já nasce com `lead_score` calculado (não fica null até 06h BRT) | ✓ VERIFIED | `CustomerImportService.cs:637-638` — `LeadScoringService.CalculateScore(customer, null, DateTime.UtcNow)` + `customer.UpdateSegmentation(...)` in the CREATE branch, called after `UpdateClassification`/`SetWebsiteKind` (so `WebsiteKind`/`Quality`/`EmailType`/`IsEligibleForCampaigns` are populated first); tests `Import_NewCustomer_GetsLeadScoreAndSegmentAtImport` (score 40, Warm) and `Import_NewCustomer_WeakSignals_IsBornCold` (score 20, Cold) pass |
| 4 | Nenhum `Customer` existente perde histórico ao ser atualizado via dedup por `ExternalId` | ✓ VERIFIED | Only one `new Customer(` in the entire file (CREATE branch, line 554); ExternalId-match path always resolves to `UpdateBasicInfo`/`UpdateContactInfo`/`SetExternalId`/`OptOutEmail` on the *tracked* `existingCustomer` instance, never delete+recreate; `UpdateBasicInfo` (`Customer.cs:256-268`) only touches `Name`/`Email`/`PersonType`/`CompanyName`/`Document` — does not reset `EmailOptOut`, `CreatedAt`, `Notes`, `Tags`, or timeline |

**Score:** 4/4 success criteria verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `api-core/src/Diax.Domain/Customers/ICustomerRepository.cs` | `GetByExternalIdAsync` contract | ✓ VERIFIED | Present, single declaration |
| `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs` | EF impl mirroring `GetByEmailAsync` | ✓ VERIFIED | `c.ExternalId == normalized` with `Trim()` normalization |
| `api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs` | `ImportCustomerRow.ExternalId` as last positional param | ✓ VERIFIED (not re-read in full, confirmed via consuming code + SUMMARY + test constructors using named `ExternalId:` arg) | |
| `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` | `MapToImportRow` propagates `lead.Id` | ✓ VERIFIED | `ExternalId: lead.Id > 0 ? lead.Id.ToString() : null` at line 380; stale "migração em prod" comment replaced with IMPT-01 note at line 345 |
| `api-core/src/Diax.Application/Customers/LeadScoringService.cs` | Recalibrated fit block (ceiling 45) + `SegmentForScore` | ✓ VERIFIED | Lines 114-166; `WebsiteKind.OwnSite`+10, `Quality.High`+5, `EmailType.PersonalDirect`+5, `HasSuspiciousDomain`-15; `SegmentForScore` public static, used by both `RecomputeAllAsync` (line 75) and `CustomerImportService` (line 638) |
| `api-core/src/Diax.Application/Customers/CustomerImportService.cs` | ExternalId→email→phone resolution + score-at-import | ✓ VERIFIED | Full resolution block lines 412-483 (match), 512-549 (D-01/D-02 guards), 637-638 (score), 709-718 (D-04 backfill) |
| Test files (4) | Coverage per plan | ✓ VERIFIED | `CustomerRepositoryExternalIdTests.cs` (4 facts), `LeadScoringServiceTests.cs` (4 facts + 1 theory of 6 cases added, 12 pre-existing untouched), `CustomerImportServiceTests.cs` (6 dedup facts + 3 scoring facts added, all pre-existing untouched), `ExtractorIntegrationServiceTests.cs` (logger arg updated) |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `ExtractorIntegrationService.MapToImportRow` | `ImportCustomerRow.ExternalId` | `lead.Id.ToString()` | ✓ WIRED | Confirmed at line 380 |
| `CustomerImportService` | `ICustomerRepository.GetByExternalIdAsync` | lookup before email/phone in persistence loop | ✓ WIRED | Line 431, precedes email/phone lookups at 448/463 |
| `CustomerImportService` | `IEmailSuppressionRepository.IsSuppressedAsync` | old-email check on swap (D-02) | ✓ WIRED | Line 531-534, gated by `isEmailSwap` |
| `CustomerImportService` | `Customer.SetExternalId` | create + backfill | ✓ WIRED | Line 566 (create), 716 (backfill); `grep -c "SetExternalId"` = 2 |
| `CustomerImportService` | `ILogger<CustomerImportService>` | `LogWarning` with `ExternalIdConflict` marker | ✓ WIRED | Line 476-478; verified by Moq `Times.Once` in test |
| `CustomerImportService` (CREATE) | `LeadScoringService.CalculateScore`/`SegmentForScore` | static invocation after `UpdateClassification`, before `AddAsync` | ✓ WIRED | Lines 637-638, correctly ordered |
| `LeadScoringService.RecomputeAllAsync` | `SegmentForScore` | replaces inline ternary | ✓ WIRED | Line 75; `grep -c "score >= HotThreshold"` = 1 (only inside the helper) |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| IMPT-01 | 08-01, 08-03 | Dedup por ID externo do Extrator | ✓ SATISFIED | `ExternalId` column + index (Phase 7), `GetByExternalIdAsync`, resolution order, D-01/D-03/D-04 logic and tests all present |
| IMPT-02 | 08-03 | `POST /customers/import` dedup real para `source=Scraping` | ✓ SATISFIED | Same resolution path used for all sources; `Import_Scraping_DoublePull_SameExternalId_DifferentEmail_NoDuplicateCreated` and pre-existing `Import_DoublePull_IsIdempotent_...` both green |
| IMPT-03 | 08-02, 08-04 | `lead_score` calculado no import, sem esperar 06h BRT | ✓ SATISFIED | `CalculateScore`/`UpdateSegmentation` called in CREATE branch (08-04); recalibration to ceiling 45 makes a strong lead reach Warm (08-02) |

No orphaned requirements — `.planning/REQUIREMENTS.md` maps only IMPT-01/02/03 to Phase 8, and all three appear in the plans' `requirements` frontmatter.

**Note (documentation staleness, not a code defect):** `.planning/REQUIREMENTS.md` line 20/68 still marks IMPT-03 as "In Progress (recalibração feita em 08-02, chamada no import pendente em 08-04)". Plan 08-04 has since been completed (commit `6fb3440`, confirmed in code and covered by tests) — this line is stale documentation that should be updated to "Complete" but does not reflect a functional gap.

### Anti-Patterns Found

None blocking. Two items reviewed and confirmed harmless per the audit brief:

1. **In-batch ExternalId duplicate (audited, not a gap).** When a single batch contains two rows with the same `ExternalId` but different emails, `seenExternalIdsInBatch.Add(rowExternalId)` (line 421) returns `false` on the second occurrence — the row is skipped (`skippedCount++`, error "Duplicata no lote", `continue`) *before* any DB query or `Customer` creation happens for it. This guard is a pure in-memory `HashSet` check, independent of whether `SaveChangesAsync` has run or whether the EF change tracker would surface the first row's uncommitted `AddAsync` — so there is no window where a duplicate could be created. The second row is simply rejected rather than merged; this is an intentional single-batch edge case, distinct from the cross-pull (multiple `ImportAsync` calls) scenario that IMPT-01 targets, which the D-01 tests do cover.

2. **Plan 08-04 acceptance-criteria contradiction (documentation inconsistency, not a defect).** Plan 08-04's Task 1 `<action>` block dictates a verbatim guard comment for the ENRICH branch that mentions `IEmailEventRepository` in prose (explaining why recompute is skipped there); the same plan's `<acceptance_criteria>` asserts `grep -q "IEmailEventRepository" ... é FALSO`. Independently confirmed: `grep -n "IEmailEventRepository" CustomerImportService.cs` returns exactly one hit, a `//` comment at line 650 ("só o `IEmailEventRepository` conhece") — no field, constructor parameter, or `using` directive was added for that type (`grep -q "LeadScoringService _"` and equivalent checks for a new dependency all come back false; the constructor still has its original 9 dependencies + `ILogger`). This is a self-contradictory plan instruction that the executor correctly resolved by following the authoritative `<action>` text and documenting the conflict in `08-04-SUMMARY.md` — not a code defect.

**Project rules compliance:**
- All test/build commands documented in the phase's plans and summaries use `-c Release`; no bare `dotnet test` invocation found in any command context (only prose mentions inside deviation notes).
- `grep -rn "Should()"` across all four modified/created test files returns zero matches — FluentAssertions is not used, consistent with the phase's project rule (xUnit `Assert` + Moq only).
- `git status --porcelain` on `api-core/src/Diax.Infrastructure/Data/Migrations/` is empty — no migration created, consistent with "Customer.ExternalId and its index already shipped in Phase 7."
- No `ExternalId` backfill script for the ~2300 pre-existing customers was written; explicitly deferred per D-04 (organic backfill via ongoing pulls), documented in `08-03-SUMMARY.md`.

### Human Verification Required

None. All success criteria are verifiable via code/test inspection; no UI, real-time, or external-service behavior is in scope for this phase.

### Gaps Summary

No gaps found. All 4 ROADMAP success criteria are verified against the actual codebase (not just SUMMARY claims):

- Dedup resolution order is ExternalId → email → phone, shared by both `Import` and `Scraping` sources, with D-01 (email swap), D-02 (compliance guard on old suppressed email, two layers), D-03 (email wins on conflict, logged), and D-04 (organic backfill) all implemented and each covered by a dedicated test with matching literal names.
- Score-at-import is wired correctly: computed only in the CREATE branch, after the two Phase-7-signal-populating calls (`SetWebsiteKind`, `UpdateClassification`) and before `AddAsync`; the ENRICH branch is explicitly guarded against recompute (verified: `UpdateSegmentation` appears exactly once in the file).
- The recalibrated fit block genuinely lets a realistic strong lead reach Warm (45, or as low as 30 without local-DDD) without engagement, while Hot (60) stays structurally unreachable by fit alone (max 45) — this is proven both by the arithmetic in `CalculateScore` and by passing tests with exact expected values.
- No history/timeline loss: single `Customer` constructor call site in the whole file, all match paths mutate the tracked existing entity in place, and `UpdateBasicInfo` is confirmed to leave `EmailOptOut`/`CreatedAt`/`Notes`/`Tags` untouched.

Full suite: 891 tests passing, 0 failing (up from 868 baseline), consistent with the orchestrator-verified state.

---

*Verified: 2026-09-07*
*Verifier: Claude (gsd-verifier)*
