---
phase: 8
slug: import-dedup-e-score-em-tempo-real
status: draft
nyquist_compliant: false
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
- **Before `/gsd:verify-work`:** Full suite green (baseline 839 tests as of PR #104)
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| TBD | 01 | 1 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_UpdatesEmail" -c Release` | ❌ new `[Fact]` in existing file | ⬜ pending |
| TBD | 01 | 1 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_PreservesEmailOptOut" -c Release` | ❌ new `[Fact]` | ⬜ pending |
| TBD | 01 | 1 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~ExternalIdAndEmailMatchDifferentCustomers" -c Release` | ❌ new `[Fact]` | ⬜ pending |
| TBD | 01 | 1 | IMPT-01 | unit | `dotnet test --filter "FullyQualifiedName~BackfillsExternalIdOnExistingCustomer" -c Release` | ❌ new `[Fact]` | ⬜ pending |
| TBD | 01 | 1 | IMPT-02 | unit | `dotnet test --filter "FullyQualifiedName~Scraping_DoublePull_SameExternalId" -c Release` | ❌ new `[Fact]`, extends `CustomerImportServiceTests.cs:434` | ⬜ pending |
| TBD | 02 | 2 | IMPT-03 | unit | **TBD — blocked on scope decision (see below)** | ❌ not designed | ⛔ blocked |

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

## Blocking Scope Decision (IMPT-03)

`LeadScoringService.CalculateScore` (`LeadScoringService.cs:111`) caps the fit-only subscore at **25**, and `WarmThreshold` is **30**. At import time engagement is null and Status is a fresh `Lead`, so a synchronously-scored lead is **mathematically always Cold**. Calling the existing function at import satisfies the letter of IMPT-03 (score is no longer zero) but delivers no segmentation value.

`08-CONTEXT.md` instructs the planner to stop and ask the user rather than pick a reading. Planning for IMPT-03 is blocked until that answer arrives; IMPT-01 and IMPT-02 are unblocked and can be planned and executed independently.

---

## Validation Sign-Off

- [x] Wave 0 covers all MISSING references (none needed)
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [ ] All tasks have `<automated>` verify — blocked for IMPT-03 pending scope decision
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
