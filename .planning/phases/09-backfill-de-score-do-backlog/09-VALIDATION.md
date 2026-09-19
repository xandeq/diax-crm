---
phase: 9
slug: backfill-de-score-do-backlog
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-09-19
---

# Phase 9 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (+ Moq for mocks) — `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| **Config file** | none dedicated — standard `dotnet test` via the csproj |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests|FullyQualifiedName~CustomerRepository" -c Release` |
| **Full suite command** | `dotnet test -c Release` |
| **Estimated runtime** | ~90 seconds |

Note: `CLAUDE.md` describes `Diax.UnitTests/`+`Diax.IntegrationTests/`; the real repo has a single
`tests/Diax.Tests/` project mixing both styles. Treat `CLAUDE.md`'s description as stale on this
point. Also: `LeadScoringServiceTests.cs` uses plain xUnit `Assert.*`, not FluentAssertions —
follow the file being extended, not the stale doc convention.

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests|FullyQualifiedName~CustomerRepository" -c Release`
- **After every plan wave:** Run `dotnet test -c Release` (full suite)
- **Before `/gsd:verify-work`:** Full suite must be green, PLUS the manual before/after `sales-dashboard` evidence captured in 09-02's `BACKFILL-EVIDENCE.md` (this phase's success criteria are partly about live production state, which no automated suite can assert)
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 09-01-01 | 01 | 1 | BFIL-01 | integration (EF InMemory) | `dotnet test --filter "FullyQualifiedName~CustomerRepository" -c Release` | ❌ W0 | ⬜ pending |
| 09-01-02 | 01 | 1 | BFIL-01 | manual/operational (script correctness) | `python scripts/backfill_lead_scores.py --dry-run` | ✅ | ⬜ pending |
| 09-02-01 | 02 | 2 | BFIL-01 | manual/operational (baseline capture) | `GET /api/v1/sales-dashboard` before trigger | ✅ | ⬜ pending |
| 09-02-02 | 02 | 2 | BFIL-01 | manual/operational (idempotency proof) | `GET /api/v1/sales-dashboard` after 2x trigger, diff | ✅ | ⬜ pending |
| 09-02-03 | 02 | 2 | BFIL-01 | checkpoint:human-verify | N/A — user reviews `BACKFILL-EVIDENCE.md` + created follow-up tasks | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

Pre-existing coverage (not new, but part of what proves BFIL-01 correct): `LeadScoringServiceTests.cs`
already has 14 test cases covering `CalculateScore`/`SegmentForScore` teto-45 thresholds and
`RecomputeAllAsync_SegmentsLeads_AndPersists` idempotency — no changes needed there.

---

## Wave 0 Requirements

- [ ] `api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs` — new EF-InMemory test asserting `GetAllLeadsAsync` excludes `Status = Inactive(5)` and `Status = Churned(6)`, following the `CustomerRepositoryExternalIdTests.cs` pattern. This is the one real coverage gap: the exclusion only exists implicitly in production code today.

No framework/tooling install needed — xUnit/Moq already present and referenced by the project.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| Backfill actually ran and scored the real production backlog | BFIL-01 | One-time operational change against live data — no automated suite can assert live DB state | 09-02: capture `GET /api/v1/sales-dashboard` before trigger → `backfill_baseline.json`; POST `/api/v1/pipeline/recompute-scores`; capture after → `backfill_after.json`; diff `unsegmentedOpenLeads` (must decrease/stay same, never increase) |
| Backfill is idempotent against real prod data | BFIL-01 | Idempotency under real DB state (not just mocked) can only be proven by actually re-running it twice | 09-02: trigger `recompute-scores` a second time, capture `backfill_after_rerun.json`, diff against `backfill_after.json` — scores/segments must be unchanged |
| Follow-up tasks created by `recompute-scores` side effect are sane | BFIL-01 | Side effect (`MaxFollowUpsPerRun=10`) touches user-facing task list — needs a human glance, not just a count assertion | 09-02 checkpoint: user reviews `followUpTasksCreated` count and a sample in `BACKFILL-EVIDENCE.md` before approving |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (09-02-03 is `checkpoint:human-verify`, correctly exempted per checkpoint pattern)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (the one `CustomerRepositoryLeadFilterTests.cs` gap)
- [x] No watch-mode flags
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-09-19
