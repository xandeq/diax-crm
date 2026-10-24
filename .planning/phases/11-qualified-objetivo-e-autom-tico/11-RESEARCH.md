# Phase 11: Qualified Objetivo e Automático - Research

**Researched:** 2026-09-20
**Domain:** Operational verification of an already-implemented automation (email reply classification → CRM status advancement), Windows Task Scheduler, SQL Server audit trail
**Confidence:** HIGH

## Summary

This phase requires no new code for QUAL-01/QUAL-02 — `docs/email-marketing/reply_watch.py` (commit `e6532e47`) already implements the objective Contacted→Qualified criterion (`classify_intent(text) != 'negative'`) and already runs unattended via Windows Task Scheduler task `DIAX-Reply-Watch`. Live verification during this research confirmed all three legs of D-02's proof requirement: (1) the task is `Ready` and has been executing on its real cadence with `LastTaskResult=0`, (2) the state file (`previews/replies-state.json`) and log file (`logs/reply-watch.log`) show real classified replies going back to 2026-09-04, and (3) cross-referencing one `seen` entry against the CRM's own audit log (`GET /api/v1/audit-logs/resource/Customer/{id}`) produced a second-level timestamp match between the script's local log line and a real `Status: 1→2` database write — the strongest possible evidence a "verify, don't build" phase can produce.

The work in this phase is entirely documentation: write a runbook (following Phase 10's `OBSERVABILIDADE-watchdogs.md` pattern) that (a) states QUAL-01's criterion explicitly and traceably, and (b) records the D-02 evidence trail so it doesn't just live in this research file. No code changes are required to satisfy QUAL-01/QUAL-02. The only uncommitted diff on `reply_watch.py` (WhatsApp draft greeting wording in `build_drafts`/`hot_whatsapp_draft`) does not touch `classify_intent` or `main()`'s status-advancement logic — it must be left untouched/preserved, not discarded, per the existing STATE.md blocker.

**Primary recommendation:** Write one new runbook file documenting the objective criterion (QUAL-01) and the real evidence trail (QUAL-02) using the exact commands/queries below to reproduce the proof; do not modify `reply_watch.py`'s classification or status-advancement code.

## User Constraints

### Locked Decisions
- **D-01:** Phase 11 scope is verify-and-document only. Do not extend to WhatsApp (which only creates a task today, never advances status) and do not tighten the "non-negative = Qualified" criterion (it currently accepts neutral replies like "quem é você?"). Both are explicitly deferred.
- **D-02:** The phase must prove — with real evidence, same pattern as `BACKFILL-EVIDENCE.md` (Phase 9) and `OBSERVABILIDADE-watchdogs.md` (Phase 10) — that `DIAX-Reply-Watch` actually runs (Scheduled Task `Ready`/`Running`, not `Disabled`) and that `crm_status` worked for real at least once (a real `customer_id` that went from status<2 to status=2 via this mechanism).
- **D-03:** Document QUAL-01 as a formal requirement — the objective criterion is literally `classify_intent(text) != 'negative'` — somewhere traceable (new runbook or a section in `docs/email-marketing/OBSERVABILIDADE-watchdogs.md`), not left implicit in code.

### Claude's Discretion
- Where exactly to document (new file vs. a section in the existing Phase 10 runbook).
- How to prove the "at least once" of D-02 (query DB/API vs. a controlled test).

### Deferred Ideas (OUT OF SCOPE)
- Extending auto-advancement to Qualified on the WhatsApp channel too.
- Tightening the "non-negative = Qualified" criterion (today accepts neutral replies such as "who is this?").

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QUAL-01 | Objective Contacted→Qualified criterion defined and documented | Criterion located and confirmed in code (`classify_intent`), exact regexes captured below, ready to be transcribed verbatim into a runbook |
| QUAL-02 | `reply_watch.py` automatically advances CRM status when QUAL-01 criterion is met | Live proof gathered: Scheduled Task status/schedule, state file history, log file history, and a real audit-log-confirmed `Status: 1→2` transition attributable to a specific scheduled run |

## Standard Stack

No new libraries. This is a verification/documentation phase over existing Python 3.14 stdlib + `requests`/`pyodbc`/`imaplib` code already in production. No installation needed.

**Version verification:** N/A — no new dependencies introduced by this phase.

## Architecture Patterns

### How `reply_watch.py` actually advances status (confirmed by reading `main()`, lines 242-288)

1. `load_env()` reads `~/.claude/.secrets.env` into `os.environ` (same pattern as every other script in `docs/email-marketing/`).
2. `load_state()` reads `docs/email-marketing/previews/replies-state.json` (`{last_uid, seen}`), defaulting to `{'last_uid': 0, 'seen': {}}` if the file is missing/corrupt.
3. `fetch_new(st)` opens `imaplib.IMAP4_SSL('mail.alexandrequeiroz.com.br', 993)` with `HOSTGATOR_CONTATO_EMAIL`/`HOSTGATOR_CONTATO_PASS`, searches UIDs `> last_uid` (or last 48h on first run), returns up to 60 new messages.
4. `lead_context(emails)` — **direct SQL Server query** via `import db as crmdb` (module lives outside this repo, see Don't Hand-Roll below) joining `email_queue_items` + `email_campaigns` + `customers` to resolve `customer_id`, `name`, `phone`, `website`, `service`, and current `status` for each replying address, restricted to campaigns named `AQ - %` with `q.status=2` (sent).
5. For each message not already `seen` in the last 7 days and not an auto-reply/bounce (`is_auto_reply`): `classify_intent(text)` runs the two regexes (QUAL-01's actual criterion — see Code Examples). `check_site()` is called for a personalized draft; drafts are built; a Telegram alert (`tg()`) and a CRM task (`crm_task()`, `POST /api/v1/tasks`) are always created for any handled reply.
6. **The QUAL-02 branch (lines 280-283):**
   ```python
   if intent == 'negative':
       suppress(em)
   elif lead.get('customer_id') and (lead.get('status') or 0) < 2:
       crm_status(H, lead['customer_id'], 2)
   ```
   `crm_status` does `PATCH /api/v1/customers/{id}/status {"status": 2}` — the same endpoint `mcp__diax-crm__update_lead_status` uses.
7. `save_state(st)` only runs `if not DRY` — dry runs (`--dry` flag) never persist `seen`/`last_uid`, which explains a historical anomaly found in the evidence (see Common Pitfalls).

### Recommended Runbook Structure (mirrors `OBSERVABILIDADE-watchdogs.md`)
```
docs/email-marketing/QUALIFIED-avanco-automatico.md   (new, recommended name)
├── 1. Critério objetivo (QUAL-01) — classify_intent() verbatim + NEG/POS regex
├── 2. Onde roda — Task DIAX-Reply-Watch: schedule, state, action command
├── 3. Prova real de funcionamento (D-02) — the audit-log timestamp match below
├── 4. O que NÃO está coberto (D-01 boundary) — WhatsApp gap, criterion looseness
└── 5. Dependências externas — db.py/site_check.py live outside this repo
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Proving a scheduled task really runs | A new monitoring script | `Get-ScheduledTask` + `Get-ScheduledTaskInfo` (already used in Phase 10's research) | Native, zero-dependency, gives `State`, `LastRunTime`, `LastTaskResult`, full trigger definition in one call |
| Proving a specific field changed in the DB | Ad-hoc SQL query against production | `GET /api/v1/audit-logs/resource/Customer/{id}` (existing `AuditLogsController`, requires `audit.view` permission, already granted to the admin account `reply_watch.py` itself authenticates as) | Every `Customer` save is already captured by `AuditSaveChangesInterceptor` with `oldValues`/`newValues`/`changedProperties`/`timestampUtc` — this is the authoritative source, not log-file guessing |

**Key insight:** Don't write a new verification script. Every piece of proof needed for D-02 is retrievable read-only through interfaces that already exist (`Get-ScheduledTask*`, the CRM's own audit endpoint, the script's own state/log files). Writing a bespoke prover would violate D-01 (this phase touches nothing, it only reads).

## Runtime State Inventory

Not applicable — this is not a rename/refactor/migration phase. No strings, IDs, or identifiers are being renamed. Noted for completeness per the researcher checklist; skipped by design.

## Real Evidence Gathered (for direct transcription into the runbook)

### 1. State file — confirms real execution history

**Path (confirmed in code, `STATE = DIR + 'previews/replies-state.json'`):**
`docs/email-marketing/previews/replies-state.json`

Current content (2026-09-20):
```json
{
 "last_uid": 13523,
 "seen": {
  "<email-redacted>@redacted.invalid": {"at": "2026-09-04", "intent": "neutral", "seg": ""},
  "<email-redacted>@redacted.invalid": {"at": "2026-09-04", "intent": "neutral", "seg": ""},
  "<email-redacted>@redacted.invalid": {"at": "2026-09-04", "intent": "neutral", "seg": ""},
  "<email-redacted>@redacted.invalid": {"at": "2026-09-04", "intent": "negative", "seg": "clinica"},
  "<email-redacted>@redacted.invalid": {"at": "2026-09-07", "intent": "positive", "seg": "pet"},
  "<email-redacted>@redacted.invalid": {"at": "2026-09-17", "intent": "negative", "seg": "pousada"}
 }
}
```
`last_uid=13523` confirms the IMAP polling has advanced far past the mailbox's early UIDs — not a fresh/never-run state.

### 2. Scheduled Task `DIAX-Reply-Watch` — confirms it actually runs, on schedule, successfully

```
TaskName : DIAX-Reply-Watch
State    : Ready

LastRunTime        : 9/18/2026 7:00:01 PM
LastTaskResult     : 0            (success)
NextRunTime        : 9/21/2026 8:00:00 AM   (next Monday — correctly skips the weekend)
NumberOfMissedRuns : 0

Trigger:
  StartBoundary : 2026-09-04T08:00:00-03:00
  DaysOfWeek    : 62              (Mon+Tue+Wed+Thu+Fri bitmask — matches docstring "seg-sex")
  Repetition Interval : PT30M
  Repetition Duration : PT11H     (08:00 → 19:00 — matches docstring "08-19")

Action:
  Execute          : C:\Program Files\Python314\pythonw.exe
  Arguments        : D:\claude-code\diax-crm\docs\email-marketing\reply_watch.py
  WorkingDirectory : D:\claude-code\diax-crm\docs\email-marketing
```
No `--dry` flag in the registered action — the scheduled task always runs live. This is the exact schedule the module docstring claims ("roda a cada 30 min (seg-sex 08-19)"), independently confirmed via `Get-ScheduledTask`/`Get-ScheduledTaskInfo`, not just read from a comment.

### 3. Log file — confirms real polling cadence and real classifications

**Path (confirmed in code, `LOG = DIR + 'logs/reply-watch.log'`):**
`docs/email-marketing/logs/reply-watch.log` (81 lines, 2026-09-04 → 2026-09-18)

The log shows regular "N msgs novas, 0 respostas tratadas" heartbeat lines every 30-90 min across weekdays (proving the task fires on schedule even when nothing happens), interspersed with real classified replies:
```
[2026-09-07 08:00:08] 🟢 <email-redacted>@redacted.invalid [positive] pet | ...
[2026-09-07 08:00:22] 12 msgs novas, 1 respostas tratadas
[2026-09-17 13:30:04] 🔴 <email-redacted>@redacted.invalid [negative] pousada | ...
[2026-09-17 13:30:15] 1 msgs novas, 1 respostas tratadas
```

### 4. Cross-reference against the CRM's own audit trail — the D-02 proof

Queried **read-only** (`GET`, admin JWT from `DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD`, same login pattern `reply_watch.py`'s own `api_login()` uses) against:
- `GET /api/v1/customers?search={email}` — resolves current status
- `GET /api/v1/audit-logs/resource/Customer/{id}` — full change history (`oldValues`/`newValues`/`changedProperties`/`timestampUtc`), requires `audit.view` (already granted to the admin account)

**Confirmed case — `<email-redacted>@redacted.invalid`** (customer_id `2dcb6a14-006a-426c-bc60-b6599a8f0e64`, "Pet Shop Abertos 24 Horas Em Fundão, Es"):

| Timestamp (UTC) | changedProperties | Status before → after |
|---|---|---|
| 2026-09-05T13:48:54Z | LeadScore,Segment | 0 → 0 |
| 2026-09-05T18:00:18Z | EmailSentCount,LastEmailSentAt | 0 → 0 |
| 2026-09-05T18:39:30Z | (full entity, first real save) | 0 → 1 (Contacted) |
| **2026-09-07T11:00:22.9668226Z** | **(full entity, incl. Status)** | **1 → 2 (Qualified)** |
| 2026-09-07T17:01:19Z | LeadScore,Segment | 2 → 2 |
| 2026-09-14T11:53:37Z | LeadScore (Phase 9 backfill) | 2 → 2 |

**The match:** the log line `[2026-09-07 08:00:08] 🟢 <email-redacted>@redacted.invalid [positive] pet` is in BRT (UTC-3, the notebook's local time) → `2026-09-07 11:00:08 UTC`. The audit log's `Status: 1→2` write landed at `2026-09-07T11:00:22.9668226Z` — **14 seconds later**, entirely consistent with the script's sequential HTTP calls (`api_login()` → `crm_task()` POST → `crm_status()` PATCH) inside the same `main()` execution that produced that exact log line. This is a second-level, cross-system timestamp correlation between (a) the scheduled task firing, (b) `classify_intent` returning `'positive'`, and (c) a real production database write advancing Status from Contacted to Qualified. This is the D-02 evidence.

**Corroborating negative-path evidence:** both `<email-redacted>@redacted.invalid` and `<email-redacted>@redacted.invalid` were classified `negative` and their audit trails show **no** `Status`-changing event at all (only `LeadScore` edits from the Phase 9 backfill) — status stayed at 1 (Contacted), consistent with the `negative` branch calling `suppress()` instead of `crm_status()`.

**A second, weaker data point:** `<email-redacted>@redacted.invalid` is also `status=2` today, but its `Status: 1→2` audit event (`2026-09-05T10:04:55Z`) predates any `reply_watch.py` log entry for that address — this lead was already Qualified by another mechanism before `reply_watch.py` ever classified its reply as `neutral` on 2026-09-04/07. This is **not** attributable to `reply_watch.py`; it instead demonstrates the `status<2` guard is working correctly (the script saw status already `>=2` and correctly skipped a redundant PATCH). Do not cite this case as QUAL-02 proof — cite `<email-redacted>@redacted.invalid` only, it is the unambiguous one.

### 5. IMAP dependency — confirmed independent of Phase 10's WAHA/n8n watchdogs

`fetch_new()` connects to `mail.alexandrequeiroz.com.br:993` using `HOSTGATOR_CONTATO_EMAIL`/`HOSTGATOR_CONTATO_PASS` (both present in `~/.claude/.secrets.env`, confirmed by key name only, values not read). This is a Hostgator mailbox — entirely unrelated infrastructure to WAHA (WhatsApp) or n8n. Confirmed live: Phase 10's watchdog tasks are `n8n-watchdog` (Ready) and `waha-watchdog` (Running) — two separate Scheduled Tasks, neither monitors mail server reachability. If `HOSTGATOR_CONTATO_*` becomes invalid or the mail server stops answering, `fetch_new()` logs `'sem HOSTGATOR_CONTATO_*'` and returns `[]` silently — `reply_watch.py` would keep "succeeding" (`LastTaskResult=0`) while doing nothing, exactly the kind of gap `OBSV-01` closed for WAHA/n8n. **This is explicitly out of scope for Phase 11 per D-01** — note it in the new runbook as a known gap, do not build a fix.

### 6. Cross-repo dependency — `db.py` and `site_check.py` live outside `diax-crm`

`reply_watch.py` does `sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')` and then `import db as crmdb` / `from site_check import check_site, summarize_fup`. Both modules physically live at `C:\Users\acq20\.claude\skills\financas\scripts\` — **outside the diax-crm repository**, in the user's global Claude "finanças" skill folder. `db.py` reads `DIAX_DB_CONNSTRING` from `~/.claude/.secrets.env` and opens a **direct `pyodbc` connection to the production SQL Server** (`sql1002.site4now.net`), bypassing the .NET API entirely — used by `lead_context()` (read) and `suppress()` (write to `email_suppressions`). This is a working, real dependency (confirmed the file exists and imports cleanly), not a bug — but worth one sentence in the runbook's "dependências externas" section: this script breaks if that global skill folder is ever reorganized, and it is one of two write paths into the CRM (direct SQL for suppression, REST API for status advancement) — mixed-integration pattern, not something to refactor in this phase.

## Common Pitfalls

### Pitfall 1: Assuming every `seen` entry is reply_watch-caused proof
**What goes wrong:** Treating any email in `replies-state.json` with `status=2` in the CRM as automatic proof of QUAL-02.
**Why it happens:** The state file and CRM status can agree by coincidence — a lead can reach status 2 through another path (manual advance, a different automation) before or independent of `reply_watch.py` ever processing its reply.
**How to avoid:** Always cross-reference against `GET /api/v1/audit-logs/resource/Customer/{id}` and check the **timestamp** of the `Status`-changing event against the script's own log line (converting BRT→UTC, +3h). Only claim proof when the timestamps correlate to the second/minute, as with `<email-redacted>@redacted.invalid` above.
**Warning signs:** A `Status: 1→2` audit event whose timestamp is *before* the earliest `reply_watch.py` log/state entry for that address (as with `<email-redacted>@redacted.invalid`).

### Pitfall 2: Trusting `intent` values logged vs. persisted as always consistent
**What goes wrong:** `docs/email-marketing/logs/reply-watch.log` shows `<email-redacted>@redacted.invalid` classified `🟢 [positive]` at `2026-09-04 16:53:29`, but the current `replies-state.json` shows the same address as `"intent": "neutral"`, and its CRM status never advanced past 1 (Contacted).
**Why it happens (most likely, not fully confirmed):** `save_state()` only writes `if not DRY`, and the `DRY` branch's `continue` skips setting `st['seen'][em]` entirely (see `main()`, the `if DRY: ...; continue` line is before the `st['seen'][em] = ...` assignment). The `16:53:29` block looks like a manual `--dry` test run on deployment day (the Scheduled Task's own `StartBoundary` is exactly `2026-09-04T08:00`), whose classifications were logged (via `log()`, which runs regardless of `DRY`) but never persisted to state or acted on — a later, real run at `17:00:04` re-processed the mailbox and produced the final (different) `neutral` state for this address, without a corresponding status change (no `Status`-changing audit event exists for this customer at all).
**How to avoid:** When writing the runbook, don't cite this address as an example — it's the one unresolved anomaly in the evidence. Cite it only as a documented, non-blocking open question.
**Warning signs:** log/state disagreement on `intent` for the same address+date, or a `seen` entry with a non-negative intent whose customer never reached status 2.

### Pitfall 3: BRT vs UTC when correlating log timestamps to audit timestamps
**What goes wrong:** Comparing `reply_watch.log`'s local timestamps directly against `audit-logs`' `timestampUtc` field without the -3h offset makes correlation look like it's off by 3 hours.
**How to avoid:** The notebook runs in America/Sao_Paulo (BRT, UTC-3) — always add 3 hours to log timestamps before comparing to `timestampUtc`.

## Code Examples

### QUAL-01's objective criterion — verbatim, ready for the runbook

```python
# Source: docs/email-marketing/reply_watch.py (commit e6532e47)
NEG = re.compile(r'n[ãa]o (tenho|temos|tem|quero|queremos|envie|enviem|mande|mandem|precis)|sem interesse|remov|descadastr|'
                 r'retir(ar|em)|pare de|parem de|unsubscribe|spam|não me envie|nao me envie', re.I)
POS = re.compile(r'pode (enviar|mandar|explicar|ligar|chamar)|tenho interesse|temos interesse|interess|or[çc]amento|'
                 r'quanto (custa|fica|é|e o valor)|valor|pre[çc]o|vamos conversar|me liga|me chama|agend|proposta|'
                 r'gostaria de|quero (saber|entender|ver)', re.I)

def classify_intent(text):
    t = (text or '')[:1500]
    if NEG.search(t): return 'negative'
    if POS.search(t): return 'positive'
    return 'neutral'
```

**QUAL-01, formally stated:** *A lead advances from Contacted to Qualified when its email reply's first 1500 characters do NOT match the `NEG` pattern above* — i.e., `classify_intent(reply_text) != 'negative'`. Both `'positive'` and `'neutral'` replies count (this is the looseness D-01 explicitly defers tightening).

### The status-advancement branch — verbatim, for the "onde/como" section

```python
# Source: docs/email-marketing/reply_watch.py, main(), lines 279-283
crm_task(H, title, desc, priority=4 if intent != 'negative' else 2)
if intent == 'negative':
    suppress(em)
elif lead.get('customer_id') and (lead.get('status') or 0) < 2:
    crm_status(H, lead['customer_id'], 2)
```

### Reproducing the audit-log cross-reference (for anyone re-verifying later)

```
GET https://api.alexandrequeiroz.com.br/api/v1/customers?search={email}
GET https://api.alexandrequeiroz.com.br/api/v1/audit-logs/resource/Customer/{id}
Authorization: Bearer {admin JWT from POST /api/v1/auth/login}
```
Both are read-only GETs against endpoints that already exist; no new tooling required.

## State of the Art

Not applicable in the usual sense (no external library/ecosystem drift to track) — this is internal, already-built automation. The one relevant "state of the art" fact: `AuditSaveChangesInterceptor` (mentioned in `CLAUDE.md`) auto-captures every `Customer` save, which is what made the D-02 proof possible without adding any new instrumentation.

## Open Questions

1. **Why did `<email-redacted>@redacted.invalid` never advance past status 1 despite a logged `positive`/`neutral` classification?**
   - What we know: the address appears once in the log (`16:53:29`, `positive`), the state file's final value is `neutral`, no `Status`-changing audit event exists for this customer ever.
   - What's unclear: whether the `16:53:29` run was a manual `--dry` test (most likely, given it's the deployment day and precedes the task's own first scheduled fire) or whether `crm_status()`'s PATCH call actually failed silently (the function only checks `r.status_code == 200` and returns a bool that `main()` never inspects — a failed PATCH would be invisible in the log).
   - Recommendation: mention as a known, non-blocking anomaly in the runbook; do not attempt to fix (D-01 boundary). If the planner wants extra certainty, could re-run `reply_watch.py --dry` once by hand and diff the classify output against a hand-copied reply body — but this is optional, not required to satisfy QUAL-01/QUAL-02.

2. **Does `crm_task()`'s Telegram/task creation reliably co-occur with `crm_status()`?**
   - What we know: both calls are in the same `main()` code path, sequentially, inside the same non-DRY branch — a successful `crm_status` call (confirmed via audit log for the listamais case) strongly implies `crm_task()` executed moments earlier in the same run.
   - What's unclear: `GET /api/v1/tasks?search=...` did not return a task matching this specific lead in this research session (the `search` query param appears to not filter as expected — it returned the same generic task list regardless of query string), so this leg wasn't independently confirmed via the Tasks API.
   - Recommendation: not needed for QUAL-01/QUAL-02 (which are only about status advancement) — safe to leave unconfirmed, optionally note in the runbook as "assumed, not independently verified via Tasks API."

## Validation Architecture

`workflow.nyquist_validation` was not found as an explicit `false` in `.planning/config.json` — treating as enabled, but this phase has no code to test (it produces zero application code changes, only a documentation file). The applicable "test" for this phase is entirely the operational evidence already gathered above.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None applicable — Python script, no test suite, verified via live production state (same as Phase 9/10) |
| Config file | none |
| Quick run command | `Get-ScheduledTask -TaskName 'DIAX-Reply-Watch'` (confirms task still Ready) |
| Full suite command | Re-run the audit-log cross-reference query in "Real Evidence Gathered §4" against a NEW `seen` entry once the mailbox receives its next real reply, to re-confirm the mechanism is still live at execute-time |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QUAL-01 | Objective criterion is documented and traceable | manual-only (doc review) | N/A — verified by reading `classify_intent` source, transcribed above | N/A |
| QUAL-02 | Status auto-advances on non-negative reply | operational/manual | `GET /api/v1/audit-logs/resource/Customer/{id}` for a known `seen` entry (see §4 reproduction steps) | ✅ endpoint exists in production today |

### Sampling Rate
- **Per task commit:** none (no code changes expected for QUAL-01/QUAL-02)
- **Per wave merge:** re-check `Get-ScheduledTaskInfo -TaskName 'DIAX-Reply-Watch'` for `LastTaskResult=0` before closing the phase
- **Phase gate:** the D-02 evidence in this document (audit-log timestamp match) stands as the human-verifiable proof; no automated test asserts it going forward

### Wave 0 Gaps
None — existing production infrastructure (Scheduled Task, audit log endpoint, state/log files) covers everything this phase needs to prove.

## Sources

### Primary (HIGH confidence)
- `docs/email-marketing/reply_watch.py` (read in full, commit `e6532e47`, plus live uncommitted diff via `git diff`)
- Live `Get-ScheduledTask`/`Get-ScheduledTaskInfo` output for `DIAX-Reply-Watch`, `n8n-watchdog`, `waha-watchdog` (2026-09-20)
- Live content of `docs/email-marketing/previews/replies-state.json` and `docs/email-marketing/logs/reply-watch.log` (2026-09-20)
- Live, read-only `GET` queries against production `api.alexandrequeiroz.com.br` — `/api/v1/customers`, `/api/v1/audit-logs/resource/Customer/{id}` — using the same auth pattern `reply_watch.py` itself uses (2026-09-20)
- `D:\claude-code\diax-crm\api-core\src\Diax.Api\Controllers\V1\AuditLogsController.cs` (confirms `resource/{resourceType}/{resourceId}` endpoint shape)
- `C:\Users\acq20\.claude\skills\financas\scripts\db.py` (confirms cross-repo direct-SQL dependency and its target DB)
- `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` (Phase 10 runbook, used as structural template)
- `.planning/phases/11-qualified-objetivo-e-autom-tico/11-CONTEXT.md`, `.planning/REQUIREMENTS.md`, `.planning/STATE.md`

### Secondary (MEDIUM confidence)
- Inference that `crm_task()`/`tg()` co-occurred with the confirmed `crm_status()` call (same code path, not independently re-verified via Tasks API search)

### Tertiary (LOW confidence)
- Root cause of the `<email-redacted>@redacted.invalid` anomaly (most likely explanation: an untracked manual `--dry` test run on deployment day) — flagged as an open question, not asserted as fact

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new stack, pure verification
- Architecture: HIGH — read `main()` in full, confirmed against live production audit trail
- Pitfalls: HIGH for Pitfalls 1 and 3 (directly observed); MEDIUM for Pitfall 2 (plausible reconstruction, not 100% certain root cause)

**Research date:** 2026-09-20
**Valid until:** Evidence is a point-in-time snapshot (Scheduled Task last-run, log/state file contents) — re-verify `LastTaskResult`/`LastRunTime` if this research is consumed more than ~7 days after 2026-09-20, since the task runs continuously and state will have moved on.
