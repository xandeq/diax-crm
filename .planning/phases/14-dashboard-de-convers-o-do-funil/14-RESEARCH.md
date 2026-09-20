# Phase 14: Dashboard de Conversão do Funil - Research

**Researched:** 2026-09-20
**Domain:** Verify-and-document an already-shipped feature (C# backend formula + Next.js frontend page), no new build
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Escopo real (descoberto em discuss-phase — DASH-01 já implementado)**

`crm-web/src/app/dashboard-comercial/page.tsx` já renderiza o funil com barras proporcionais e
`conversionToNext` (percentual entre cada etapa). Backend: `Diax.Application/Customers/
SalesDashboardService.cs` já calcula `ConversionToNext`. Isso é exatamente o que DASH-01 pede.

- **D-01 (decisão do usuário):** Escopo é **só verificar e documentar** — mesmo padrão da Phase 11
  (QUAL-01/02). NÃO soma as métricas de email/WhatsApp (Fases 12/13) nessa tela agora — fica
  deferred.
- **D-02:** Verificar com dado real: os percentuais de `conversionToNext` batem com os números reais
  do funil (comparar contra `dashboard_summary` / funnel counts já vistos nesta sessão: Lead 4.201,
  Contacted 2.951, Qualified 31, etc. pós Phase 9). Confirmar que a página carrega de verdade
  (screenshot ou captura de tela, dado que é frontend) — não só que o endpoint responde.

### Claude's Discretion

- Se achar algum gap real (ex. página não reflete os dados pós-Phase-9 corretamente), decidir se é
  bug a corrigir nesta fase ou achado a documentar/deferir

### Deferred Ideas (OUT OF SCOPE)

- Somar métricas de email/WhatsApp (Fases 12/13) na mesma tela do dashboard comercial — fica pra
  milestone futura, não é esta fase
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-------------------|
| DASH-01 | Dashboard de conversão por etapa do funil (Lead→Contacted→Qualified→Customer, com percentuais, não só volume bruto) | Confirmed shipped and live in production since 2026-07-03 (commit `c200f4b7`, PR #61, on `main`). Formula verified correct via unit tests + live re-derivation of the API response (see Code Examples). Page confirmed reachable from main nav and command palette (not orphaned). One ROADMAP success criterion (period filter) is NOT implemented — flagged in Open Questions for a Claude's-Discretion call during planning. |
</phase_requirements>

## Summary

DASH-01 is not a build task. The commercial dashboard (`/dashboard-comercial`) already exists in
production, was merged in PR #61 (`c200f4b7`, 2026-07-03) — over two months before this milestone
started — and is currently linked in the main navigation (`AppShell.tsx`, section "CRM" →
"Dashboard Comercial") and in the command palette. `SalesDashboardService.BuildFunnel` implements
the percentage-per-stage math the requirement asks for, and it is already covered by four passing
xUnit tests (`SalesDashboardTests.cs`) that exercise the pure function directly, including a
divide-by-zero case.

This session captured a **live** read of `GET /api/v1/sales-dashboard` against production
(`api.alexandrequeiroz.com.br`) on 2026-09-20 and hand-verified every `conversionToNext` value
against the raw stage counts — the formula is correct and matches the code. One important nuance
was found and must be documented, not silently assumed away: `conversionToNext` is **not** an
adjacent-stage conversion rate (e.g. strictly Contacted→Qualified). It is a cumulative
"how much of (this stage + everything ahead of it) has already moved past this stage" ratio. With
`Negotiating` currently at 0 leads, this produces a technically-correct-but-visually-misleading
100% figure for that stage (2 Customers / (0 Negotiating + 2 Customer) = 100%, read on screen as
"100% avançaram" for a stage that has literally zero leads in it). This is a genuine finding this
phase should document, not a bug to fix (not in scope per D-01).

A second finding: the ROADMAP.md success criterion #3 for this phase ("Usuário pode filtrar o funil
por período, ex. última semana, último mês") is **not implemented anywhere** in the current
dashboard — there is no date-range control on the page and the backend query takes no date
parameters (only the separate 6-month revenue chart has a fixed, non-adjustable lookback). This is
the one place where "already implemented" is not fully true against the original ROADMAP wording,
and CONTEXT.md's "Claude's Discretion" clause exists precisely for this kind of gap — the planner
must explicitly decide (with the user, if warranted) whether to document this as an accepted gap or
open a small follow-up task.

**Primary recommendation:** Treat Phase 14 exactly like Phase 11 (QUALIFIED-avanco-automatico.md) —
one plan, no code changes, output is a runbook doc + a live-verified evidence trail (API capture +
screenshot), following the same "already existed, only needed proving" pattern. Do not build a
period filter as part of this phase unless the user explicitly asks after seeing this gap — CONTEXT.md
D-01 locks scope to verify+document.

## Standard Stack

No new stack. This phase reuses 100% of the existing project stack — no packages to add.

| Layer | What's already there | Confirms |
|-------|----------------------|----------|
| Backend | `Diax.Application.Customers.SalesDashboardService` (pure static methods `BuildFunnel`/`BucketByMonth`/`AcceptanceRate`) + `SalesDashboardController` (`GET /api/v1/sales-dashboard`, `[Authorize]`) | Follows Clean Architecture layering per `CLAUDE.md` — no violation found |
| Frontend | `crm-web/src/app/dashboard-comercial/page.tsx` + `crm-web/src/services/salesDashboard.ts` (`getSalesDashboard()` → `apiFetch('/sales-dashboard')`) | Follows `CLAUDE.md`'s "API calls must live in `src/services/`, never `fetch` directly in components" rule — verified, no violation |
| Tests | `api-core/tests/Diax.Tests/Application/Customers/SalesDashboardTests.cs` (xUnit) | 4 tests covering `BuildFunnel`, `BucketByMonth`, `AcceptanceRate` — see Validation Architecture |

**No installation needed.**

## Architecture Patterns

### Existing pattern this phase must follow: the Phase 11 runbook

`docs/email-marketing/QUALIFIED-avanco-automatico.md` (Phase 11, QUAL-01/02) is the canonical
template for "already built, only needs proving" phases in this milestone. Structure to mirror:

1. State the objective mechanism/formula verbatim with source file + line citation.
2. Provide a live, reconfirmed proof (not a proof copied from a prior session) — timestamps,
   real IDs, real API responses.
3. Explicitly list "what is NOT covered" / known limitations — do not hide gaps.
4. Add a "how to reproduce this proof" section with exact commands/endpoints.
5. Add a maintenance note stating the proof is a snapshot and will go stale.

Phase 13's `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` is a second reference for the same
pattern applied to a multi-requirement phase.

**Naming/location note (Claude's Discretion for planner):** every v1.4 phase runbook so far — Phase
10 (`MIGRACAO-fire-scripts.md`, `OBSERVABILIDADE-watchdogs.md`), 11 (`QUALIFIED-avanco-automatico.md`),
12 (`DMARC-escalonamento.md`, `ESCALA-EMAIL-runbook.md`), 13 (`ESCALA-WHATSAPP-runbook.md`) — lives
under `docs/email-marketing/`, even when the topic (like Phase 10's script consolidation) isn't
strictly "email". Following that established milestone convention, a `docs/email-marketing/
DASHBOARD-conversao-funil.md` (or similar name) would be consistent. There is no
`docs/dashboards/` folder precedent to break that convention for. Root `docs/` is reserved for
older, unrelated pre-v1.4 strategic docs (2026-03-28 series) — not phase runbooks.

### Pattern: pure static functions for testable business math

`SalesDashboardService.BuildFunnel(Dictionary<CustomerStatus,int>)`,
`BucketByMonth(List<Proposal>, DateTime, int)`, and `AcceptanceRate(...)` are all `public static`
pure functions taking plain data in, returning plain DTOs out — no DB/DI dependency. This is the
same pattern used by `decide()`/`veredito()` in Phases 12/13 Python scripts, applied here in C#.
It's why the formula is unit-testable without EF Core/InMemory-DB ceremony. No changes needed; just
confirm this pattern when documenting "why this formula can be trusted."

### Anti-Patterns to Avoid

- **Do not add a new endpoint/service for a "verification" query.** The existing
  `GET /api/v1/sales-dashboard` (same auth, same JWT flow used throughout this session via
  `POST /api/v1/auth/login`) is sufficient to capture live evidence. Building a parallel
  read path would violate D-01 (verify-only, no new code).
- **Do not assume the CONTEXT.md's Phase-9-backfill reference numbers (Lead 4.201, Contacted 2.951,
  Qualified 31) are still current.** They are 1 day stale as of this research. Re-measure live —
  see Code Examples for the confirmed live numbers as of 2026-09-20.

## Don't Hand-Roll

Nothing new to build here — this section is intentionally empty for this phase. The one thing to
explicitly *not* hand-roll: a manual/parallel funnel-count query (e.g., a new SQL script or Python
tool hitting the DB directly) to "double check" the numbers. The API endpoint already exists,
already queries `Customer.Status` directly (`GetStatusCountsAsync`, a straight `GroupBy(c =>
c.Status)` over the full `Customer` table, no filtering/exclusions), and is the same source of
truth the dashboard itself uses — use it for verification, don't reinvent it.

## Common Pitfalls

### Pitfall 1: `conversionToNext` is a cumulative "advanced-past" ratio, not an adjacent-stage rate
**What goes wrong:** Reading the UI label "↓ X% avançaram" under the "Contacted" bar as literally
"X% of Contacted leads became Qualified" is wrong. The actual formula (verbatim from
`SalesDashboardService.BuildFunnel`, lines 103-111):
```csharp
var ahead = FunnelStages.Skip(i + 1).Sum(s => counts.GetValueOrDefault(s.Status));
var basis = count + ahead;
conversion = basis > 0 ? Math.Round((decimal)ahead / basis, 4) : null;
```
`ahead` sums **every** stage beyond the current one (Qualified + Negotiating + Customer for the
"Contacted" row), not just the immediately-next stage. It's `(everyone beyond this stage) /
(this stage + everyone beyond it)`.
**Why it happens:** Correct math for "what fraction of the pool at-or-past this stage has moved
beyond it" (a legitimate funnel-health metric), but the UI copy ("avançaram para a próxima etapa")
and the ROADMAP wording ("Contacted→Qualified") both imply a stricter adjacent-only interpretation.
**How to avoid:** Document the formula precisely in the runbook (cite the exact lines, as above);
don't describe it in the runbook as "Contacted→Qualified rate" — describe it as "% of the
downstream pool already past this stage."
**Warning signs:** Any stage between two others where an intermediate stage has 0 count will make
this ratio jump because the smaller intermediate stage no longer dilutes the numerator/denominator gap.

### Pitfall 2: 100% conversion illusion on empty intermediate stages
**What goes wrong:** As measured live on 2026-09-20: `Negotiating = 0` leads, `Customer = 2` leads.
`ahead = 2`, `basis = 0 + 2 = 2`, `conversion = 2/2 = 1.0` → dashboard renders "↓ 100.0% avançaram"
under a bar that visually shows **zero** leads in that stage. Mathematically consistent with the
divide-by-zero guard (`basis > 0`) working as designed, but it is a real, screenshot-able
UI oddity a user could reasonably call a "bug" on first glance.
**Why it happens:** The `basis > 0` check correctly avoids `NaN`/exception when `count == 0`, but
doesn't special-case `count == 0` to suppress or caveat the percentage.
**How to avoid:** Not a fix for this phase (D-01: verify-only). Document this explicitly as a known,
accepted display quirk in the runbook — cite the live numbers (below) as proof, so a future reader
doesn't rediscover it and assume it's an unverified bug.
**Warning signs:** Any funnel stage showing 0 raw count but a non-null, high conversion percentage.

### Pitfall 3: ROADMAP success criterion #3 (period filter) is unimplemented
**What goes wrong:** Planning this phase as "100% already done, just write a doc" would silently
drop a documented success criterion. `ROADMAP.md` §Phase 14 lists 4 success criteria; criterion #3
("Usuário pode filtrar o funil por período") has no corresponding UI control or API parameter in
the current implementation — `SalesDashboardController.Get()` takes no query params beyond `ct`,
and `page.tsx` has no date picker.
**Why it happens:** The ROADMAP was written before `discuss-phase` discovered the dashboard already
existed; the original criteria assumed a fresh build where a period filter would naturally be
scoped in.
**How to avoid:** The planner must explicitly acknowledge this gap in the runbook (don't silently
mark criterion #3 "Complete") and record a discretionary decision: either (a) document it as an
accepted gap deferred to a future phase, or (b) surface it as a checkpoint question to the user
before closing DASH-01 as "Complete" in REQUIREMENTS.md/ROADMAP.md traceability. Given D-01 locks
scope to verify-only with no code changes, (a) is the path consistent with CONTEXT.md unless the
user says otherwise at a checkpoint.
**Warning signs:** Marking DASH-01 "Complete" without any mention of criterion #3 in the phase's
SUMMARY/runbook.

### Pitfall 4: BRT vs UTC when cross-checking against audit logs
**What goes wrong:** If verification involves correlating dashboard counts against
`GET /api/v1/audit-logs/resource/Customer/{id}` (as Phase 11 did for QUAL-02), timestamps in logs
are local BRT (America/Sao_Paulo, UTC-3) while `timestampUtc` in the audit log is UTC — a 3-hour
mismatch reads as "wrong" if not converted.
**Why it happens:** Documented already in Phase 11's runbook as a recurring trap in this codebase.
**How to avoid:** Only relevant if this phase's verification digs into individual audit-log entries
(not required by D-02, which asks for aggregate counts, not entry-level tracing) — but worth
carrying forward as a standing caveat if the executor chooses to spot-check specific leads.

## Code Examples

### The exact conversion formula (verbatim, verified against live data)
```csharp
// Source: api-core/src/Diax.Application/Customers/SalesDashboardService.cs, lines 94-116
private static readonly (CustomerStatus Status, string Label)[] FunnelStages =
{
    (CustomerStatus.Lead, "Leads"),
    (CustomerStatus.Contacted, "Contactados"),
    (CustomerStatus.Qualified, "Qualificados"),
    (CustomerStatus.Negotiating, "Negociando"),
    (CustomerStatus.Customer, "Clientes"),
};

public static List<FunnelStageDto> BuildFunnel(Dictionary<CustomerStatus, int> counts)
{
    var result = new List<FunnelStageDto>();
    for (var i = 0; i < FunnelStages.Length; i++)
    {
        var (status, label) = FunnelStages[i];
        var count = counts.GetValueOrDefault(status);

        decimal? conversion = null;
        if (i < FunnelStages.Length - 1)
        {
            var ahead = FunnelStages.Skip(i + 1).Sum(s => counts.GetValueOrDefault(s.Status));
            var basis = count + ahead;
            conversion = basis > 0 ? Math.Round((decimal)ahead / basis, 4) : null;
        }

        result.Add(new FunnelStageDto(status.ToString(), label, count, conversion));
    }
    return result;
}
```
`CustomerStatus` (`api-core/src/Diax.Domain/Customers/Enums/CustomerStatus.cs`) has **7** values:
`Lead=0, Contacted=1, Qualified=2, Negotiating=3, Customer=4, Inactive=5, Churned=6`. The funnel
intentionally covers only the first 5 (in-flight + won) — `Inactive`/`Churned` counts exist in the
raw `GetStatusCountsAsync()` dictionary (a plain `GroupBy(c => c.Status)` over every status with no
exclusion — `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs:77-83`) but
are never read by `BuildFunnel`'s loop, since it only iterates `FunnelStages`. This is a deliberate
scope boundary, not a bug — the funnel shows the active pipeline, not attrition. Worth stating
explicitly in the runbook since CONTEXT.md's reference numbers include `Inactive=1065`, which a
reader could otherwise expect to see reflected somewhere on this dashboard (it correctly is not).

### Live production API capture (2026-09-20, via real login + real JWT)
Reproduction: `POST https://api.alexandrequeiroz.com.br/api/v1/auth/login` with the admin
credentials already used throughout this session → `Authorization: Bearer {token}` →
`GET https://api.alexandrequeiroz.com.br/api/v1/sales-dashboard`. Actual `funnel` array returned:

```json
[
  { "stage": "Lead",        "label": "Leads",        "count": 4246, "conversionToNext": 0.4147 },
  { "stage": "Contacted",   "label": "Contactados",  "count": 2972, "conversionToNext": 0.012  },
  { "stage": "Qualified",   "label": "Qualificados", "count": 34,   "conversionToNext": 0.0556 },
  { "stage": "Negotiating", "label": "Negociando",   "count": 0,    "conversionToNext": 1      },
  { "stage": "Customer",    "label": "Clientes",     "count": 2,    "conversionToNext": null   }
]
```

Hand-verification of every value against the formula above:
- Lead: ahead = 2972+34+0+2 = 3008; basis = 4246+3008 = 7254; 3008/7254 = **0.41468** ≈ 0.4147 ✓
- Contacted: ahead = 34+0+2 = 36; basis = 2972+36 = 3008; 36/3008 = **0.01196** ≈ 0.012 ✓
- Qualified: ahead = 0+2 = 2; basis = 34+2 = 36; 2/36 = **0.05556** ≈ 0.0556 ✓
- Negotiating: ahead = 2; basis = 0+2 = 2; 2/2 = **1.0** ✓ (see Pitfall 2)
- Customer: last stage, `conversion = null` by construction (loop bound `i < Length - 1`) ✓

**Formula is correct and matches the code exactly — zero discrepancy found.**

Comparison against CONTEXT.md's post-Phase-9-backfill reference (2026-09-19): Lead 4.201→4.246
(+45), Contacted 2.951→2.972 (+21), Qualified 31→34 (+3), Negotiating 0→0, Customer 2→2 (unchanged).
Movement is small and directionally consistent with one day of live outreach activity (Phases
12/13 sending email/WhatsApp daily) — no anomaly.

### Unit test coverage already in place (proves formula correctness independent of this session)
```csharp
// Source: api-core/tests/Diax.Tests/Application/Customers/SalesDashboardTests.cs
[Fact]
public void BuildFunnel_ComputesConversionToNext()
{
    var counts = new Dictionary<CustomerStatus, int>
    {
        [CustomerStatus.Lead] = 90, [CustomerStatus.Contacted] = 5,
        [CustomerStatus.Qualified] = 3, [CustomerStatus.Negotiating] = 1,
        [CustomerStatus.Customer] = 1,
    };
    var funnel = SalesDashboardService.BuildFunnel(counts);
    // adiante de Lead: 5+3+1+1=10; base 100 → 10%
    Assert.Equal(0.10m, funnel[0].ConversionToNext);
    Assert.Null(funnel[^1].ConversionToNext);
}

[Fact]
public void BuildFunnel_EmptyCounts_AllZeroNullConversion()
{
    var funnel = SalesDashboardService.BuildFunnel(new Dictionary<CustomerStatus, int>());
    Assert.All(funnel, s => Assert.Equal(0, s.Count));
    Assert.All(funnel.Take(4), s => Assert.Null(s.ConversionToNext));
}
```
The empty-counts test proves the divide-by-zero guard (`basis > 0`) is already exercised — no gap
here for this phase to fill.

### Frontend service call (confirms real API, not mocked)
```typescript
// Source: crm-web/src/services/salesDashboard.ts (full file, 42 lines)
export async function getSalesDashboard(): Promise<SalesDashboard> {
  return apiFetch<SalesDashboard>('/sales-dashboard', { method: 'GET' });
}
```
`page.tsx` calls this in a `useCallback` `load()` on mount (after auth check) and on manual refresh
button click — no mock data path, no hardcoded fallback data. Confirms CLAUDE.md's frontend
service-layer rule is followed correctly.

### Navigation confirms the page is NOT orphaned
```typescript
// Source: crm-web/src/components/AppShell.tsx, section 'CRM' items array
{ icon: TrendingUp, label: 'Dashboard Comercial', href: '/dashboard-comercial' },
// Also present in the command palette (Ctrl+K) commandItems array:
{ label: 'Ir para Dashboard Comercial', href: '/dashboard-comercial', category: 'Navegação' },
```
Note: this project's nav lives in `AppShell.tsx`, not `Header.tsx` as `CLAUDE.md`'s "Navigation is
header-only" line might suggest by name — `Header.tsx` and `AppShell.tsx` coexist; `AppShell.tsx` is
the component that actually owns the sidebar-less nav menu + command palette in this codebase today.
Confirmed via `grep` — no other component references `/dashboard-comercial`.

### Deployment confirmation
```
git log -1 --format="%H %ci" c200f4b7
→ c200f4b7a9419120346bfeccec1e310211aa5def 2026-07-03 14:03:48 -0400

git branch --contains c200f4b7
→ main (and several feature branches merged from main)
```
The commit that introduced the dashboard (`PR #61`, "dashboard comercial — funil, receita e agenda
em uma tela") is on `main`, which auto-deploys via GitHub Actions on push
(`deploy-crm-web-hostgator.yml`, `deploy-api-core-smarterasp.yml`). This has been live in production
for ~2.5 months, well before this milestone (v1.4) began — it is not "work in progress from an
earlier phase of this same milestone," it predates the milestone entirely.

## Open Questions

1. **Should the period-filter gap (ROADMAP success criterion #3) block closing DASH-01 as "Complete"?**
   - What we know: the current implementation has no date-range filter anywhere in the dashboard;
     `SalesDashboardController.Get()` and `page.tsx` both take zero date parameters.
   - What's unclear: whether the user considers this criterion essential (in which case DASH-01
     should close "Partial" like several EMAL/WHAT requirements did in Phases 12-13) or accepts the
     snapshot-only view as sufficient given D-01's explicit "verify-only, no new code" scope.
   - Recommendation: Present this as an explicit checkpoint/finding in the phase's task sequence
     (mirroring how Phase 12's 12-06 and Phase 13's 13-01 surfaced discretionary gaps to the user
     rather than silently resolving them) — do not have the plan silently mark DASH-01 "Complete"
     without surfacing this.

2. **Exact filename/location for the runbook doc.**
   - What we know: every other v1.4 phase runbook lives in `docs/email-marketing/`, regardless of
     topical fit (Phase 10's script-consolidation and watchdog docs are there too).
   - What's unclear: whether the planner should follow that convention verbatim for a CRM-dashboard
     topic, or start a more accurately-named location (e.g. `docs/dashboards/`) since this really
     isn't about email marketing at all.
   - Recommendation: follow the established `docs/email-marketing/` convention for consistency
     with Phases 10-13 unless the user has a strong preference otherwise — this is Claude's
     Discretion per CONTEXT.md, no user decision recorded either way.

3. **Screenshot capture mechanism (D-02 requires visual proof the page renders).**
   - What we know: the project's testing protocol (`CLAUDE.md`/`STATE.md`) references Playwright
     e2e as the standard smoke-test tool (`/wave-qa`), and the app requires a real JWT login via
     `AuthGuard` before `/dashboard-comercial` renders any data (no public/mocked view).
   - What's unclear: whether the executing session has direct browser/Playwright tooling available
     to capture an actual rendered screenshot, versus needing to rely solely on the raw JSON API
     capture (already obtained and verified above) as the evidence of correctness, with a manual
     human screenshot as a separate, human-executed checkpoint step.
   - Recommendation: plan a task that (a) captures the live API JSON (already proven possible and
     done in this research), and (b) either scripts a Playwright screenshot if tooling is available
     in the execution environment, or explicitly hands a "please screenshot `/dashboard-comercial`
     after logging in" checkpoint to the human user — do not skip the visual-proof requirement
     silently just because the API-level proof is easier to obtain.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (backend, `api-core/tests/Diax.Tests/`), no frontend test framework in this repo (no Jest/Vitest/Playwright config found for `crm-web/`) |
| Config file | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| Quick run command | `dotnet test --filter "FullyQualifiedName~SalesDashboardTests" -c Release` (from `api-core/`) |
| Full suite command | `dotnet test -c Release` (from `api-core/`) — per `CLAUDE.md`, always `-c Release` (Smart App Control blocks Debug test DLLs on this machine) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|--------------------|-------------|
| DASH-01 | `BuildFunnel` computes correct `conversionToNext` per stage, including divide-by-zero safety | unit | `dotnet test --filter "FullyQualifiedName~SalesDashboardTests.BuildFunnel_ComputesConversionToNext" -c Release` | ✅ already exists |
| DASH-01 | `BuildFunnel` handles all-empty counts without throwing | unit | `dotnet test --filter "FullyQualifiedName~SalesDashboardTests.BuildFunnel_EmptyCounts_AllZeroNullConversion" -c Release` | ✅ already exists |
| DASH-01 | Live production numbers match the formula (manual/live verification, not unit-testable) | manual (live API + math re-derivation) | `POST /api/v1/auth/login` → `GET /api/v1/sales-dashboard` (done in this research, reproducible identically) | ✅ done in research; re-run recommended during execution for freshness |
| DASH-01 | Page renders real data end-to-end in the browser (not just the API in isolation) | manual (screenshot) | login via UI at `/login`, navigate to `/dashboard-comercial` | ❌ no automated e2e exists — see Open Question 3 |

### Sampling Rate
- **Per task commit:** N/A — no code changes expected this phase (D-01: verify-only)
- **Per wave merge:** Re-run `dotnet test --filter "FullyQualifiedName~SalesDashboardTests"` once, purely as a confirmation the pre-existing tests still pass (regression safety net, not new coverage)
- **Phase gate:** Live API capture (reproducible curl/script) + a human-confirmed screenshot before closing DASH-01 in REQUIREMENTS.md/ROADMAP.md traceability

### Wave 0 Gaps
- None for backend math — `SalesDashboardTests.cs` already covers the exact formula this phase
  needs to prove correct; no new test file required.
- Optional (not blocking, not required by D-01): a frontend e2e/Playwright smoke test asserting
  `/dashboard-comercial` renders the funnel section after login — does not exist today, and adding
  one would be a bd (code-writing) task, arguably out of scope for a "verify and document" phase.
  Flag as a possible small addition only if the planner judges D-02's "confirmar que a página
  carrega de verdade" requirement is best satisfied by an automated regression test rather than a
  one-time manual screenshot. Recommendation: manual screenshot is sufficient and consistent with
  how Phase 11 satisfied its own live-proof requirement (no new test file was created in 11-01).

## Sources

### Primary (HIGH confidence)
- `D:\claude-code\diax-crm\api-core\src\Diax.Application\Customers\SalesDashboardService.cs` — read in full, formula verified line-by-line
- `D:\claude-code\diax-crm\api-core\src\Diax.Api\Controllers\V1\SalesDashboardController.cs` — read in full
- `D:\claude-code\diax-crm\api-core\tests\Diax.Tests\Application\Customers\SalesDashboardTests.cs` — read in full
- `D:\claude-code\diax-crm\crm-web\src\app\dashboard-comercial\page.tsx` — read in full
- `D:\claude-code\diax-crm\crm-web\src\services\salesDashboard.ts` — read in full
- `D:\claude-code\diax-crm\api-core\src\Diax.Domain\Customers\Enums\CustomerStatus.cs` — read in full
- `D:\claude-code\diax-crm\api-core\src\Diax.Infrastructure\Data\Repositories\CustomerRepository.cs` (GetStatusCountsAsync/GetSegmentCountsAsync) — read
- Live production API call: `POST /api/v1/auth/login` + `GET /api/v1/sales-dashboard` against `api.alexandrequeiroz.com.br`, executed 2026-09-20, response hand-verified against the formula
- `git log`/`git branch --contains` on `c200f4b7` — confirms deployment history

### Secondary (MEDIUM confidence)
- `D:\claude-code\diax-crm\crm-web\src\components\AppShell.tsx` (grep matches only, not fully read) — confirms nav + command-palette entries exist; full file not read since only the two matching sections were needed

### Tertiary (LOW confidence)
- None — no WebSearch/external sources were needed for this phase; everything is internal codebase + live production verification

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new stack, fully confirmed by reading existing files
- Architecture: HIGH — pattern directly observed in code and cross-referenced against 4 prior-phase runbooks
- Pitfalls: HIGH — all four pitfalls derived from direct code reading + live data re-derivation, not speculation

**Research date:** 2026-09-20
**Valid until:** Live funnel numbers (Lead 4.246/Contacted 2.972/Qualified 34/Negotiating 0/Customer 2)
are a snapshot and will drift daily given active outreach (Phases 12/13) — re-capture at execution
time rather than trusting these exact figures if more than ~1-2 days pass. The code-level findings
(formula, nav, deployment, tests) are stable and do not need re-verification unless the code changes.
