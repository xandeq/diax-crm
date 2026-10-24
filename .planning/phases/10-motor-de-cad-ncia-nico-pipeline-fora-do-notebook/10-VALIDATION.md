---
phase: 10
slug: motor-de-cad-ncia-nico-pipeline-fora-do-notebook
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-09-19
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — `docs/email-marketing/` is plain Python scripts run via Windows Task Scheduler, entirely outside the .NET/xUnit suite. No pytest.ini/conftest.py anywhere in the tree. |
| **Config file** | none |
| **Quick run command** | `python daily_waves.py --dry` (builds the full plan, writes no data, sends no email/Telegram) |
| **Full suite command** | N/A — verification here is operational/manual, same precedent as Phase 9 (`docs/lead-quality/BACKFILL-EVIDENCE.md`) |
| **Estimated runtime** | ~5-10 seconds per dry-run/smoke check |

Note: `test_p1.py` exists in this directory but despite its name is NOT a unit test — it's a manual
live-fire WhatsApp outreach script that hits the real WAHA API. Do not treat it as test
infrastructure.

---

## Sampling Rate

- **After every task commit:** Run `python -m py_compile <changed_file>.py` + `python waves_smoke.py` (10-01's offline smoke check, ~5s, no side effects)
- **After every plan wave:** `python daily_waves.py --dry --force` (bypasses weekend guard) — confirms the full 20-niche plan still builds after each refactor step
- **Before `/gsd:verify-work`:** Full manual evidence per the Manual-Only Verifications table below, mirroring Phase 9's `BACKFILL-EVIDENCE.md` pattern
- **Max feedback latency:** ~10 seconds (smoke script), fault-injection checks (OBSV-01) are one-off manual verifications, not part of a tight loop

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 10-01-01 | 01 | 1 | SCRP-01 | offline smoke (hash-freeze) | `python waves_smoke.py` | ✅ (created by this task) | ⬜ pending |
| 10-01-02 | 01 | 1 | SCRP-01 | doc | N/A (migration doc) | ✅ | ⬜ pending |
| 10-02-01..03 | 02 | 2 | SCRP-01 | dry-run + smoke | `python daily_waves.py --dry --force` + `python waves_smoke.py` | ✅ | ⬜ pending |
| 10-03-01 | 03 | 3 | SCRP-01 | manual (bridge-critical) | `python -c "import import_extrator_bridge"` + before/after dry-run capture | ✅ | ⬜ pending |
| 10-03-02..03 | 03 | 3 | SCRP-01 | manual (import check) | `python -c "import <module>"` per remaining 6 live dependents | ✅ | ⬜ pending |
| 10-04-01 | 04 | 4 | SCRP-01 | mechanical gate | `grep -rn "from fire_\|import fire_" docs/email-marketing/**/*.py \| grep -v "^fire_"` (must be empty) | ✅ | ⬜ pending |
| 10-04-02 | 04 | 4 | SCRP-01 | checkpoint:human-verify | N/A — user reviews grep-empty proof before the `mv` | N/A | ⬜ pending |
| 10-04-03 | 04 | 4 | SCRP-01 | filesystem move + re-verify | `python daily_waves.py --dry --force` post-archive | ✅ | ⬜ pending |
| 10-05-01..02 | 05 | 1 | OBSV-01 | manual (fault injection) | Stop WAHA/n8n, run watchdog once, confirm Telegram alert; restart, confirm recovery message | ✅ | ⬜ pending |
| 10-05-03 | 05 | 1 | OBSV-01 | checkpoint:human-verify | N/A — user confirms alert format/content before considering done | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `waves_smoke.py` (10-01 Task 1) — offline hash-freeze smoke check (`COPY_BANK`/`build_html_v2` sha256 against pre-refactor baseline, `HEALTHY` mutation check via `sys.modules[...].__module__` resolution) — created as part of this phase's own Wave 1, not a pre-existing gap.

*No other Wave 0 gaps — this directory intentionally has no pytest framework, consistent with Phase 9's precedent, and `waves_smoke.py` is the phase's own lightweight substitute, built as its first deliverable.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| `daily_waves.py` still builds the full 20-niche plan after extraction | SCRP-01 | No CI/test framework in this directory; live production script | `python daily_waves.py --dry --force`, compare niche list in the dry-run log against `ROTATION` |
| Archived files don't break anything importing them | SCRP-01 | Depends on real filesystem state post-move | `python -c "import <module>"` for each of the 7 live dependents, post-archive |
| `import_extrator_bridge.py` (extrator→CRM production bridge) still works after rewiring | SCRP-01 | Highest-risk file in the phase — the user explicitly asked mid-session whether this pipeline still works | Before/after dry-run capture + explicit assertion that rewired imports resolve to the same values as before |
| Watchdog correctly detects WAHA/n8n down and alerts via Telegram | OBSV-01 | No sandbox/staging for WAHA/n8n — testing against the real local instance is unavoidable | Stop the `waha` container or n8n-local task, run the watchdog once, confirm Telegram message arrives; restart, confirm recovery message |
| Alert doesn't spam on repeated failure | OBSV-01 | Transition-only logic needs to be proven against real repeated runs | Run the watchdog twice in a row while a failure persists; confirm the second run does NOT re-alert; verify the state file updates |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (py_compile/AST/dry-run/grep) or are `checkpoint:human-verify` with an automated pre-check
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covered — `waves_smoke.py` is the phase's own Wave 0 deliverable (10-01 Task 1)
- [x] No watch-mode flags
- [x] Feedback latency < 10s for the automated smoke path; manual fault-injection checks are one-off by nature (OBSV-01 has no alternative — no WAHA/n8n staging environment exists)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-09-19
