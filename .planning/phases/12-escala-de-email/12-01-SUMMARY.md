---
phase: 12-escala-de-email
plan: 01
subsystem: email-marketing
tags: [dmarc, whm, dns, python, email-deliverability, email_events]

# Dependency graph
requires:
  - phase: 10-motor-unico-de-cadencia
    provides: waves_core.py/daily_waves.py module-safety pattern (waves_smoke.py import-safe check) that dmarc_escalation.py had to satisfy
provides:
  - "docs/email-marketing/dmarc_escalation.py: DMARC escalation engine (read + decide + gated write, zero real writes executed)"
affects: [12-05-escrita-dmarc-em-producao, 12-06-runbook-e-verificacao-final]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "WHM zone locate-then-edit (dumpzone -> editzonerecord), Line re-resolved on every call, never persisted"
    - "Bounce/spam health sourced from email_events (event_type 4/5) via db.py, never the EmailCampaign.UnsubscribeCount aggregate"
    - "Pure decision function decide(st, health, today) - no I/O, fully unit-testable via offline --selftest"
    - "Write path gated by apply=False default; --apply flag exists but is never invoked programmatically by this plan"

key-files:
  created: [docs/email-marketing/dmarc_escalation.py]
  modified: []

key-decisions:
  - "--simulate-bad-window uses a synthetic non-floor state (pct=50) in addition to injected health, so it actually demonstrates the revert branch — with the real production pct at the floor (25), injecting bad health alone would only ever produce 'hold, ja no minimo' per decide()'s own floor-guard rule"
  - "--drill (used by 12-05 against a disposable _gsddrill subdomain) was intentionally NOT added in this plan — PLAN.md's Task 2 CLI list only specifies --status/--selftest/--check/--advance/--revert/--set-pct, and --drill is Wave 2 scope per 12-VALIDATION.md's task map (12-05-02)"

requirements-completed: [EMAL-01]

duration: 25min
completed: 2026-09-20
---

# Phase 12 Plan 01: DMARC Escalation Engine Summary

**Built `dmarc_escalation.py` — reads the live WHM DMARC zone + real email_events bounce/spam health, decides advance/hold/revert via a pure function, and gates every write behind `--apply` (never invoked in this plan; DNS confirmed unchanged at pct=25 throughout).**

## Performance

- **Duration:** 25 min
- **Started:** 2026-09-20T13:05Z (approx)
- **Completed:** 2026-09-20T13:15Z (approx)
- **Tasks:** 2
- **Files modified:** 1 (new)

## Accomplishments

- `read_zone_dmarc()` locates the `_dmarc` TXT record via `dumpzone` on every call (never persists the zone `Line`, per Pitfall 1 — the zone has 164 lines and 5 other domains that can shift it)
- `step_health()` is the single source of truth for bounce/spam rate, querying `email_events` directly by `event_type` 4 (Bounced) and 5 (Spam), counted by distinct `queue_item_id` — never the campaign-aggregate `UnsubscribeCount`, which the Phase 12 research proved conflates spam with voluntary opt-out
- `decide(st, health, today)` is a pure function (no I/O) implementing the exact 6-branch order from PLAN.md: health-based revert always evaluated first (bypasses the 7-day gate and the minimum-sample gate — D-02/D-04), floor-guard against reverting below pct=25, `done` at pct=100, `hold` for missing/insufficient elapsed time or sample, `advance` otherwise
- `set_pct()`/`commit_state()`/`tg_send()` implement the real write path completely, but `apply` defaults to `False` everywhere and every CLI verb (`--advance`, `--revert`, `--set-pct N`) only writes with an explicit `--apply` flag that no task in this plan ever passes
- 9-assertion offline `--selftest` (3 on `parse_pct`/`TPL`, 6 on `decide()`'s branches) runs in under a second with zero network/DB access
- `waves_smoke.py`'s `imports_locais_resolvem` check (which dynamically imports every `.py` in the folder) stayed green throughout — confirms the module does no network/DB/`load_env()` work at import time

## Task Commits

1. **Task 1: Leitura de estado — zona WHM, state file e saude real de email_events** - `596f646` (feat)
2. **Task 2: Decisao advance/hold/revert + escrita WHM gated por --apply (nunca acionada aqui)** - `c1b5f31` (feat)

**Plan metadata:** (this commit) `docs(12-01): complete DMARC escalation engine plan`

## Files Created/Modified

- `docs/email-marketing/dmarc_escalation.py` (488 lines) - DMARC escalation engine: WHM zone read, email_events health, pure decision logic, WHM write gated by `--apply`, offline selftest, Telegram alert templates (module-local, not wired to `alerta_telegram.py`)

## Live Evidence (captured during Task 1/2 verification, this session, 2026-09-20)

**`--status` (read-only):**
```
Zona WHM: pct=25 (Line 125)
  txtdata: v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r
State file: dmarc_pct=25 dmarc_changed_at=None
  Divergencia zona vs state: nenhuma
Saude do degrau atual (ultimos 7 dias (state sem dmarc_changed_at ainda)):
  sent=59 bounced=3 spam=0 bounce_rate=5.08% spam_rate=0.00%
```
(matches the baseline measured live in PLAN.md's `<interfaces>` block exactly: 59/3/0/5.08%)

**`--check` (real decision, verbatim):**
```
acao=hold pct_atual=25 pct_alvo=25
motivo: ja no degrau minimo (pct=25) - bounce=5.08% spam=0.00% em 59 envios (limiar bounce>5% ou spam>0.1%)
health: sent=59 bounced=3 spam=0 bounce_rate=5.08% spam_rate=0.00%
```
The real bounce rate today (5.08%) is already above the D-04 revert threshold (5%), but production is already sitting at the floor step (pct=25) — `decide()`'s floor guard correctly reports `hold`, not `revert`, since there is nowhere lower to go. This is the honest state of the base, not a bug (as PLAN.md's own interfaces block anticipated).

**`--simulate-bad-window --check` (synthetic, proves the revert branch):**
```
(--simulate-bad-window ativo - pct=50 e health INJETADOS, nao vem do banco/state real)
acao=revert pct_atual=50 pct_alvo=25
motivo: bounce=12.00% spam=0.00% em 500 envios (limiar bounce>5% ou spam>0.1%)
```

**`--advance` (dry-run, no write):**
```
DRY-RUN - requisicao que SERIA enviada:
  GET https://srv232.prodns.com.br:2087/json-api/editzonerecord
  Authorization: whm alexa084:***
  line=125
  txtdata=v=DMARC1; p=quarantine; pct=50; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r
```
Immediately after, `nslookup -type=TXT _dmarc.alexandrequeiroz.com.br ns232.prodns.com.br` still showed `pct=25` — zero write occurred.

**`previews/daily-state.json` MD5 — identical before and after running `--status`, `--check`, `--simulate-bad-window --check`, `--advance`, and `--revert`:**
```
353c337084bc6b4e235b3f5a3779ba0a
```
No `logs/dmarc-escalation.log` file was ever created during this session — proof `--apply` was never reached (that code path is the only one that calls `log_line()`).

## Decisions Made

- `--simulate-bad-window` simulates both health AND a non-floor `pct` (50) rather than only health, because the real production DMARC is already at the floor (pct=25) — `decide()`'s own floor-guard rule means bad health alone against the real state would only ever demonstrate `hold`, never `revert`. This was necessary to satisfy PLAN.md's explicit acceptance criterion that `--simulate-bad-window --check` prints `acao=revert`, while still keeping the 6 `decide()` unit scenarios in `--selftest` testing the pure function directly against both floor and non-floor states.
- `--drill` (referenced in 12-VALIDATION.md's task map for 12-05, run against a disposable `_gsddrill` subdomain) was deliberately not implemented here — PLAN.md's own Task 2 CLI enumeration for 12-01 lists only `--status | --selftest | --check | --advance | --revert | --set-pct`, and the drill mechanics belong to 12-05 (Wave 2, after the human checkpoint), which depends on this plan.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed non-ASCII em-dash characters causing mojibake in Windows console output**
- **Found during:** Task 1/2 live verification (`--status`, `--check`, `--advance`, `--simulate-bad-window --check` runs)
- **Issue:** Several `print()` statements used a Unicode em-dash (`—`) inside f-strings shown to the user (`do_status()` header, `decide()`'s floor-guard motivo string, `--simulate-bad-window` notice, and the apply-path `log_line()` message). On this Windows console codepage, that character rendered as a `�` replacement glyph instead of crashing — cosmetic but confusing output, and a real risk of `UnicodeEncodeError` on a stricter codepage (this project has a standing memory/rule about Windows Python encoding issues).
- **Fix:** Replaced all em-dashes inside strings that reach `print()`/`log_line()` with a plain ASCII hyphen (`-`). Em-dashes left untouched in code comments and docstrings (never printed) and in `tg_send()` messages (sent as UTF-8 JSON to Telegram's API, which handles full Unicode natively).
- **Files modified:** `docs/email-marketing/dmarc_escalation.py` (same file, both task commits)
- **Verification:** Re-ran `--selftest`, `--status`, `--check`, `--simulate-bad-window --check`, `--advance`, `--revert` — all output clean ASCII, no `�` glyphs, exit codes unchanged.
- **Committed in:** `596f646` (Task 1 portion) and `c1b5f31` (Task 2 portion) — fixed before each task's final commit, not as a separate commit.

**2. [Rule 1 - Bug] Fixed `datetime.datetime.utcnow()` deprecation warning**
- **Found during:** Task 1 live `--status` run
- **Issue:** `_since_datetime_from_state()`'s fallback path used `datetime.datetime.utcnow()`, deprecated in the Python version installed (3.14) and scheduled for removal.
- **Fix:** Replaced with `datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None)`, which produces the same naive-UTC datetime pyodbc expects for the `email_events.occurred_at` comparison, without the deprecation warning.
- **Files modified:** `docs/email-marketing/dmarc_escalation.py`
- **Verification:** Re-ran `--status`; no warning in output.
- **Committed in:** `596f646` (Task 1)

**3. [Rule 1 - Bug] Removed literal `UnsubscribeCount` string from an explanatory docstring**
- **Found during:** Task 2 self-verification of the acceptance criterion `grep -c "UnsubscribeCount\|email-campaigns/analytics" dmarc_escalation.py` must return `0`
- **Issue:** `step_health()`'s docstring originally named `EmailCampaign.UnsubscribeCount` explicitly to explain why it's not used — this satisfied the intent (documenting the anti-pattern) but literally failed the acceptance criterion's grep, which is a strict textual absence check, not a semantic one.
- **Fix:** Reworded the docstring to describe the same anti-pattern ("the campaign's aggregate opt-out counter") without using the literal forbidden strings.
- **Files modified:** `docs/email-marketing/dmarc_escalation.py`
- **Verification:** `grep -c "UnsubscribeCount\|email-campaigns/analytics" dmarc_escalation.py` now returns `0`; `--selftest` and `waves_smoke.py` still green.
- **Committed in:** `c1b5f31` (Task 2)

---

**Total deviations:** 3 auto-fixed (all Rule 1 — bug fixes discovered during this plan's own verification, no scope creep, no architectural changes).
**Impact on plan:** All three are cosmetic/correctness fixes to code written in this same plan, caught before the task's own commit. No behavior change to `decide()`, `set_pct()`, or any write-gating logic.

## Issues Encountered

None beyond the three auto-fixed items above.

## User Setup Required

None - no external service configuration required. `WHM_HOST`/`WHM_PORT`/`WHM_USER`/`WHM_API_TOKEN` and `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` already existed in `~/.claude/.secrets.env`, confirmed working live (`--status` successfully authenticated to WHM and read the real zone).

## Next Phase Readiness

- `dmarc_escalation.py` is ready for 12-05 (Wave 2, gated by human checkpoint) to add `--drill` (disposable-record proof) and to be the first plan that ever passes `--apply`.
- Current production DMARC state (`pct=25`, bounce rate 5.08% already above the 5% revert threshold) means 12-05's own `--check`/`--advance` dry-run will likely report `hold` at the floor, not `advance` — this is expected and consistent with the real state of the base, not a blocker; the deliverability warmup work in 12-02/12-03/12-04 (volume ramp, A/B measurement) may need to improve bounce rate before pct can realistically advance past 25.
- No blockers for 12-02/12-03 (parallel plans in this same wave, different files).

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/dmarc_escalation.py
- FOUND: .planning/phases/12-escala-de-email/12-01-SUMMARY.md
- FOUND: commit 596f646 (Task 1)
- FOUND: commit c1b5f31 (Task 2)
