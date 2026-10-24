---
phase: 09-backfill-de-score-do-backlog
verified: 2026-09-19T00:00:00Z
status: passed
score: 4/4 must-haves verified
---

# Phase 9: Backfill de Score do Backlog Verification Report

**Phase Goal:** O backlog de ~5.266 leads pré-v1.3 (que nunca passaram pelo `LeadScoringService`) tem `LeadScore`/`Segment` recalculados pelo motor oficial em C#, substituindo o fit score improvisado em Python usado na auditoria manual.
**Verified:** 2026-09-19
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (from ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Todo `Customer` relevante do backlog tem `LeadScore`/`Segment` gravado por `LeadScoringService.CalculateScore`/`SegmentForScore`, não pelo script Python de auditoria | VERIFIED | `BACKFILL-EVIDENCE.md` criterion 1: único endpoint chamado foi `POST /api/v1/pipeline/recompute-scores` → `LeadScoringService.RecomputeAllAsync` (confirmed in `PipelineController.cs:51-58`). `scripts/backfill_lead_scores.py` contains no scoring arithmetic (grepped: only HTTP orchestration, `RECOMPUTE_PATH` constant, argparse). `unsegmentedOpenLeads=0` in all 3 snapshot JSONs. |
| 2 | Leads já marcados `Inactive` (1.065, via `mark_junk_inactive.py`) não são repontuados como Hot/Warm/Cold | VERIFIED | `CustomerRepository.cs:70-75` (`GetAllLeadsAsync`) and `:85-92` (`GetSegmentCountsAsync`) both filter `Status < CustomerStatus.Customer`, structurally excluding `Inactive(5)`/`Churned(6)` (enum confirmed: Lead=0…Customer=4, Inactive/Churned follow). New regression test `CustomerRepositoryLeadFilterTests.cs` (3 `[Fact]`s) proves this against a real EF-InMemory `DiaxDbContext`, not a mock — ran live, **3/3 passed** (`dotnet test --filter CustomerRepositoryLeadFilterTests -c Release`, 4s, 0 failed). `openLeadsTotal=7242` = `hotLeads+warmLeads+coldLeads` (18+1118+6106) exactly in all 3 snapshots — the 1,065 Inactive never leak into segment counts. |
| 3 | Backfill é seguro de re-rodar — duas execuções não duplicam/corrompem score | VERIFIED | `backfill_after.json` vs `backfill_after_rerun.json`: `hotLeads`, `warmLeads`, `coldLeads`, `openLeadsTotal`, `unsegmentedOpenLeads` byte-identical (18/1118/6106/7242/0 both times); `leadsScored=7242` both calls. `--idempotency` flag in script triggers the second call. |
| 4 | Amostra antes/depois mostra `Segment` coerente com teto de fit 45 do motor oficial, não teto 25 do script ad-hoc | VERIFIED | `BACKFILL-EVIDENCE.md` sample: `leadScore` up to 90 (id `d29e37f4…`), only reachable via fit(45)+engagement(35) of the official engine; none of the sample scores fall on the legacy 0-6 or ad-hoc 0-25 scales. |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs` | EF-InMemory coverage of Inactive/Churned exclusion, contains `CustomerStatus.Churned`, ≥60 lines | VERIFIED | 116 lines, 3 `[Fact]`s, `CustomerStatus.Churned` present (3 occurrences), instantiates real `new CustomerRepository(db)` (3 occurrences), all 3 tests pass live |
| `scripts/backfill_lead_scores.py` | Deterministic trigger, contains `pipeline/recompute-scores`, ≥120 lines, no scoring formula | VERIFIED | 236 lines, `RECOMPUTE_PATH = "/api/v1/pipeline/recompute-scores"`, `--snapshot-only`/`--run`/`--idempotency` flags present, imports `load_secrets, login` from `fetch_and_classify_leads` (existing auth helper, no reimplemented logic) |
| `docs/lead-quality/backfill_baseline.json` | Pre-trigger funnel/segment snapshot with `unsegmentedOpenLeads` | VERIFIED | 353 lines, contains `unsegmentedOpenLeads: 0` |
| `docs/lead-quality/backfill_after.json` | Post-1st-trigger snapshot | VERIFIED | 353 lines, `unsegmentedOpenLeads: 0` |
| `docs/lead-quality/backfill_after_rerun.json` | Post-2nd-trigger snapshot (idempotency proof) | VERIFIED | 353 lines, `unsegmentedOpenLeads: 0`, scalars identical to `backfill_after.json` |
| `docs/lead-quality/BACKFILL-EVIDENCE.md` | Narrative answering all 4 success criteria with real numbers, contains `BFIL-01`, ≥40 lines | VERIFIED | 73 lines, references `BFIL-01` in title, answers all 4 criteria with literal numbers cited from the JSONs |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `CustomerRepositoryLeadFilterTests.cs` | `CustomerRepository.GetAllLeadsAsync` | real `CustomerRepository` instance over InMemory `DiaxDbContext` | WIRED | `new CustomerRepository(db)` used 3x, calling `.GetAllLeadsAsync()`/`.GetSegmentCountsAsync()` — asserts against the actual query implementation (`Where(c => c.Status < CustomerStatus.Customer)`), not a mock |
| `scripts/backfill_lead_scores.py` | `fetch_and_classify_leads.load_secrets/login` | import of existing auth helper | WIRED | `from fetch_and_classify_leads import load_secrets, login` present, following `mark_junk_inactive.py` convention |
| `scripts/backfill_lead_scores.py` | `POST /api/v1/pipeline/recompute-scores` | authenticated `requests.post` | WIRED | `RECOMPUTE_PATH` constant used in `trigger_recompute()`; confirmed executed against production per `BACKFILL-EVIDENCE.md` timestamps and resulting snapshot deltas |
| `PipelineController.recompute-scores` | `LeadScoringService.RecomputeAllAsync` | direct service call | WIRED | `PipelineController.cs:58`: `await _leadScoringService.RecomputeAllAsync(userId.Value, ct)` — same method used by the daily `LeadScoringWorker`, confirming "motor oficial" claim |
| `BACKFILL-EVIDENCE.md` | `backfill_after.json`/`backfill_after_rerun.json` | numbers cited literally | WIRED | All scalar values in the evidence doc (18/1118/6106/7242/0) match the JSON files exactly |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| BFIL-01 | 09-01, 09-02 | Backlog pré-v1.3 (~5.266 leads) tem LeadScore/Segment recalculados pelo motor oficial C#, não pelo script ad-hoc Python | SATISFIED | Marked `[x]` in `REQUIREMENTS.md` line 12; production backfill executed and proven idempotent (`BACKFILL-EVIDENCE.md`); regression test locks the Inactive/Churned exclusion guarantee; traceability table (line 62) confirms "Complete" |

No orphaned requirements found — REQUIREMENTS.md traceability table maps only BFIL-01 to Phase 9, and both plans declare `requirements: [BFIL-01]` in frontmatter.

### Anti-Patterns Found

None found in the phase's created files. Scan of `CustomerRepositoryLeadFilterTests.cs`, `scripts/backfill_lead_scores.py`, and `BACKFILL-EVIDENCE.md` for TODO/FIXME/placeholder/stub patterns returned only one narrative match inside the evidence markdown's prose (a sentence discussing why Cold-segment percentage is expected behavior, not a code stub) — not a code anti-pattern.

### Human Verification Required

None. This phase's success criteria are entirely about a verifiable production data state (scores/segments recalculated, idempotent, evidenced), and all four were checked against literal JSON snapshot values plus a live-running regression test suite — no UI/UX/visual/real-time behavior in scope. The one item that legitimately required a human (reviewing the 13 follow-up tasks the backfill created as a side effect) was already completed during plan 09-02's `checkpoint:human-verify` task, with the resolution (3 of 13 flagged as scraping contamination and closed) documented inline in `BACKFILL-EVIDENCE.md`.

### Gaps Summary

No gaps. All 4 observable truths from the ROADMAP success criteria are verified against actual code (query filters, controller wiring) and actual production evidence (before/after/rerun JSON snapshots), not just SUMMARY claims. The regression test was re-executed live during this verification (not trusted from the SUMMARY) and passed 3/3. BFIL-01 is correctly marked Complete in REQUIREMENTS.md and is the only requirement mapped to this phase.

---
*Verified: 2026-09-19*
*Verifier: Claude (gsd-verifier)*
