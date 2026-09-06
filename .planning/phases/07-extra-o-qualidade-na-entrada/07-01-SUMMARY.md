---
phase: 07-extra-o-qualidade-na-entrada
plan: 01
subsystem: api
tags: [csharp, xunit, domain-modeling, lead-quality, python-port]

# Dependency graph
requires: []
provides:
  - "WebsiteKind enum (Unknown/OwnSite/Directory) in Diax.Domain.Customers.Enums"
  - "WebsiteClassifier.Classify(url) — pure, 1:1 port of classify_url/DIRECTORY_HOSTS (site_check.py)"
  - "JunkDomainFilter.IsJunk(domain) / IsJunkEmail(email) — pure, 1:1 port of is_junk_domain (mx_check.py)"
affects: [07-02, 07-03, 07-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pure static classifier/filter classes (no interface, no DI) for functions with zero I/O — mirrors the WaveGeoFilter-style port used for IsOutsideTargetGeo"
    - "Python truth-table ported to xUnit [Theory]/[InlineData] with a companion [Fact] that pins list cardinality (count + Distinct().Count()) instead of grepping quoted strings, to survive future refactors safely"

key-files:
  created:
    - api-core/src/Diax.Domain/Customers/Enums/WebsiteKind.cs
    - api-core/src/Diax.Application/Customers/WebsiteClassification/WebsiteClassifier.cs
    - api-core/src/Diax.Application/Customers/Services/JunkDomainFilter.cs
    - api-core/tests/Diax.Tests/Customers/WebsiteClassifierTests.cs
    - api-core/tests/Diax.Tests/Customers/JunkDomainFilterTests.cs
  modified: []

key-decisions:
  - "DirectoryHosts array is a verbatim, byte-for-byte copy of DIRECTORY_HOSTS (site_check.py) — 41 entries confirmed programmatically before porting (D-08)"
  - "JunkHosts/JunkSuffixes/JunkParts are verbatim copies of mx_check.py's JUNK_HOSTS/JUNK_SUFFIXES/JUNK_PARTS — no entries added or removed"
  - "Both classes are static and pure (no interface, no DI registration) since they have zero I/O and zero state — consistent with existing WebsiteClassification-adjacent pure helpers in this codebase"

patterns-established:
  - "Pure Python-ported logic goes in a static class under Diax.Application/<Module>/, with a companion xUnit Theory test mirroring every case from the Python docstring/behavior table 1:1"

requirements-completed: [EXTR-01, EXTR-03]

# Metrics
duration: 12min
completed: 2026-09-06
---

# Phase 7 Plan 1: WebsiteClassifier + JunkDomainFilter Summary

**Ported `classify_url`/`DIRECTORY_HOSTS` (site_check.py) and `is_junk_domain` (mx_check.py) to pure C# static classes, closing the gap that let `instagram.local` and `wixpress.com` bounce through the .NET import path before any MX query.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-09-06T09:19:00Z
- **Completed:** 2026-09-06T09:31:00Z
- **Tasks:** 2
- **Files modified:** 5 (all new)

## Accomplishments
- `WebsiteKind` enum + `WebsiteClassifier.Classify(url)` — classifies a lead's `website` field as `OwnSite` vs `Directory` (third-party page: econodata, social networks, marketplaces, page builders) vs `Unknown` (empty/placeholder/malformed), never throwing on malformed input
- `JunkDomainFilter.IsJunk(domain)` / `.IsJunkEmail(email)` — pre-DNS filter that catches placeholder domains (`.local`, `example.com`, etc.) and real-but-junk infrastructure domains (`wixpress.com`, `sentry.io`) that a pure MX check would miss because they have valid MX records
- 55 new tests (19 + 36) mirroring the Python truth tables case-by-case, plus a canonical-list-cardinality guard test for `DirectoryHosts` (41 entries, D-08)
- Full suite verified green: 781 tests passing (baseline 707 + 55 new from this plan + additional tests from the concurrent 07-02 plan), 0 failures, build clean in Release

## Task Commits

Each task was committed atomically:

1. **Task 1: WebsiteKind enum + WebsiteClassifier (porta 1:1 de classify_url)** - `58403b7` (feat)
2. **Task 2: JunkDomainFilter (porta 1:1 de is_junk_domain)** - `495d30e` (feat)

**Plan metadata:** (this commit, next)

## Files Created/Modified
- `api-core/src/Diax.Domain/Customers/Enums/WebsiteKind.cs` - `Unknown=0 / OwnSite=1 / Directory=2` domain enum
- `api-core/src/Diax.Application/Customers/WebsiteClassification/WebsiteClassifier.cs` - pure static `Classify(string? url)`, 41-entry `DirectoryHosts` list, `DirectoryHostList` read-only view for test parity
- `api-core/src/Diax.Application/Customers/Services/JunkDomainFilter.cs` - pure static `IsJunk(string? domain)` and `IsJunkEmail(string? email)`, `JunkSuffixes`/`JunkHosts`/`JunkParts` verbatim from `mx_check.py`
- `api-core/tests/Diax.Tests/Customers/WebsiteClassifierTests.cs` - 18 `[InlineData]` cases + 1 canonical-list `[Fact]` = 19 tests
- `api-core/tests/Diax.Tests/Customers/JunkDomainFilterTests.cs` - 33 `[InlineData]` cases + 3 `[Fact]` for `IsJunkEmail` = 36 tests

## Decisions Made
- Confirmed `DIRECTORY_HOSTS` has exactly 41 entries by running a small Python snippet against the canonical `site_check.py` before writing the C# copy (rather than trusting a manual count) — matches the plan's D-08 constraint exactly.
- No new NuGet dependencies, no changes to `Diax.Tests.csproj` — both test files use pure xUnit `Assert.*`, consistent with the plan's explicit "FluentAssertions not available" constraint.
- Did not touch `ExtractorIntegrationService.cs` or any wiring — this plan only produces the two pure classifier/filter units; consumption is a downstream plan's job per the file scope in the plan frontmatter.

## Deviations from Plan

None — plan executed exactly as written, using the exact code blocks specified in each task's `<action>`.

**Note (not a deviation, not fixed):** The acceptance criterion for Task 2 states `grep -c "instagram" JunkDomainFilter.cs` should return `0`. The file's XML doc comment (copied verbatim from the plan's own `<action>` block) mentions `instagram.local` as the motivating incident example, so the literal grep returns `1` (from the comment, not from `JunkHosts`). The intent of the criterion — that `instagram.local` is caught by the `.local` suffix rule and has no dedicated `JunkHosts` entry — is true and is verified by the `IsJunk("instagram.local") == true` test case, which passes. Not changed, since the plan's own code block is the source of the comment and altering it would deviate from the "EXATAMENTE" instruction.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. Both classes are pure, in-process code with zero I/O.

## Next Phase Readiness
- `WebsiteClassifier.Classify` and `JunkDomainFilter.IsJunk`/`IsJunkEmail` are ready to be wired into `ExtractorIntegrationService.ImportLeadsAsync` (planned for a later plan in this phase, per `07-CONTEXT.md` code_context — insertion point is right after `IsLowQualityEmail`/before `IsOutsideTargetGeo`).
- A lead rejected by `JunkDomainFilter` should be counted in the "e-mail lixo" bucket (`LowQualityEmailRejectedCount`), not a new bucket, per the plan's decision log — this is a note for whichever plan wires the counters into `CustomerImport` (D-04).
- No blockers. Full test suite (781 tests) green in Release configuration.

---
*Phase: 07-extra-o-qualidade-na-entrada*
*Completed: 2026-09-06*

## Self-Check: PASSED

All 5 created files found on disk. Both task commits (`58403b7`, `495d30e`) found in git log. Full test suite re-verified green (781 passed, 0 failed) after both tasks.
