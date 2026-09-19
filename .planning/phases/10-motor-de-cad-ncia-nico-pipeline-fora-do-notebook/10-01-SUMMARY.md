---
phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook
plan: 01
subsystem: testing
tags: [python, ast, hashlib, regression-smoke, email-marketing, script-consolidation]

# Dependency graph
requires: []
provides:
  - "waves_smoke.py — offline (no-network) 5-check regression smoke for daily_waves.py's cadence engine, <5s runtime"
  - "waves-baseline.json — frozen pre-refactor snapshot (COPY_BANK sha256, build_html_v2 sha256, ROTATION, HEALTHY)"
  - "MIGRACAO-fire-scripts.md — grep-generated inventory/migration path for the 48 fire_*.py scripts + 7 live non-fire_-named dependents"
affects: [10-02, 10-03, 10-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AST-based static import verification (no dynamic import of side-effecting modules like debug_campaign.py)"
    - "Hash-freeze regression baseline (sha256 of canonical JSON + rendered HTML) as a lightweight substitute for a test framework in a directory with none"

key-files:
  created:
    - docs/email-marketing/waves_smoke.py
    - docs/email-marketing/waves-baseline.json
    - docs/email-marketing/MIGRACAO-fire-scripts.md
  modified: []

key-decisions:
  - "waves_smoke.py never calls importlib.import_module on any module whose name starts with fire_ — this both protects debug_campaign.py's target (fire_today_20, which triggers a real email send on import) and correctly no-ops on fire_*-named import targets since those get archived later in the phase."
  - "Check 5 (imports_locais_resolvem) still parses debug_campaign.py's own source via ast.parse (safe, no execution) to inspect its import statements — it is excluded from being an import *target*, not from being a scanned *source* file."

patterns-established:
  - "Baseline JSON is regenerated only via --update-baseline flag, intentionally requiring a manual/deliberate action before it can change — prevents an in-flight refactor from silently rewriting its own regression oracle."

requirements-completed: [SCRP-01]

# Metrics
duration: ~20min
completed: 2026-09-19
---

# Phase 10 Plan 01: Rede de Segurança do Motor de Cadência Summary

**Offline hash-freeze smoke check (`waves_smoke.py`, 5 checks, <5s, zero rede) + baseline congelado do estado atual + inventário real de migração dos 48 `fire_*.py`, todos sem tocar em nenhum arquivo de produção.**

## Performance

- **Duration:** ~20 min
- **Completed:** 2026-09-19T21:42:52Z
- **Tasks:** 2
- **Files modified:** 3 (all new/additive — zero production files touched)

## Accomplishments

- `waves_smoke.py` created: 5 checks (`rotation_coberta`, `copy_bank_integro`, `template_v2_identico`,
  `healthy_mutavel_in_place`, `imports_locais_resolvem`), stdlib-only, runs in <2s
- `waves-baseline.json` frozen against the **pre-refactor** code (`daily_waves.py` still importing
  from `fire_today_20`/`fire_w1_2707`/`fire_w2w3_2707`) — this is the reference state future plans
  (10-02/10-03/10-04) must not regress from
- Negative test executed and proven: corrupting `copy_bank_sha256` to `"xxx"` produces exit code 1
  and `FALHOU copy_bank_integro: sha256 de COPY_BANK diverge do baseline (...)`, then restored to
  `5/5 OK` via `--update-baseline`
- `MIGRACAO-fire-scripts.md` documents the real (grep/ls-generated, not memorized) inventory: 48
  `fire_*.py` files, 3 extraction sources, 45 dead-end archival candidates, and the 7 live
  non-`fire_*`-named dependents (`debug_campaign.py`, `import_extrator_bridge.py`,
  `replenish_leads.py`, `rescrape_21.py`, `rescrape_27.py`, `rescrape_tavily_27.py`,
  `scrape_and_import.py`) with the mandatory rewire-before-archive order

## Smoke Output (evidence)

```
OK rotation_coberta
OK copy_bank_integro
OK template_v2_identico
OK healthy_mutavel_in_place
OK imports_locais_resolvem
== 5/5 OK ==
```
Run twice consecutively — identical output both times (determinism confirmed).

**Baseline hashes (frozen 2026-09-19T21:38:54Z, regenerated after negative-test restore):**
- `copy_bank_sha256`: `1d43f11515b90b0b76cc5c192b77c989835cf12503903110bf6d131116292b6c`
- `template_v2_sha256`: `05d50fa3620c8662ee3f1fbcf344f95ce5caf863e24f2933727caafb80f3fd49`
- `rotation`: 20 items · `copy_bank_niches`: 20 items

**Negative test (proves the smoke actually detects regression):**
1. Edited `waves-baseline.json`, set `copy_bank_sha256` to `"xxx"`
2. `python waves_smoke.py` → exit code `1`, output `FALHOU copy_bank_integro: sha256 de COPY_BANK diverge do baseline (atual=1d43f115..., baseline=xxx)`, `== 4/5 OK ==`
3. `python waves_smoke.py --update-baseline` → baseline restored, `python waves_smoke.py` → `5/5 OK` again, exit `0`

**Real fire_*.py count:** `ls fire_*.py | wc -l` → **48** (matches the count documented in `MIGRACAO-fire-scripts.md`)

## Task Commits

Each task was committed atomically:

1. **Task 1: Criar waves_smoke.py e congelar o baseline do motor atual** - `e7c8ae18` (feat)
2. **Task 2: Documentar o caminho de migração dos 48 fire_*.py** - `96dd5583` (docs)

_No TDD-style RED/GREEN split — Task 1 was written directly to the behavior spec and verified via
its own `--update-baseline` + double-run + negative-test cycle rather than a separate failing-test
commit, since there is no test framework in this directory (per 10-VALIDATION.md)._

## Files Created/Modified

- `docs/email-marketing/waves_smoke.py` - 5-check offline regression smoke (232 lines); AST-based import scanner never dynamically imports any `fire_*`-prefixed module
- `docs/email-marketing/waves-baseline.json` - frozen pre-refactor snapshot (COPY_BANK sha256, build_html_v2 sha256, ROTATION list, COPY_BANK niche keys, HEALTHY, PER_PROVIDER)
- `docs/email-marketing/MIGRACAO-fire-scripts.md` - 156-line migration doc: extraction map, 7-dependent table with file:line, archival criterion, "how to add a new campaign" and 2-command verification recipe

## Decisions Made

- **Module-exclusion by prefix, not by name, for Check 5's import-target guard:** rather than
  hardcoding `debug_campaign.py` as a special case, `waves_smoke.py` excludes any import target
  starting with `fire_` from `importlib.import_module`. This is more robust (also covers the other
  46 `fire_*.py` files that will disappear from the tree in plan 10-04 without the smoke needing an
  update) and still satisfies the "never import debug_campaign.py" constraint, since its only local
  import (`fire_today_20`) falls under that same prefix exclusion. A comment in the source file
  names `debug_campaign.py` explicitly so the reasoning isn't implicit.
- **`ARGS_TEMPLATE` and `CHAVES_CFG` are frozen constants** exactly as specified in the plan's
  interface contract — changing them after `--update-baseline` has been run would silently
  invalidate the baseline's `template_v2_sha256`/`copy_bank_sha256` without any error, so they are
  documented as "never change" in the script's own docstring intent.

## Deviations from Plan

None - plan executed exactly as written. Both tasks matched their `<action>`/`<behavior>` spec and
all `<acceptance_criteria>` were verified individually (see Self-Check below).

## Issues Encountered

None. One correction was made to `MIGRACAO-fire-scripts.md`'s hand-typed 48-file table before
committing (a manual column-alignment pass had introduced a duplicate/misalignment) — caught by
re-diffing against `ls -1 fire_*.py | sort` before the file was staged, not after commit, so no
extra commit was needed for the fix.

## User Setup Required

None - no external service configuration required. Fully offline, no network calls, no writes to
the CRM.

## Next Phase Readiness

- `waves_smoke.py` + `waves-baseline.json` are ready to be run after every extraction step in plans
  10-02/10-03 as the fast (<5s) regression gate specified in 10-VALIDATION.md's sampling rate
- `MIGRACAO-fire-scripts.md` gives plan 10-03 the exact file:line list of the 7 dependents that must
  be rewired before plan 10-04 can archive `fire_today_20.py`/`fire_w1_2707.py`/`fire_w2w3_2707.py`
- No blockers. `docs/email-marketing/reply_watch.py` remains uncommitted (pre-existing, unrelated
  work per STATE.md Blockers) and was correctly left untouched by this plan.

## Self-Check: PASSED

- FOUND: docs/email-marketing/waves_smoke.py
- FOUND: docs/email-marketing/waves-baseline.json
- FOUND: docs/email-marketing/MIGRACAO-fire-scripts.md
- FOUND: e7c8ae18 (Task 1 commit)
- FOUND: 96dd5583 (Task 2 commit)

---
*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Completed: 2026-09-19*
