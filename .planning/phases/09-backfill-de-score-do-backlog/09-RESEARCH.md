# Phase 9: Backfill de Score do Backlog - Research

**Researched:** 2026-09-19
**Domain:** .NET 8 Clean Architecture backend (diax-crm/api-core) — reusing an existing domain
service (`LeadScoringService`) against production data, not building new scoring logic
**Confidence:** HIGH (all claims below are verified by reading the actual source in
`api-core/src/`, not by inference from docs)

## Summary

BFIL-01 sounds like it requires building a new backfill mechanism, but the mechanism **already
exists, is already tested, and is already wired into two other code paths** (the daily
`LeadScoringWorker` and `CustomerImportService`'s create branch). `LeadScoringService.RecomputeAllAsync`
recomputes `LeadScore`/`Segment` for every `Customer` with `Status < CustomerStatus.Customer` (i.e.
Lead/Contacted/Qualified/Negotiating) using the official static `CalculateScore`/`SegmentForScore`
pair, and — critically — its underlying query (`ICustomerRepository.GetAllLeadsAsync`) already
excludes `Status = Inactive (5)` and `Status = Churned (6)` by construction. This means requirement
#2 (don't repontuate the 1,065 leads already marked Inactive via `mark_junk_inactive.py`) is
**already satisfied for free** by the existing repository filter — no new exclusion logic needed.

The real work of Phase 9 is therefore much smaller than "build a backfill job": (1) verify the
current production state (has the passive daily worker already touched the backlog, or is it still
null?), (2) trigger `RecomputeAllAsync` deterministically exactly once rather than relying on the
passive 09:00 UTC worker (which resets its "already ran today" state on every redeploy — unreliable
for a one-time backfill with a delivery deadline), (3) capture before/after evidence via the
existing dashboard/list endpoints, and (4) add one missing regression test that locks in the
Inactive-exclusion behavior at the real EF Core query level (today it's only proven at the mocked
unit-test level).

One important pitfall found: there is a **second, older, vestigial scoring engine** in
`OutreachService.SegmentLeadsAsync` (0–6 point scale, Hot≥5/Warm≥3 thresholds) still live behind
`POST /api/v1/outreach/segment`. It is NOT `LeadScoringService` and must not be confused with it or
invoked as part of this phase — see Common Pitfalls.

**Primary recommendation:** Do not write a new backfill script/engine. Trigger the existing
`POST /api/v1/pipeline/recompute-scores` endpoint (which calls
`LeadScoringService.RecomputeAllAsync` under the hood) once, deterministically, from an
authenticated one-off script following the exact login pattern already used by
`scripts/mark_junk_inactive.py`; verify via `GET /api/v1/sales-dashboard` and
`GET /api/v1/leads?segment=X` before/after; add the one missing integration test for the
Inactive-exclusion guarantee.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BFIL-01 | Backlog pré-v1.3 (~5.266 leads que nunca passaram pelo `LeadScoringService`) tem `LeadScore`/`Segment` recalculados usando o motor oficial (C#), não o script Python ad-hoc | `LeadScoringService.RecomputeAllAsync` (already exists, already tested) is the exact mechanism; `GetAllLeadsAsync`'s `Status < Customer` filter already excludes the 1,065 `Inactive` junk leads; `CalculateScore`/`SegmentForScore` are pure static functions verified idempotent by existing unit tests. See Architecture Patterns and Common Pitfalls below for how to trigger it safely and verify results. |
</phase_requirements>

## Standard Stack

No new libraries needed. This phase reuses existing project infrastructure exclusively.

### Core (existing, reused)
| Component | Location | Purpose | Why reuse, not rebuild |
|---|---|---|---|
| `LeadScoringService.CalculateScore` | `api-core/src/Diax.Application/Customers/LeadScoringService.cs:110` | Pure static 0–100 score function (fit ≤45 + engagement ≤35 + intent + penalties) | This IS "the official C# engine" the requirement asks for — Phase 8 already raised the fit cap to 45 here |
| `LeadScoringService.SegmentForScore` | same file, line 163 | Maps score → Hot/Warm/Cold (60/30 thresholds) | "Fonte ÚNICA dos limiares" per its own doc comment — used by both the daily worker and the import path; reusing it guarantees consistency |
| `LeadScoringService.RecomputeAllAsync` | same file, line 60 | Loops all open leads, computes score+segment, persists only if changed, optional follow-up task creation | Already does exactly what a "backfill" needs: bulk recompute + idempotent write-if-changed guard |
| `ICustomerRepository.GetAllLeadsAsync` | `Diax.Infrastructure/Data/Repositories/CustomerRepository.cs:70` | `WHERE Status < CustomerStatus.Customer` | Already excludes Inactive(5)/Churned(6) — satisfies requirement #2 without new code |
| `POST /api/v1/pipeline/recompute-scores` | `Diax.Api/Controllers/V1/PipelineController.cs:51` | Existing authenticated endpoint that calls `RecomputeAllAsync` | Already deployed; no new endpoint needed |
| `GET /api/v1/sales-dashboard` | `Diax.Api/Controllers/V1/SalesDashboardController.cs` | Status + Segment counts via `SalesDashboardService` (`GetStatusCountsAsync`/`GetSegmentCountsAsync`) | Ready-made before/after verification tool — no custom SQL script needed |
| `GET /api/v1/leads?segment={Hot\|Warm\|Cold}` | `Diax.Api/Controllers/V1/LeadsController.cs:35` | Paginated, segment-filterable lead list | Ready-made sampling tool for the "amostra antes/depois" success criterion |

**Installation:** none — nothing new to install. `dotnet build`/`dotnet test` on the existing
`api-core` solution is sufficient.

### Alternatives Considered
| Instead of | Could use | Tradeoff |
|---|---|---|
| Reusing `RecomputeAllAsync` via existing endpoint | New dedicated `POST /api/v1/pipeline/backfill-legacy-scores` endpoint | Only justified if you need to suppress the follow-up-task side effect (see Pitfall 1) without touching the existing endpoint's contract. Adds a new controller action + test for marginal benefit — prefer extending the existing method's optional parameter instead if this is needed. |
| Triggering via authenticated Python script (mirrors `mark_junk_inactive.py`) | Manual curl/Postman call, or a temporary `[AllowAnonymous]` debug route | Scripted login-then-call is safer (repeatable, logged, matches existing project convention) and avoids ever weakening auth even temporarily. |
| Waiting for the passive `LeadScoringWorker` (06h BRT / 09h UTC daily) | — | Do NOT rely on this for a one-time backfill deadline: `_lastRunDate` is an in-memory instance field on `BackgroundService`, reset to `null` on every app restart. Given this project deploys via GitHub Actions on every push to `main` (frequent during active phases), the worker may not reliably reach its scheduled window before the next redeploy resets it. See Pitfall 2. |

## Architecture Patterns

### Recommended Task Flow (no new production code required for the core mechanism)

```
1. Baseline capture
   GET /api/v1/sales-dashboard  →  record Status/Segment counts (esp. Inactive=1065, and
                                     count of Lead/Contacted/Qualified/Negotiating with
                                     Segment == null or LeadScore == null)

2. Trigger backfill once, deterministically
   POST /api/v1/pipeline/recompute-scores  (authenticated — same login pattern as
                                              scripts/mark_junk_inactive.py)
   → internally calls LeadScoringService.RecomputeAllAsync(currentUserId, ct)

3. Verify
   GET /api/v1/sales-dashboard  →  diff against baseline
   GET /api/v1/leads?segment=Warm (and Hot, Cold)  →  spot-check a sample: are old
     backlog leads (Source=Scraping/Import, pre-v1.3 CreatedAt) now scored coherently
     with the teto-45 formula (e.g. a lead with website+phone+eligible+local DDD but no
     Phase 7 signals should land at fit=25 → Cold, not Warm — see
     LeadScoringServiceTests.CalculateScore_WeakPhase7Signals_StaysColdWithoutEngagement)

4. Idempotency proof (success criterion 3)
   POST /api/v1/pipeline/recompute-scores again → GET /api/v1/sales-dashboard again →
   counts must be byte-identical to step 3. (Guaranteed by the
   `if (lead.LeadScore != score || lead.Segment != segment)` guard in RecomputeAllAsync,
   but the requirement asks to prove it, not just trust it.)

5. Regression test (Wave 0 gap — see Validation Architecture)
   Add an EF-InMemory integration test proving GetAllLeadsAsync excludes
   Status=Inactive/Churned, following the existing pattern in
   tests/Diax.Tests/Customers/CustomerRepositoryExternalIdTests.cs
```

### Pattern: One-off authenticated production script (existing convention)
**What:** `scripts/mark_junk_inactive.py` already establishes the project's convention for "run
once against production data": log in via `DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD` (from
`~/.claude/.secrets.env`, loaded by `fetch_and_classify_leads.py`'s `load_secrets()`/`login()`
helpers), then call one authenticated endpoint, with retry + dry-run support.
**When to use:** For triggering `POST /api/v1/pipeline/recompute-scores` exactly once. There is no
C# console-app or `dotnet run --project Tools` pattern in this repo for one-off jobs — everything
one-off goes through a small Python script hitting the deployed API, exactly like
`mark_junk_inactive.py` did for the Inactive marking step this phase depends on.
**Example (structure to follow, not to invent from scratch):**
```python
# Source: scripts/mark_junk_inactive.py (existing, verbatim pattern to reuse)
from fetch_and_classify_leads import load_secrets, login
secrets = load_secrets()
api_base = secrets.get("DIAX_API_BASE_URL", "https://api.alexandrequeiroz.com.br").rstrip("/")
token = login(api_base, secrets.get("DIAX_ADMIN_EMAIL"), secrets.get("DIAX_ADMIN_PASSWORD"))
r = requests.post(f"{api_base}/api/v1/pipeline/recompute-scores",
                   headers={"Authorization": f"Bearer {token}"}, timeout=120)
```
Note the `timeout=120` — see Pitfall 3 (shared-hosting request timeout risk with ~5k+ row bulk
update).

### Anti-Patterns to Avoid
- **Reimplementing scoring in Python:** the exact anti-pattern this phase exists to correct
  (`outreach_priority_valid_leads.csv`'s ad-hoc fit script, teto 25). Any new backfill logic MUST
  call the C# `LeadScoringService` — either via the HTTP endpoint (recommended) or, if a plan
  explicitly needs a repository-level operation, by calling the same static
  `CalculateScore`/`SegmentForScore` methods server-side. Never port the formula to Python again.
- **Calling `OutreachService.SegmentLeadsAsync` (`POST /api/v1/outreach/segment`) instead of
  `RecomputeAllAsync`:** looks superficially similar ("segment leads") but is a different, older,
  much weaker formula. See Pitfall 1.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---|---|---|---|
| Recompute score/segment for backlog leads | New backfill console app, new endpoint, or a script that re-derives fit/engagement math | `LeadScoringService.RecomputeAllAsync` via existing `POST /api/v1/pipeline/recompute-scores` | Already exists, already unit-tested (14+ test cases covering fit ceiling, bounce penalty, DDD detection, Phase 7 signal interplay), already the single source of Hot/Warm/Cold thresholds shared with the daily worker and the import path |
| Excluding junk/Inactive leads from re-scoring | A new `WHERE Status <> Inactive` filter, or a lookup against `lead_ids_junk.csv` | `ICustomerRepository.GetAllLeadsAsync` (`Status < CustomerStatus.Customer`) | Already excludes Inactive(5) and Churned(6) — this exclusion is a side effect of code that predates this phase; verifying it (not rebuilding it) is the task |
| Before/after verification of counts | Ad-hoc SQL query against the production SmarterASP DB | `GET /api/v1/sales-dashboard` (+ `GET /api/v1/leads?segment=X` for sampling) | Already returns exactly the Status/Segment counts needed; avoids a manual SQL connection to prod that CLAUDE.md discourages outside of migrations |

**Key insight:** This phase's biggest risk is *inventing* work that already exists in the codebase
(a second scoring engine, a second backfill mechanism, a second verification query). The
`LeadScoringService`/`RecomputeAllAsync`/`GetAllLeadsAsync`/`sales-dashboard` chain already forms a
complete, tested, production-proven pipeline — Phase 9's actual deliverable is triggering it once
correctly and proving it worked, not building new machinery.

## Common Pitfalls

### Pitfall 1: Confusing `LeadScoringService` with the legacy `OutreachService.SegmentLeadsAsync`
**What goes wrong:** `POST /api/v1/outreach/segment` (still live, `OutreachController.cs:48-53`)
calls `OutreachService.SegmentLeadsAsync` → `CalculateLeadScore`, a completely different, older
0–6-point formula (Website +2, Phone +1, Tags +1, priority-city +2; thresholds Hot≥5/Warm≥3/Cold<3)
— NOT `LeadScoringService`. It only touches leads where `Status == Lead && Segment == null &&
LeadScore == null`.
**Why it happens:** Both methods use the words "segment"/"score" and both live in "Outreach"-adjacent
code; a cursory search for "score" or "segment" surfaces this dead-end just as often as the real one.
**How to avoid:** Only ever call `POST /api/v1/pipeline/recompute-scores` (or the static
`LeadScoringService.CalculateScore`/`SegmentForScore` directly) for this phase. Do not touch
`OutreachController`/`OutreachService` at all — fixing/removing the legacy engine is out of scope
for BFIL-01 (out of scope per REQUIREMENTS.md: "Reescrever LeadScoringService" — and this is a
*different* service, so touching it is even further out of scope).
**Warning signs:** If a verification sample shows a backlog lead with `LeadScore` in the 0–6 range
instead of 0–100, `/outreach/segment` was called on it at some point — re-run
`/pipeline/recompute-scores` to overwrite it with the correct value (safe, since `RecomputeAllAsync`
recomputes unconditionally for all open leads, not just null ones).

### Pitfall 2: Assuming the passive daily worker already did the backfill
**What goes wrong:** `LeadScoringWorker` (`Diax.Infrastructure/Workers/LeadScoringWorker.cs`) is
registered as a hosted service (`DependencyInjection.cs:546`) and calls the exact same
`RecomputeAllAsync` every day at 09:00 UTC — in theory this should have already covered the backlog
since Phase 8's teto-45 deploy. In practice, `_lastRunDate` is a plain in-memory field with no
persistence; every `git push` to `main` triggers a redeploy (`deploy-api-core-smarterasp.yml`),
which restarts the process and resets this field to `null`. On a project with frequent same-day
deploys, the worker can go days without actually reaching its 09:00 UTC window uninterrupted.
**Why it happens:** The worker's idempotent-by-date design (`if (... || _lastRunDate == today)
continue;`) is correct for avoiding double-runs within one process lifetime, but was never designed
to survive restarts — it wasn't built with a "one-time backfill" use case in mind, only "keep daily
scores fresh."
**How to avoid:** Don't infer completion from "the worker has been deployed since Phase 8." Verify
directly via `GET /api/v1/sales-dashboard` (or a spot sample of backlog `Customer` rows) whether
`LeadScore`/`Segment` are already populated and coherent with teto 45 BEFORE deciding a trigger is
needed — then trigger the endpoint explicitly regardless, since it's a cheap, idempotent, safe
no-op if the worker already did the job.
**Warning signs:** `GetSegmentCountsAsync`/dashboard shows a large `Segment == null` count for
Status<Customer leads, or `LeadScore` values below 100/above 6 that don't match either formula
described here (would indicate a third, unknown code path — investigate before proceeding).

### Pitfall 3: Legacy leads score lower fit — this is expected, not a bug
**What goes wrong:** `WebsiteKind` defaults to `Unknown` for "todo lead legado" (doc comment on
`Customer.WebsiteKind`, `Customer.cs:66`); `Quality`/`EmailType` are nullable and default to `null`
for any `Customer` never passed through `LeadSanitizationService`/`UpdateClassification`. The
2026-09-17/19 audit scripts (`fetch_and_classify_leads.py`, `lead_quality_filter.py`,
`site_check.py`, `mx_check.py`) only ever write to local CSVs — **none of them PATCH `WebsiteKind`,
`Quality`, or `EmailType` back to the CRM** (verified: no `PATCH`/`api/v1/customers` calls in those
scripts, only `mark_junk_inactive.py` does, and only for `Status`). So most backlog leads will score
the Phase 7 fit bonuses (`WebsiteKind.OwnSite +10`, `Quality.High +5`, `EmailType.PersonalDirect
+5`) as zero — their fit ceiling in practice is 25, not the full 45, unless they separately have
strong engagement.
**Why it happens:** Phase 7's quality classification only runs at import time for NEW leads; it was
never back-applied to the pre-v1.3 backlog, and BFIL-01 (per REQUIREMENTS.md Out of Scope) does not
ask for that — only for running the *scoring* engine, with whatever inputs already exist.
**How to avoid:** Don't treat a majority-Cold outcome for the backlog as a bug to debug — it's the
correct behavior of `CalculateScore` given the data those rows actually have. If the user later
wants richer segmentation of the backlog, that's a WebsiteKind/Quality/EmailType backfill — a
different, larger phase, explicitly not this one.
**Warning signs:** none needed — this is documentation, not something to test/detect.

### Pitfall 4: Bulk `SaveChangesAsync` over thousands of rows on shared hosting
**What goes wrong:** `RecomputeAllAsync` loads ALL open leads (`ToListAsync()`, no paging — could
be well beyond the 5,266 backlog count once existing already-scored leads are included) into memory
and tracked by EF Core, then issues one `SaveChangesAsync` at the end. On SmarterASP shared hosting
(60s SQL command timeout per `CLAUDE.md`), a very large changeset could theoretically approach that
limit, and if triggered synchronously via the HTTP endpoint, IIS/the reverse proxy's own idle
timeout could also cut the request before the response returns (even though the DB write might
still complete server-side).
**Why it happens:** The endpoint is a synchronous `HttpPost` wrapping a potentially long-running
bulk operation — fine for daily incremental deltas (the write-if-changed guard means most days only
a handful of rows actually change), but a first-time backfill on ~5k+ never-before-scored rows will
have a much larger changeset than a typical daily run.
**How to avoid:** Use a generous client-side timeout when triggering (`timeout=120` or more) and
don't assume an HTTP timeout/error means the backfill failed — re-check via
`GET /api/v1/sales-dashboard` before retrying (retrying is safe/idempotent either way, but doubles
load unnecessarily). If in practice this proves too slow, the fallback is letting the passive daily
worker finish the job over 1–2 cycles instead of forcing it synchronously — but attempt the direct
trigger first since the existing test suite gives no evidence of a performance problem at this
scale.
**Warning signs:** HTTP request to `/recompute-scores` times out or returns a 5xx, while
`sales-dashboard` counts partially changed on inspection (partial write is fine, EF Core's single
`SaveChangesAsync` is one transaction so a mid-write failure would roll back entirely — a timeout
on the HTTP layer doesn't necessarily mean the DB transaction itself failed).

## Code Examples

### The official scoring engine (verbatim, source of truth)
```csharp
// Source: api-core/src/Diax.Application/Customers/LeadScoringService.cs:110-166
public static int CalculateScore(Customer lead, CustomerEngagementSummary? engagement, DateTime nowUtc)
{
    var score = 0;
    // Fit (max 45): Website+5, Phone/WhatsApp+5, IsEligibleForCampaigns+5, local DDD 27/28+10,
    // WebsiteKind.OwnSite+10, Quality.High+5, EmailType.PersonalDirect+5, HasSuspiciousDomain-15
    // Engagement (max 35): opens/clicks + recency bucket
    // Intent: Status Qualified/Negotiating +15
    // Penalties: EmailOptOut -15, WhatsAppOptOut -15
    return Math.Clamp(score, 0, 100);
}

public static LeadSegment SegmentForScore(int score) =>
    score >= HotThreshold /* 60 */ ? LeadSegment.Hot
    : score >= WarmThreshold /* 30 */ ? LeadSegment.Warm
    : LeadSegment.Cold;
```

### The existing bulk-recompute entry point (already deployed)
```csharp
// Source: api-core/src/Diax.Api/Controllers/V1/PipelineController.cs:51-60
[HttpPost("recompute-scores")]
public async Task<IActionResult> RecomputeScores(CancellationToken ct)
{
    var userId = GetCurrentUserId();
    if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
    var summary = await _leadScoringService.RecomputeAllAsync(userId.Value, ct);
    return Ok(summary); // LeadsScored, Hot, Warm, Cold, FollowUpTasksCreated
}
```
Note: this passes the calling user as `followUpOwnerUserId` — up to `MaxFollowUpsPerRun` (10)
follow-up tasks WILL be created for newly-discovered Hot, idle backlog leads. This is arguably a
desirable side effect (surfaces real opportunities in the backlog) but the plan should make an
explicit decision about it (Claude's discretion / confirm with user) rather than let it happen as
an unexamined side effect.

### Existing test pattern to extend (Wave 0 gap)
```csharp
// Source: api-core/tests/Diax.Tests/Customers/CustomerRepositoryExternalIdTests.cs (pattern)
// New test to add, same style, same file or a sibling:
[Fact]
public async Task GetAllLeadsAsync_ExcludesInactiveAndChurnedLeads()
{
    using var db = CreateDbContext(); // InMemory, per existing helper
    var openLead = SeedCustomer(db, "Aberto", "a@x.com", null); // Status defaults to Lead
    var inactive = SeedCustomer(db, "Inativo", "b@x.com", null);
    inactive.UpdateStatus(CustomerStatus.Inactive);
    var churned = SeedCustomer(db, "Churn", "c@x.com", null);
    churned.UpdateStatus(CustomerStatus.Churned);
    await db.SaveChangesAsync();

    var result = await new CustomerRepository(db).GetAllLeadsAsync();

    Assert.Single(result);
    Assert.Equal(openLead.Id, result.First().Id);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|---|---|---|---|
| Fit score improvised in Python (`outreach_priority_valid_leads.csv`), teto 25 | `LeadScoringService.CalculateScore`, teto 45, fed by Phase 7 quality signals | Phase 8 (v1.3, decision D-05), deployed via PRs #99/#111 | A lead with strong fit alone can now reach Warm without any engagement; Hot still requires real engagement by construction |
| Manual/ad-hoc junk classification via CSV review | `mark_junk_inactive.py` → `PATCH /customers/{id}/status` → `Status = Inactive` | 2026-09-17/19 audit session, already executed (1,065 rows confirmed) | Those rows are now structurally excluded from `GetAllLeadsAsync`/`RecomputeAllAsync` — no separate exclusion logic needed for BFIL-01 |

**Deprecated/outdated:**
- `OutreachService.SegmentLeadsAsync`/`CalculateLeadScore` (`Diax.Application/Outreach/OutreachService.cs:135-209`) — still live behind `POST /api/v1/outreach/segment`, but superseded by `LeadScoringService` for any lead that also flows through the pipeline/import/daily-worker paths. Not touched by this phase; flagged here purely so the planner doesn't mistake it for the "official engine."

## Open Questions

1. **What is the CURRENT production state of `LeadScore`/`Segment` for the backlog, right now?**
   - What we know: The mechanism to backfill exists and is idempotent; the passive daily worker
     may or may not have already reached these rows (Pitfall 2).
   - What's unclear: Without live DB/API access at research time, it's unverified whether the
     backlog already shows populated scores or still shows nulls/legacy-formula values.
   - Recommendation: Make "capture baseline via `GET /api/v1/sales-dashboard`" the first task of
     the plan (Wave 0), before deciding whether the trigger step is a no-op confirmation or an
     actual state change.

2. **Should the backfill trigger create follow-up tasks (existing side effect of
   `POST /recompute-scores`)?**
   - What we know: `RecomputeAllAsync(userId, ct)` as called by the existing endpoint always passes
     a `followUpOwnerUserId`, capped at 10 tasks/run.
   - What's unclear: Whether the user wants ~10 "new" follow-up tasks to suddenly appear from
     3-year-old backlog leads that just got scored Hot for the first time, or whether that's noise.
   - Recommendation: Surface this as a decision point in planning (Claude's discretion per
     CONTEXT.md conventions, or ask the user directly) — cheap to support both (call with
     `followUpOwnerUserId: null` for a silent backfill, or keep default for the "bonus" behavior).

## Validation Architecture

### Test Framework
| Property | Value |
|---|---|
| Framework | xUnit (+ Moq for mocks) — `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| Config file | none dedicated — standard `dotnet test` via the csproj |
| Quick run command | `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests" -c Release` |
| Full suite command | `dotnet test -c Release` (Release required — Debug DLLs are blocked by Smart App Control per project CLAUDE.md/STATE.md) |

Note: project `CLAUDE.md` describes `Diax.UnitTests/`+`Diax.IntegrationTests/` as the test layout;
the actual repo has a single `tests/Diax.Tests/` project mixing both styles (mocked unit tests under
`Application/`, EF-InMemory integration-style tests under `Customers/`). Treat `CLAUDE.md`'s
description as stale on this point — `Diax.Tests` is the real, single test project.

Also note: `CLAUDE.md` lists FluentAssertions as a test library; the actual
`LeadScoringServiceTests.cs` uses plain xUnit `Assert.*` throughout. Follow the file being extended
(xUnit `Assert`) rather than the CLAUDE.md-documented convention, for consistency with neighboring
tests.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|---|---|---|---|---|
| BFIL-01 | `CalculateScore`/`SegmentForScore` honor teto-45 thresholds (Hot/Warm/Cold) | unit | `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests" -c Release` | ✅ (14 existing test cases already cover this, including the exact teto-45 ceiling) |
| BFIL-01 | `RecomputeAllAsync` is idempotent (write-if-changed guard) | unit | `dotnet test --filter "FullyQualifiedName~RecomputeAllAsync_SegmentsLeads" -c Release` | ✅ (`LeadScoringServiceTests.RecomputeAllAsync_SegmentsLeads_AndPersists`) |
| BFIL-01 | `GetAllLeadsAsync` excludes `Inactive`/`Churned` leads (requirement #2 — don't repontuate junk) | integration (EF InMemory) | `dotnet test --filter "FullyQualifiedName~CustomerRepository" -c Release` | ❌ Wave 0 — see gap below |
| BFIL-01 | Production backfill actually ran and is idempotent against real prod data (success criteria 1/3/4) | manual/operational | `GET /api/v1/sales-dashboard` before/after diff; re-run trigger and diff again | N/A — inherently a one-time operational verification against live data, not a repeatable automated test |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~LeadScoringServiceTests|FullyQualifiedName~CustomerRepository" -c Release`
- **Per wave merge:** `dotnet test -c Release` (full suite)
- **Phase gate:** Full suite green, PLUS the manual before/after dashboard evidence captured in the
  plan's SUMMARY (this phase's success criteria are partly about live production state, which no
  automated suite can assert)

### Wave 0 Gaps
- [ ] New test: `GetAllLeadsAsync` excludes `Status = Inactive(5)` and `Status = Churned(6)` —
      add to `tests/Diax.Tests/Customers/` following the `CustomerRepositoryExternalIdTests.cs`
      InMemory pattern (see Code Examples above). Currently this guarantee only exists implicitly
      in production code and is not asserted by any test.
- [ ] No framework/tooling install needed — xUnit/Moq already present, `LeadScoringServiceTests.cs`
      already gives near-complete coverage of the pure scoring functions.

*(No test framework gaps — only the one missing integration test above.)*

## Sources

### Primary (HIGH confidence — read directly from repo source)
- `api-core/src/Diax.Application/Customers/LeadScoringService.cs` — full read, `CalculateScore`/`SegmentForScore`/`RecomputeAllAsync` implementation and doc comments
- `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs` — `GetAllLeadsAsync` query, confirms `Status < CustomerStatus.Customer` filter
- `api-core/src/Diax.Domain/Customers/Enums/CustomerStatus.cs` — full enum, confirms Inactive=5/Churned=6 values
- `api-core/src/Diax.Domain/Customers/Customer.cs` — full read, confirms `WebsiteKind` defaults `Unknown`, `Quality`/`EmailType` nullable, `UpdateSegmentation` is a plain setter (no side effects)
- `api-core/src/Diax.Infrastructure/Workers/LeadScoringWorker.cs` — daily 09:00 UTC trigger, confirms in-memory `_lastRunDate` state
- `api-core/src/Diax.Infrastructure/DependencyInjection.cs:546` — confirms `LeadScoringWorker` is registered as a hosted service
- `api-core/src/Diax.Api/Controllers/V1/PipelineController.cs` — `POST /recompute-scores` endpoint, confirms `followUpOwnerUserId` side effect
- `api-core/src/Diax.Api/Controllers/V1/OutreachController.cs` + `Diax.Application/Outreach/OutreachService.cs` — confirms the legacy `SegmentLeadsAsync`/`CalculateLeadScore` engine and its live endpoint
- `api-core/src/Diax.Api/Controllers/V1/SalesDashboardController.cs`, `LeadsController.cs` — confirms verification/sampling endpoints
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs:624-652` — doc comments explicitly stating "Quem repontua leads existentes é o LeadScoringWorker das 06:00 BRT"
- `api-core/tests/Diax.Tests/Application/Customers/LeadScoringServiceTests.cs` — full read, confirms extensive existing coverage and xUnit (not FluentAssertions) convention
- `api-core/tests/Diax.Tests/Customers/CustomerRepositoryExternalIdTests.cs` — confirms EF-InMemory integration test pattern to extend
- `scripts/mark_junk_inactive.py` — confirms the "1,065 already Inactive" fact and the project's one-off-script-against-prod convention
- `.planning/PROJECT.md` (Phase 9 section + "Contexto de origem") — confirms audit session dates, CSV row counts (1,065/1,728/2,364), and that this is a rerun-safety requirement, not a one-shot script

### Secondary (MEDIUM confidence)
- `docs/lead-quality/lead_ids_junk.csv` row count (1,066 lines incl. header = 1,065 data rows) cross-checked against PROJECT.md's stated "1.065 registros" — consistent
- Grep across `scripts/fetch_and_classify_leads.py` and `scripts/lead_quality_filter.py` found no `PATCH`/`api/v1/customers` calls, supporting the claim that Quality/EmailType/WebsiteKind were never written back for the backlog (absence-of-evidence, moderately confident given full-file grep, not a full manual read of both scripts)

### Tertiary (LOW confidence)
- None — all findings above were verifiable directly against source code and committed data files.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new dependencies, every component cited was read directly from source
- Architecture: HIGH — the recommended flow reuses existing, already-tested, already-deployed code paths
- Pitfalls: HIGH for Pitfalls 1/2/3 (verified by reading the actual conflicting code / doc comments); MEDIUM for Pitfall 4 (reasoned from configured timeouts, not empirically measured against a 5k-row prod dataset)

**Research date:** 2026-09-19
**Valid until:** 30 days (stable internal codebase, low churn risk area — but re-verify current
`sales-dashboard` counts at plan time regardless, since Open Question 1 is time-sensitive)
