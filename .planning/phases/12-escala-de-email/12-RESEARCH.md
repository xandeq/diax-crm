# Phase 12: Escala de Email - Research

**Researched:** 2026-09-20
**Domain:** DNS/DMARC automation (WHM API), email deliverability warmup, A/B measurement, existing Python outreach engine (diax-crm)
**Confidence:** HIGH (DMARC/WHM, volume backend limits, webmail filter, A/B code, state file) / MEDIUM (bounce-rate threshold choice, A/B winner metric — no prior art in this codebase, Claude's Discretion per CONTEXT.md)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**DMARC (D-01, D-02)**

Registro atual confirmado via DNS ao vivo: `_dmarc.alexandrequeiroz.com.br` =
`v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r`.

**Domínio está no HostGator/WHM (conta `alexa084`), NÃO no Cloudflare** — confirmado ao vivo
(`GET /zones?name=alexandrequeiroz.com.br` no Cloudflare retornou 0 resultados). Qualquer automação
de DNS usa a **API do WHM** (`WHM_HOST`/`WHM_USER`/`WHM_API_TOKEN` em `.secrets.env`, ver
`~/.claude/docs/reference-infra.md`), não a API Cloudflare que os scripts anteriores desta sessão
usaram para outros domínios.

- **D-01:** Escalona 25%→50%→75%→100%, **1 degrau por semana** (4 semanas total). Antes de subir
  cada degrau, checar relatório de bounce/spam-complaint do Brevo (a fonte já é `rua@dmarc.brevo.com`
  — usar o relatório agregado do Brevo, não reinventar parsing de DMARC aggregate report).
- **D-02:** Se um degrau disparar bounce/complaint alto, **reverte automaticamente** pro degrau
  anterior via WHM API, e avisa via Telegram (mesmo canal/padrão do watchdog da Phase 10 —
  `alerta_telegram.py` ou variante). "Alto" — definir limiar em research/planning com base no que
  o relatório Brevo já expõe (ex. bounce rate > X% ou spam complaint > Y no período do degrau).

**Volume (D-03)**

- **D-03:** Escalona 87→300/dia em **+20%/dia** (87→104→125→150→180→216→260→300, ~8 dias úteis).
  Ritmo padrão de warmup de deliverability — não é negociável pra mais rápido.

### Claude's Discretion

- **A/B de subject:** qual métrica decide vencedor (open rate vs. reply rate) e onde o resultado
  fica visível — não discutido a fundo, pesquisar o que o Brevo já expõe via API antes de decidir.
- **Leads webmail (560):** cap diário e nome da tag — não discutido a fundo, usar critério
  conservador (ex. cap pequeno tipo 10-20/dia) dado que domínio webmail tende a ter reputação pior.

### Deferred Ideas (OUT OF SCOPE)

Nenhuma — discussão ficou dentro do escopo da fase.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EMAL-01 | DMARC sobe de `pct=25` para `pct=100` | WHM `editzonerecord` call already proven working in this exact codebase (`run_gates_20260904.sh`) for this exact TXT record. Reuse pattern verbatim, drive `pct=` value + step counter from state file. |
| EMAL-02 | Volume de envio escala de 87 para 300/dia com warmup gradual | Backend send cap (`Email:DailyLimit=900`, `HourlyLimit=225` in appsettings.json) is NOT a blocker — plenty of headroom. Warmup must be implemented in `daily_waves.py`/`waves_core.py` (Python side), the only place volume is currently decided. |
| EMAL-03 | A/B de subject tem o resultado medido e reportado | Variant alternation already exists (`SUBJ_B` dict, `ab_flip` in state, campaign name suffix `sA`/`sB`). No comparison/reporting exists yet — `weekly_report()` (Friday) is the natural integration point; campaign-level `OpenCount`/`ClickCount` already tracked per campaign, groupable by `sA`/`sB` suffix in campaign name. |
| EMAL-04 | Leads "webmail" testados com cap pequeno + tag própria | Exact exclusion filter found: `FREE_WEBMAIL` set + `email_ok()` in `waves_core.py` returns `'webmail-nao-corp'` and silently drops the lead. No tagging mechanism exists yet for customers/campaigns — needs new code path. |
</phase_requirements>

## Summary

This phase extends the existing single Python outreach engine (`docs/email-marketing/daily_waves.py` +
`waves_core.py`, Phase 10's SCRP-01 consolidation) rather than building new infrastructure. Three of
the four requirements have exact, already-proven code patterns in this same codebase to reuse
verbatim; the fourth (A/B) has the alternation mechanism built but no measurement.

The most load-bearing finding is `docs/email-marketing/run_gates_20260904.sh` (lines 12-36): this
script, run live on 2026-09-04, is the **exact WHM API call** that produced the DMARC record
CONTEXT.md confirmed live today (`pct=25`). It is proof, not a guess: `dumpzone` to find the current
`Line` number of the `_dmarc.alexandrequeiroz.com.br` TXT record (never hardcode the line — the zone
can change), then `editzonerecord` with the full record (domain, line, type, name, class, ttl,
txtdata) to replace it. The same two-call pattern (locate-then-edit) is what EMAL-01's automation
must use for every step 25→50→75→100 and for the automatic revert.

The second load-bearing finding is that this project does **not** need to call Brevo's API for
bounce/spam-complaint rates — the CRM's own `email_events` table already records `Bounced` (4) and
`Spam` (5) in real time via `BrevoWebhookController`, independent of and simpler than parsing the
`rua@dmarc.brevo.com` DMARC aggregate report or hitting Brevo's `/v3/smtp/statistics` endpoints. A
subtlety to carry into planning: `EmailCampaign.UnsubscribeCount` (the aggregate counter surfaced by
`/api/v1/email-campaigns/analytics`) **conflates spam complaints and voluntary unsubscribes** — both
call `HandleOptOutAsync` → `IncrementUnsubscribe()`. To get a true spam-complaint rate, query the
`email_events` ledger directly (`event_type = 5`), not the campaign aggregate.

The third load-bearing finding is that the backend's global send cap (`EmailSettings.DailyLimit`) is
configured in production at **900/day, 225/hour** (`appsettings.json`), not the 250/day the project's
own `CLAUDE.md` module docs describe (that doc is stale). This means EMAL-02's 87→300/day ramp is
purely a Python-side scripting concern — no backend config change, migration, or deploy is required to
raise the ceiling; the ceiling is already well above the target.

**Primary recommendation:** Extend `daily_waves.py`/`waves_core.py` and `previews/daily-state.json` in
place, following the exact patterns of Phase 10/11 (additive JSON keys, module-level state, Friday-only
branches in `main()` for weekly cadence work). Reuse `run_gates_20260904.sh`'s WHM call pattern for
DMARC, query `email_events` directly for bounce/spam rate, and treat `weekly_report()` as the
integration point for A/B result reporting.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|---------------|
| `requests` | already in use (stdlib+requests, no pinned version file found) | HTTP calls to WHM API1/UAPI, CRM API, Brevo (if used) | Every script in `docs/email-marketing/` uses raw `requests`, no HTTP client abstraction exists — don't introduce one |
| stdlib `json` | 3.x (project runs Python 3.14 per `__pycache__/*.cpython-314.pyc`) | State file read/write (`previews/daily-state.json`) | `load_state()`/`save_state()` already establish this pattern; extend the same dict, don't create a parallel state file |
| stdlib `subprocess`/Bash (`run_gates_*.sh`) | n/a | One-off/manual gated DNS operations that "auto mode do Claude Code não executa" | Convention already established for this exact class of operation (DNS prod) — see `run_gates_20260904.sh` header comment |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `pyodbc` (via project's `db.py`, imported as `import db as crmdb`) | already vendored at `C:\Users\acq20\.claude\skills\financas\scripts\db.py` | Direct SQL Server queries (email_events, email_queue_items, email_campaigns) | Use for reading `email_events`/aggregating bounce/spam rate — same pattern as `sync_bounce_suppressions()`, `weekly_report()`, `fup_exclusions()` already in `daily_waves.py` |
| `curl` + WHM `json-api` | WHM API1 (not API2, not cPanel UAPI) | DNS zone read (`dumpzone`) and write (`editzonerecord`) | Confirmed working call shape below — use exactly this, do not switch to cPanel UAPI `dns_edit_zone_record` (untested in this project, different param names) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| CRM DB `email_events` for bounce/spam rate | Brevo API `/v3/smtp/statistics` or parsing `rua@dmarc.brevo.com` DMARC aggregate XML reports | Brevo API requires `BREVO_API_KEY` (present, proven working in `backfill_brevo_engagement.py`) but adds a second source of truth and a network dependency; DMARC aggregate XML parsing is explicitly what CONTEXT.md says NOT to build ("não reinventar parsing de DMARC aggregate report"). CRM DB is already real-time, already authenticated, zero new dependency. |
| WHM API1 `editzonerecord` | cPanel UAPI (`Cpanel::DNS`) | UAPI would authenticate as the cPanel user, not WHM reseller token — different auth header shape, untested in this project. WHM API1 is proven working here; don't switch APIs mid-project without cause. |
| Python `daily_waves.py` cron-piggyback for weekly DMARC check | New standalone script + new Task Scheduler entry | Adds a second scheduled task to maintain (Windows Task Scheduler is already a known fragile point — see `feedback-windows-tasks-pythonw-caminho-permanente.md` memory and OBSERVABILIDADE-watchdogs.md gap where a task silently didn't exist). Piggybacking on the existing Friday branch in `daily_waves.py main()` (already used for `weekly_report()`, `niche_refresh()`, `db_housekeeping()`) avoids a new task entirely. |

**Installation:** No new packages. `requests`, `pyodbc` (via project's `db.py`) already used throughout `docs/email-marketing/`.

**Version verification:** Not applicable — no new dependencies introduced. `requests`/`pyodbc` versions are whatever the existing venv/interpreter used by Task Scheduler already has (not repinned by this phase).

## Architecture Patterns

### Recommended Project Structure

```
docs/email-marketing/
├── daily_waves.py          # extend: Friday branch gets DMARC-step-check; volume warmup wired into build_waves()
├── waves_core.py           # extend: MAX_PER_SEG computation gains a warmup multiplier/cap
├── waves_lib.py            # pure helpers — webmail cap/tag logic likely belongs here (mirrors is_role_mailbox/is_foreign_lead)
├── dmarc_escalation.py     # NEW: dumpzone + editzonerecord + revert logic, isolated (DNS prod is a distinct blast radius from email sending)
├── previews/
│   └── daily-state.json    # extend additively: new top-level keys (dmarc_step, dmarc_step_date, warmup_day, webmail_sent_today)
└── logs/
    └── daily-waves-YYYY-MM-DD.log   # existing per-day log — DMARC step changes should log here too
```

### Pattern 1: WHM zone-record edit (locate-then-edit, never hardcode line number)
**What:** Two WHM API1 calls: `dumpzone` to find the current `Line` of a TXT record by matching
`type`+`name`+prefix of `txtdata`, then `editzonerecord` with the full record replayed and only
`txtdata` changed.
**When to use:** Every DMARC step change (25→50→75→100) and every automatic revert.
**Confidence:** HIGH — this exact call was run successfully in this exact project on 2026-09-04 and
produced the DMARC record CONTEXT.md verified live today.
**Example (from `docs/email-marketing/run_gates_20260904.sh`, lines 12-36, verbatim):**
```bash
# Source: docs/email-marketing/run_gates_20260904.sh (proven working 2026-09-04)
set -a; eval "$(grep -E '^WHM_(HOST|PORT|USER|API_TOKEN)=' "$SEC" | tr -d '\r')"; set +a
B="https://${WHM_HOST}:${WHM_PORT}/json-api"; A="Authorization: whm ${WHM_USER}:${WHM_API_TOKEN}"

# 1. locate: never trust a fixed line number, the zone can have changed
Z=$(curl -sk "$B/dumpzone?api.version=1&domain=alexandrequeiroz.com.br" -H "$A")
DMARC_LINE=$(printf '%s' "$Z" | python -c "import sys,json; r=[x for x in json.load(sys.stdin)['data']['zone'][0]['record'] if x.get('type')=='TXT' and x.get('name')=='_dmarc.alexandrequeiroz.com.br.']; print(r[0]['Line'] if r else '')")

# 2. edit: replay every field, only txtdata actually changes
curl -sk -G "$B/editzonerecord" -H "$A" --data-urlencode "api.version=1" \
  --data-urlencode "domain=alexandrequeiroz.com.br" --data-urlencode "line=$DMARC_LINE" --data-urlencode "type=TXT" \
  --data-urlencode "name=_dmarc.alexandrequeiroz.com.br." --data-urlencode "class=IN" --data-urlencode "ttl=14400" \
  --data-urlencode "txtdata=v=DMARC1; p=quarantine; pct=50; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r" \
  | python -c "import sys,json; print('DMARC:', json.load(sys.stdin)['metadata'])"
```
**Response shape:** `{"metadata":{"reason":"...","command":"editzonerecord","result":1,"version":1}}` —
`result:1` = success (mirrors `dumpzone`'s response shape, confirmed in `zone-backup-20260904.json`).
No zone serial bump was needed explicitly — WHM handles SOA serial increment internally on
`editzonerecord` (not verified from docs, inferred from the fact this call worked and the record was
live minutes later per the script's own `nslookup` verification step at line 32-34).
**Verification step already in the proven script:** `nslookup -type=TXT _dmarc.alexandrequeiroz.com.br ns232.prodns.com.br` — queries the authoritative nameserver directly, bypassing any resolver cache. Reuse this exact verification after every step change.

### Pattern 2: Additive state file (never break existing consumers)
**What:** `previews/daily-state.json` is read/written as a flat dict by `load_state()`/`save_state()`
in `daily_waves.py` — new top-level keys are safe, `dict.get()`/`dict.setdefault()` patterns already
used throughout (`st.get('ab_flip')`, `st.setdefault('crm_tasks', {})`, `st.setdefault(cfg['key'], {})`).
**When to use:** Tracking `dmarc_step` (int 0-3 or the literal pct), `dmarc_step_changed_at` (date, for
the 1-week gate), `warmup_day` (int, for the 87→300 ramp), `webmail_sent_today`/date (for EMAL-04's cap).
**Example:**
```python
# Source: pattern from daily_waves.py load_state()/save_state() (existing, lines 178-184)
st = load_state()
dmarc_step = st.get('dmarc_step', 25)          # NEW key, default = today's confirmed live value
warmup_day = st.get('warmup_day', 1)            # NEW key, day 1 of the 8-day ramp
webmail_sent = st.setdefault('webmail_sent', {})  # NEW key, {date: count}
```

### Pattern 3: Friday-only branch for weekly-cadence work (no new Task Scheduler entry)
**What:** `main()` in `daily_waves.py` already has `if datetime.date.today().weekday() == 4:` branches
for `weekly_report()`, `niche_refresh()`, and `db_housekeeping()` (Monday+Friday). DMARC's 1-week gate
fits this exact cadence.
**When to use:** DMARC step-advance check (weekly: was last change ≥7 days ago? is bounce/spam rate
within threshold? then advance, else hold or revert).
**Example:**
```python
# Source: pattern from daily_waves.py main() (existing, lines 754-762)
if datetime.date.today().weekday() == 4:   # sexta
    try:
        msg += '\n\n' + weekly_report()
    except Exception as e:
        log_line(f'weekly_report falhou: {e}')
    # NEW: DMARC step-advance check would go here, same try/except/log/tg_send shape
```

### Pattern 4: Campaign name suffix as the only current A/B grouping key
**What:** `build_waves()` already tags every campaign name with `s{variant}` (`sA`/`sB`), and
`SUBJ_B` provides the alternate subject text. `weekly_report()`'s SQL already groups by
`c.name`/`opens` — the same query pattern, filtered/grouped by the `sA`/`sB` suffix via regex on
`c.name`, is the direct path to a measured A/B result with zero new tracking infrastructure.
**When to use:** EMAL-03's "resultado medido e reportado."
**Example:**
```python
# Source: pattern from daily_waves.py weekly_report() (existing, lines 692-699), adapted
cur.execute("""
  SELECT RIGHT(c.name, 2) variant, SUM(q.status_count) sent, SUM(opens) opens, SUM(clicks) clicks
  FROM email_campaigns c ... WHERE c.name LIKE 'AQ - %' AND c.name LIKE '% auto s_'
  GROUP BY RIGHT(c.name, 2)""")
# variant is literally 'sA' or 'sB' — the last 2 chars of the name built at line 333 of daily_waves.py
```

### Anti-Patterns to Avoid
- **Parsing DMARC aggregate XML reports from `rua@dmarc.brevo.com`:** CONTEXT.md explicitly forbids
  this ("usar o relatório agregado do Brevo, não reinventar parsing de DMARC aggregate report") — the
  `rua=` address is Brevo's own inbox, and Brevo's dashboard/API already aggregates it. Query the
  CRM's own `email_events` table instead (simpler, already real-time, zero new integration).
- **Hardcoding the WHM zone `Line` number for `_dmarc...`:** The zone changes over time (new records
  added by other subdomains/services — the zone dump shows 164 lines already, spanning 5 other
  hosted domains). Always `dumpzone` first, locate by `type`+`name`, then edit.
- **Reading `EmailCampaign.UnsubscribeCount` as a spam-complaint proxy:** It conflates spam AND
  voluntary unsubscribe (both call `HandleOptOutAsync` → `IncrementUnsubscribe()` in
  `BrevoWebhookController.cs`). Query `email_events WHERE event_type = 5` (Spam) for the true signal.
- **Raising the backend `Email:DailyLimit`/`HourlyLimit` in `appsettings.json`:** Not needed — already
  900/day, 225/hour, well above the 300/day target. Touching this file is out of scope for this phase
  and would require a deploy for no benefit.
- **Registering a new Windows Task Scheduler entry for a weekly DMARC check:** Task Scheduler is a
  known fragile point in this project (a watchdog task was silently missing until Phase 10 caught it
  live — see `OBSERVABILIDADE-watchdogs.md` §2). Piggyback on the existing Friday branch in
  `daily_waves.py main()` instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bounce/spam-complaint rate per DMARC step window | Custom IMAP/DMARC-XML parser reading `rua@dmarc.brevo.com` | SQL query against `email_events` (`event_type IN (4,5)`) scoped to the step's date window | Already real-time via webhook, already authenticated, zero new integration; CONTEXT.md explicitly forbids reinventing DMARC aggregate parsing |
| DNS zone record edit | Any Cloudflare-pattern code (this session's earlier scripts used Cloudflare for other domains) | WHM API1 `dumpzone`+`editzonerecord`, proven in `run_gates_20260904.sh` | Domain confirmed NOT on Cloudflare (0 zones) — Cloudflare code would silently no-op or 404, not fail loudly |
| Telegram alert on DMARC revert | New Telegram bot/webhook wiring | `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` already in `.secrets.env`, already used by `daily_waves.py`'s own `tg_send()` (module-local, no import needed) or `alerta_telegram.py`'s `enviar()` | Same credentials, same channel, already working in production for both daily wave summaries and watchdog alerts |
| Volume ramp state tracking | New database table/migration | Extend `previews/daily-state.json` with new top-level keys | JSON state file is the established pattern for exactly this kind of "where are we in a multi-day process" state (`cursor`, `fup_sent`, `ab_flip` already there) |

**Key insight:** Every piece of infrastructure this phase needs (DNS API auth, bounce/spam data, alert
channel, state persistence, weekly cadence hook, A/B grouping key) already exists somewhere in this
codebase from a prior phase or a prior ad-hoc session. The work is almost entirely wiring, not
building — the risk is in NOT finding these existing patterns and duplicating them under a new name.

## Common Pitfalls

### Pitfall 1: Treating the WHM zone `Line` number as stable across the 4-week escalation
**What goes wrong:** A script that reads the `Line` number once (e.g., at plan/deploy time) and
hardcodes it for all 4 weekly steps will silently edit the wrong record if the zone changes in the
meantime (other DNS work happens on this same zone — SPF was edited in the same session, and 5 other
subdomains live in this zone per `zone-backup-20260904.json`).
**Why it happens:** WHM's zone file is a flat, line-numbered text format; any edit (by this project or
manually via WHM UI) shifts subsequent line numbers.
**How to avoid:** Every step (advance or revert) must re-run `dumpzone` and re-locate the line by
`type=TXT`+`name=_dmarc.alexandrequeiroz.com.br.` immediately before calling `editzonerecord`, exactly
as `run_gates_20260904.sh` already does. Never persist a line number in state.
**Warning signs:** `editzonerecord` returns `result:1` (success) but the subsequent `nslookup`
verification shows the OLD `pct=` value, or shows a completely different record was overwritten.

### Pitfall 2: Advancing the DMARC step without a real "days since last change" check surviving script failures
**What goes wrong:** If the "was it ≥7 days ago" check derives from re-reading the DMARC record's
`pct=` value via DNS and inferring "must have been N weeks since some earlier baseline," a single
missed Friday run (PC asleep — already a documented recurring problem in this project, see
`daily_waves.py`'s own `compute_wave_slots` late-start handling) either advances too early or gets
permanently stuck never advancing.
**Why it happens:** DNS itself has no memory of "when did this value last change" — that must be
tracked in the CRM's own state.
**How to avoid:** Store `dmarc_step_changed_at` (an explicit date) in `previews/daily-state.json` on
every successful advance/revert, and gate the next advance on `(today - dmarc_step_changed_at).days >= 7`,
never on inferring elapsed time from the DNS value itself.
**Warning signs:** DMARC step advances on two consecutive Fridays, or never advances despite weeks of
clean bounce/complaint data.

### Pitfall 3: Reading `EmailCampaign.UnsubscribeCount` as the spam-complaint signal for the revert threshold
**What goes wrong:** A revert-threshold check built against the campaign aggregate
(`/api/v1/email-campaigns/analytics`'s `TotalUnsubscribes`) will trigger reverts on ordinary,
harmless unsubscribes, not just spam complaints — inflating the apparent "problem" rate and reverting
DMARC steps that were actually fine.
**Why it happens:** `BrevoWebhookController.HandleOptOutAsync` increments the same
`campaign.IncrementUnsubscribe()` counter for both `reason="spam"` and `reason="unsubscribed"`
(only `hard_bounce` is excluded from this counter) — see lines 464-514 of `BrevoWebhookController.cs`.
The ledger (`email_events`) DOES distinguish them (`EmailEventType.Spam=5` vs `Unsubscribed=6`), but
the pre-built aggregate API does not.
**How to avoid:** Query `email_events` directly with `event_type = 5` for spam complaints and
`event_type = 4` for bounces, scoped to the DMARC step's date window (`occurred_at` between step-start
and now). Do not use the campaign-level `/analytics` endpoint for this specific threshold check.
**Warning signs:** Revert triggers even though the Brevo dashboard shows near-zero spam complaints for
the period.

### Pitfall 4: Volume warmup competing with the existing `already_queued_today() >= 50` guard
**What goes wrong:** `build_waves()` already aborts the whole day's waves if 50+ items are already
queued (`guard = already_queued_today(); if guard >= 50: ... return []`). Once the warmup ramp passes
day 3 (125/day target), a naive implementation that just raises `MAX_PER_SEG`/segment count without
also raising this guard will self-sabotage — the existing guard will silently cut waves short well
before the day's target volume is reached, and there is no error, just a quiet "pulando waves" log line.
**Why it happens:** The guard was designed as an anti-runaway safety net when the baseline was 87/day,
not tuned for a moving 87→300 target.
**How to avoid:** The 50-guard threshold must itself become a function of `warmup_day`'s target volume
(e.g., `guard_threshold = today's warmup target`), not a fixed constant, OR be raised to a value above
300 (the ramp's final ceiling) with a comment explaining why, since its real purpose (catch runaway
duplicate scheduling) doesn't need to scale with the warmup.
**Warning signs:** Daily Telegram summary consistently under-delivers vs. the day's warmup target,
with a "GUARD: N itens já agendados hoje — pulando waves" line in the log.

### Pitfall 5: Webmail cap/tag competing with the existing hard exclusion in `email_ok()`
**What goes wrong:** `waves_core.py`'s `email_ok()` returns `False, 'webmail-nao-corp'` for any
`FREE_WEBMAIL` domain, and `fetch_leads()` silently drops any lead that fails `email_ok()`. A naive
"add webmail leads with a cap" implementation that tries to route them through the existing
`fetch_leads()`/`fetch_multi()` path will find they are already filtered out upstream and never reach
the cap logic at all.
**Why it happens:** The exclusion is a hard boolean gate inside the shared lead-fetch function used by
every niche/segment, not a separate filter step that can be bypassed downstream.
**How to avoid:** EMAL-04 needs either (a) a parallel fetch path that explicitly queries for
`FREE_WEBMAIL`-domain leads (inverting the current filter) with its own small cap and a distinct tag,
separate from the main niche rotation, or (b) a parameter added to `email_ok()`/`fetch_leads()` that
allows webmail through when explicitly requested by a dedicated webmail-segment caller. Both are
viable; (a) is lower-risk (doesn't touch the shared function's default behavior for the 20 existing
niches).
**Warning signs:** A "webmail" segment configured in `COPY_BANK`/`ROTATION` returns 0 leads every day
despite CONTEXT.md's "560 leads webmail hoje ignorados" figure.

## Code Examples

### DMARC record edit (WHM, proven)
```bash
# Source: docs/email-marketing/run_gates_20260904.sh (this exact codebase, run 2026-09-04)
# See Pattern 1 above for full annotated version.
```

### Real bounce/spam rate for a date window (CRM DB, not Brevo API)
```python
# Source: adapted from daily_waves.py's own DB access pattern (db as crmdb, crmdb.connect())
def dmarc_step_health(since_date):
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT
        SUM(CASE WHEN event_type=4 THEN 1 ELSE 0 END) bounces,
        SUM(CASE WHEN event_type=5 THEN 1 ELSE 0 END) spam,
        (SELECT COUNT(*) FROM email_queue_items WHERE status=2 AND sent_at >= ?) sent
      FROM email_events WHERE occurred_at >= ?""", since_date, since_date)
    r = cur.fetchone(); cx.close()
    sent = r.sent or 1
    return {'bounce_rate': (r.bounces or 0) / sent, 'spam_rate': (r.spam or 0) / sent, 'sent': sent}
```
(Table/column names `email_events.event_type`/`occurred_at`, `email_queue_items.status`/`sent_at`
confirmed from `daily_waves.py`'s existing `sync_bounce_suppressions()` and `weekly_report()` queries.)

### A/B variant already produced, just needs grouping in a query
```python
# Source: daily_waves.py build_waves() (existing, lines 310-313, 332-335)
variant = 'B' if st.get('ab_flip') else 'A'
st['ab_flip'] = not st.get('ab_flip')
...
json={'name': f'AQ - {cfg["seg"]} - {cfg["service"]} - {today} auto s{variant}', ...}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| Cloudflare API for DNS (used earlier this session for other domains) | WHM API1 (`json-api`) for this specific domain | Confirmed 2026-09-20 (this session's live check: 0 Cloudflare zones for alexandrequeiroz.com.br) | Any DMARC/DNS automation code for THIS domain must use WHM, not the Cloudflare snippets in `~/.claude/docs/reference-infra.md`'s Cloudflare section |
| `CLAUDE.md`'s documented "50/hour, 250/day" email worker cap | Live `appsettings.json`: `DailyLimit: 900`, `HourlyLimit: 225` | Unknown exact date, but confirmed live 2026-09-20 | `CLAUDE.md`'s module description is stale; do not plan around it. The 300/day EMAL-02 target has ~3x headroom already in production config |
| 48 `fire_*.py` scripts | Single `daily_waves.py`/`waves_core.py` engine (Phase 10, SCRP-01) | 2026-09-19 | All Phase 12 work extends this single engine; there is no other entry point for outreach volume/segmentation |

**Deprecated/outdated:**
- `archive/fire_*.py` (48 files): explicitly archived with a warning README in Phase 10 — never edit
  or import these for Phase 12 work, even though some contain older DMARC/SPF-adjacent references.

## Open Questions

1. **What exact bounce-rate / spam-complaint-rate threshold should trigger an automatic revert?**
   - What we know: `email_events` gives exact counts per step window; typical deliverability industry
     guidance (not verified against this project's own history) treats bounce rate > 2-5% or spam
     complaint rate > 0.1% as red flags, but this project has no prior documented threshold.
   - What's unclear: Whether the sample size at 87-300/day/step is large enough for a percentage
     threshold to be statistically meaningful over a few days, vs. needing an absolute-count threshold
     (e.g., "3+ spam complaints in the step window" rather than a rate).
   - Recommendation: Planner should pick a conservative absolute+rate combined threshold (e.g., "revert
     if spam complaints ≥ 3 OR spam rate > 0.3% OR bounce rate > 5% in the current step's window") and
     document it explicitly in the runbook this phase produces (mirroring `QUALIFIED-avanco-automatico.md`'s
     pattern of stating the exact criterion with a citation). This is Claude's Discretion territory per
     CONTEXT.md ("Alto" — definir limiar em research/planning).

2. **A/B winner metric: open rate or reply rate?**
   - What we know: Open events are tracked (`EmailEventType.Opened`) but per this project's own prior
     decision (`BrevoWebhookController.cs` comment, line 358-362: "Abertura de email não conta (Apple
     MPP torna opens não confiáveis)"), open rate is explicitly distrusted elsewhere in this same
     codebase for advancing funnel status. Click rate and reply detection (`scan_replies()` in
     `daily_waves.py`, IMAP-based) are the two signals this project already trusts.
   - What's unclear: Whether click rate (fast, per-campaign, already aggregated) or reply rate (slower,
     already scanned via IMAP but not currently grouped by A/B variant) is the better subject-line
     winner signal — a subject line's job is to get the email opened/read, which click/reply don't
     directly measure (a good subject can precede a weak body).
   - Recommendation: Given this project's own precedent of distrusting opens, use **click rate** (not
     open rate) as the primary A/B metric — it's already aggregated per campaign, already has the
     `sA`/`sB` grouping key, and needs no new tracking. Report reply rate as a secondary/informational
     signal since `scan_replies()` already runs daily. This directly follows the project's own stated
     reasoning, not external best-practice assumptions.

3. **Webmail leads: is "560" a live, re-derivable number or a one-time count from outside this session?**
   - What we know: The exact filter that produces this exclusion (`FREE_WEBMAIL` set +
     `email_ok()`'s `'webmail-nao-corp'` reason) is located and confirmed. The number 560 itself was
     not reproduced from a live query in this research pass (not in scope of files read).
   - What's unclear: Whether 560 is current or was counted at CONTEXT.md's gathering time
     (2026-09-20) — likely the latter, meaning it's accurate as of context-gathering but the planner
     should treat it as an estimate, not re-verify precisely, since the exact count will shift daily
     as new leads are imported/contacted regardless.
   - Recommendation: Plan should query the CRM leads endpoint filtered by `FREE_WEBMAIL` domains as a
     verification task, not assume 560 is still exact at execution time.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None — `docs/email-marketing/` is plain Python with no pytest/unittest suite. Verification is manual/operational (dry-run flags, live DNS checks, log inspection), matching Phase 10/11 precedent. |
| Config file | none |
| Quick run command | `python daily_waves.py --dry` (existing — prints the day's plan without sending; does NOT exercise DMARC logic, which would need its own `--dry` flag added) |
| Full suite command | none (no automated suite) — closest existing precedent is `docs/email-marketing/waves_smoke.py` (5 import/sanity checks, not functional tests) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EMAL-01 | DMARC step advances only after ≥7 days AND clean bounce/spam window | manual/live-DNS | `nslookup -type=TXT _dmarc.alexandrequeiroz.com.br ns232.prodns.com.br` (proven pattern, `run_gates_20260904.sh` line 33) | ❌ Wave 0 — no script wraps this yet |
| EMAL-01 | Automatic revert on bad window + Telegram alert | manual (force a bad-window state, observe revert + message) | none automated; would need a `--simulate-bad-window` flag on the new `dmarc_escalation.py` | ❌ Wave 0 |
| EMAL-02 | Daily volume matches warmup_day's target (±segment granularity) | manual | `python daily_waves.py --dry` inspection of `DRY: plano = [...]` log line, cross-checked against `warmup_day` in state | ❌ Wave 0 — dry-run doesn't currently print the day's volume TARGET, only the plan built |
| EMAL-03 | A/B click-rate comparison appears in the Friday report | manual | Trigger `weekly_report()`-equivalent query manually against dev/staging data, or wait for next live Friday run | ❌ Wave 0 — no isolated test for the new grouping query |
| EMAL-04 | Webmail leads get a distinct tag + respect their own cap | manual | Query `/api/v1/leads?search=...` for `FREE_WEBMAIL` domains after a dry-run to confirm cap not exceeded | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `python daily_waves.py --dry` (existing dry-run) + manual DNS/DB spot-check for whichever sub-feature was touched.
- **Per wave merge:** Full manual walkthrough of the runbook this phase should produce (mirroring `QUALIFIED-avanco-automatico.md`/`OBSERVABILIDADE-watchdogs.md` format), with live evidence citations (audit log rows, DNS query output, Telegram message screenshots) — this project's established verification standard (see STATE.md's "human-verified" pattern across Phases 9-11).
- **Phase gate:** Live DMARC record check + one full daily_waves.py live run (not --dry) showing the day's actual queued volume before `/gsd:verify-work`.

### Wave 0 Gaps
- [ ] `dmarc_escalation.py` needs a `--dry`/`--simulate` mode analogous to `daily_waves.py --dry`, since a real DNS write is not something to test against production repeatedly.
- [ ] No existing query/script computes "today's target volume" from `warmup_day` — needs to exist before EMAL-02 can be dry-run-verified at all.
- [ ] No existing query groups campaign stats by `sA`/`sB` suffix — needs to exist before EMAL-03 can be verified.

## Sources

### Primary (HIGH confidence — read directly from this codebase, live-verified in this session or a documented prior session)
- `D:\claude-code\diax-crm\docs\email-marketing\run_gates_20260904.sh` — the exact, proven WHM `dumpzone`/`editzonerecord` call for this domain's DMARC/SPF records
- `D:\claude-code\diax-crm\docs\email-marketing\zone-backup-20260904.json` — full zone dump proving the `Line`-based record model and confirming no other DMARC-adjacent records collide
- `D:\claude-code\diax-crm\docs\email-marketing\daily_waves.py` — full read; state file shape, volume/segment logic, A/B alternation, Friday-cadence pattern, webmail-adjacent role/foreign filters
- `D:\claude-code\diax-crm\docs\email-marketing\waves_core.py` — full read; `FREE_WEBMAIL`, `email_ok()`, `PER_PROVIDER`, `HEALTHY`, `fetch_leads()`
- `D:\claude-code\diax-crm\api-core\src\Diax.Api\Controllers\V1\BrevoWebhookController.cs` — full read; confirms `email_events` ledger distinguishes Spam(5)/Bounced(4)/Unsubscribed(6), and that the campaign aggregate `UnsubscribeCount` conflates spam+unsubscribe
- `D:\claude-code\diax-crm\api-core\src\Diax.Domain\EmailMarketing\Enums\EmailEventType.cs` — exact enum values (Sent=0, Delivered=1, Opened=2, Clicked=3, Bounced=4, Spam=5, Unsubscribed=6, Failed=7)
- `D:\claude-code\diax-crm\api-core\src\Diax.Infrastructure\Email\EmailSettings.cs` + `appsettings.json` (lines 124-132, 252-267) — confirms production `DailyLimit=900`/`HourlyLimit=225`, well above the 300/day EMAL-02 target
- `D:\claude-code\n8n-local\alerta_telegram.py` — full read; existing Telegram alert pattern (scoped to `waha`/`n8n` targets via argparse choices, would need extension or bypass for a `dmarc`/`volume` target)
- `D:\claude-code\diax-crm\docs\email-marketing\OTIMIZACAO-FUNIL-2026-09-03.md` (lines 23-94) — documents the DMARC/SPF deliverability plan that `run_gates_20260904.sh` executed, including the intended `pct=25 → pct=100` follow-up mentioned but not yet implemented
- `D:\claude-code\diax-crm\.planning\phases\12-escala-de-email\12-CONTEXT.md` — locked decisions, live-confirmed DMARC state
- `C:\Users\acq20\.claude\docs\reference-infra.md` — WHM auth pattern (`WHM_HOST`/`WHM_PORT`/`WHM_USER`/`WHM_API_TOKEN`), confirms Cloudflare is a different domain-set entirely

### Secondary (MEDIUM confidence)
- `D:\claude-code\diax-crm\docs\email-marketing\backfill_brevo_engagement.py` — confirms `BREVO_API_KEY` works and `https://api.brevo.com/v3/smtp/statistics/events` is reachable, as a fallback path if the CRM-DB-first approach proves insufficient (not needed per current recommendation)
- `D:\claude-code\diax-crm\docs\email-marketing\OBSERVABILIDADE-watchdogs.md` — runbook format precedent, and the specific "Task Scheduler entries can silently not exist" pitfall this phase should avoid repeating

### Tertiary (LOW confidence)
- General deliverability-industry bounce/spam-rate thresholds (2-5% bounce, 0.1% complaint) — not sourced from any doc in this project, standard industry folklore, should be treated as a starting point for the planner's own threshold decision, not fact.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries, every pattern reuses code already proven in this exact repo
- Architecture (WHM DMARC edit): HIGH — the exact call was run and its result is the live state CONTEXT.md confirmed
- Architecture (volume warmup integration point): HIGH — `MAX_PER_SEG`/`already_queued_today()` mechanics fully read and understood; MEDIUM on whether the guard-vs-warmup interaction (Pitfall 4) is exactly how the planner will choose to resolve it
- Bounce/spam threshold, A/B winner metric: MEDIUM — reasoned from this project's own prior stated distrust of open-rate and its existing data model, but no prior documented threshold/metric decision exists in this codebase to cite directly (Claude's Discretion per CONTEXT.md)
- Webmail cap/tag exact numbers (560, cap size): LOW on the specific "560" figure being current at execution time; HIGH on the filter mechanism location

**Research date:** 2026-09-20
**Valid until:** ~14 days for the WHM/DMARC mechanics and backend config values (stable infra, low churn); ~7 days for the "560 webmail leads" figure and any live bounce/spam baseline numbers (these shift daily with new imports/sends)
