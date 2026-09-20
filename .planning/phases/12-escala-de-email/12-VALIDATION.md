---
phase: 12
slug: escala-de-email
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-09-20
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None — `docs/email-marketing/` is plain Python, no pytest/unittest suite. Verification is manual/operational (dry-run flags, live DNS checks, DB queries), same precedent as Phases 9/10/11. |
| **Config file** | none |
| **Quick run command** | `python daily_waves.py --dry` (existing) + new `--dry`/`--status`/`--check` modes added by this phase's own plans to `dmarc_escalation.py`/`ab_report.py` |
| **Full suite command** | N/A — no automated suite exists or is warranted for this directory |
| **Estimated runtime** | ~5-15 seconds per dry-run/status check |

---

## Sampling Rate

- **After every task commit:** Run the relevant script's dry-run/status mode (`dmarc_escalation.py --status`, `daily_waves.py --dry`, `ab_report.py`) — never `--apply` outside the explicit checkpointed task in 12-05.
- **After every plan wave:** Full manual walkthrough per plan's own `<verify>` blocks; Wave 2 (12-04/12-05) requires the human checkpoint before any real DNS write.
- **Before `/gsd:verify-work`:** Live DMARC record re-queried via `nslookup`, one real (non-dry) `daily_waves.py` run showing actual queued volume, `ab_report.py` output captured as evidence.
- **Max feedback latency:** ~15 seconds for automated checks; DNS propagation checks are inherently slower (DNS TTL-bound) but are manual/one-off, not part of a tight dev loop.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 12-01-01..02 | 01 | 1 | EMAL-01 | dry-run only, zero writes | `python dmarc_escalation.py --status` / `--advance` (dry) | ✅ (created) | ⬜ pending |
| 12-02-01..02 | 02 | 1 | EMAL-02 | grep + dry-run | `grep -n "guard >= target"` + `daily_waves.py --dry` | ✅ | ⬜ pending |
| 12-03-01..02 | 03 | 1 | EMAL-03 | manual/report | `python ab_report.py` (reads real campaign data, read-only) | ✅ (created) | ⬜ pending |
| 12-04-01..02 | 04 | 2 | EMAL-04 | live query + dry-run | live API query for FREE_WEBMAIL count + `daily_waves.py --dry --webmail` | ✅ | ⬜ pending |
| 12-05-01 | 05 | 2 | EMAL-01 | checkpoint:decision | N/A — read-only prep (status/check/advance dry, zone backup, nslookup) | N/A | ⬜ pending |
| 12-05-02 | 05 | 2 | EMAL-01 | manual (disposable drill) | `dmarc_escalation.py --drill` against `_gsddrill.alexandrequeiroz.com.br` | ✅ | ⬜ pending |
| 12-05-03 | 05 | 2 | EMAL-01 | manual (real write, gated) | `dmarc_escalation.py --set-pct N --apply` (only task allowed to write) | ✅ | ⬜ pending |
| 12-06-01..03 | 06 | 3 | EMAL-01..04 | checkpoint:human-verify | N/A — user reviews runbook + real evidence | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `dmarc_escalation.py` — needs `--dry`/`--status`/`--check` modes before any `--apply` is trustworthy (delivered by 12-01, this phase's own Wave 1)
- [x] Volume-target-for-today logic — no prior script computed "today's warmup target"; delivered by 12-02
- [x] sA/sB click-rate grouping query — no prior script grouped by A/B variant; delivered by 12-03

*All three Wave 0 gaps are this phase's own first-wave deliverables, not pre-existing gaps to fill separately.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| DMARC step advances only after clean bounce/spam window | EMAL-01 | Live DNS + live `email_events` data, no staging environment | `nslookup -type=TXT _dmarc.alexandrequeiroz.com.br` before/after; cross-check `dmarc_escalation.py --status` output against the decision made |
| Automatic revert + Telegram alert on bad window | EMAL-01 | No safe way to force a real bad window against production | Drill against disposable `_gsddrill.alexandrequeiroz.com.br` record (12-05 Task 2) proves the write/revert mechanics; real revert logic proven via `decide()`'s live bounce-rate branch (already measured at 5.08%/10.58%, correctly yields hold, not a fabricated test) |
| Daily volume matches warmup_day's target | EMAL-02 | Depends on real lead stock per niche, not just code logic | `daily_waves.py --dry` inspection of the DRY plan log, cross-checked against `warmup_day` state key |
| A/B click-rate comparison in Friday report | EMAL-03 | Real campaign data, small click sample (12 in 30d) | `ab_report.py` run against live data — INCONCLUSIVO is the honest expected result given current sample size, not a bug |
| Webmail leads get distinct tag + respect cap | EMAL-04 | Real CRM data | Query `/api/v1/leads?search=...` for FREE_WEBMAIL domains post-dry-run, confirm cap (15) not exceeded and tag present |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (dry-run/status/grep) or are checkpoint-type with an automated pre-check
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covered — all 3 gaps are this phase's own Wave 1 deliverables
- [x] No watch-mode flags
- [x] Feedback latency <15s for automated checks; manual DNS/DB checks are inherently one-off, not loop-blocking
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-09-20
