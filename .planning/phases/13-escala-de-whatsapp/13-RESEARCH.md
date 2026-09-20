# Phase 13: Escala de WhatsApp - Research

**Researched:** 2026-09-20
**Domain:** n8n workflow orchestration + WAHA (WhatsApp HTTP API) + DIAX CRM integration endpoint (.NET) + Python ops scripts (docs/email-marketing)
**Confidence:** HIGH (all four requirements verified against live, running systems — n8n's own SQLite DB, WAHA config, CRM source code, and the production DB — not against documentation alone)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Relatório semanal (WHAT-03, único gray area discutido)**
- **D-01:** Entrega via **Telegram** (mesmo canal já usado pelo watchdog/DMARC — consistência,
  reusar `alerta_telegram.py`/padrão de mensagem já estabelecido).
- **D-02:** Sai **sexta à tarde**, mesmo padrão de cadência do A/B de email (Fase 12) — fecha a
  semana.

### Claude's Discretion (não discutido a fundo — usar bom senso, documentar decisão tomada)

- **WHAT-01** (confirmar handoff n8n→WAHA): fase de verificação — igual ao padrão da Phase 11
  (QUAL-01/02 já existiam, só precisou provar). Pesquisar se `wa-outreach-1a1.json`
  (`n8n-local/workflows/local/`) já roda de verdade antes de assumir que precisa construir algo.
- **WHAT-02** (bot desvia pra humano): critério de "é prospecção" vs atendimento normal fica a
  critério de implementação — pesquisar como o bot atual (`8nKTlX4Y7Hy1EUEn` mencionado no
  REQUIREMENTS.md) diferencia contexto hoje antes de decidir a heurística.
- **WHAT-04** (escalar wa_es→wa_br): critério de "taxa de resposta validada" e ritmo de escala
  ficam a critério de implementação — pesquisar que dado de resposta já existe pro segmento `wa_es`
  antes de definir o limiar (por analogia ao que foi feito com bounce rate no DMARC, Fase 12).

### Deferred Ideas (OUT OF SCOPE)

- Nenhuma — discussão ficou dentro do escopo da fase.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WHAT-01 | Handoff (`n8n-wa-outreach-1a1.json`) confirmado em execução pela sessão que opera o WAHA local | Live query of n8n's `~/.n8n/database.sqlite` (`workflow_entity` table) proves the workflow (`waOutreach1a1AQ`) is currently **`active=0`** and has only ever sent **1** message (test). This is the opposite of Phase 11 — the automation is NOT yet actually running. See "Live System State" and Pitfall 1. |
| WHAT-02 | Bot inbound de IA (`8nKTlX4Y7Hy1EUEn`) desvia lead de prospecção pra humano em vez de responder | Read the full workflow JSON (`waha-bot-atendente.json`) and confirmed live in the same SQLite DB: the bot is `active=1` and **already implements** the exact deflection behavior via a `crmLeads` staticData set (currently 1,057 entries) that routes inbound messages from those chat_ids away from the AI reply path into a CRM webhook + owner notification. See "Live System State" and Architecture Pattern 1. |
| WHAT-03 | Relatório semanal de métricas do canal WhatsApp (enviados/respondidos/opt-out) | Confirmed **no existing script** covers WhatsApp (both `weekly_report()` in `daily_waves.py` and `funnel_analysis.py` are email-only, zero WhatsApp references). Genuinely new work. Template pattern + reusable pieces (`db.py` connection, `alerta_telegram.enviar()`, `task_items` table, DDD-based segment logic) documented below. |
| WHAT-04 | Escalar de `wa_es` (480 leads) para `wa_br` (1138) após validar taxa de resposta em ES | Confirmed segments are computed **live at query time** by DDD (27/28 = `wa_es`), not stored config. No per-channel reply-rate table exists (unlike `email_events` for email) — replies surface only as `task_items` rows + `Customer.Status`. Live numbers pulled from production DB give a concrete existing baseline (12 wa_es / 5 wa_br ever sent, 8 total WhatsApp-reply tasks system-wide). See "Live System State" and Code Example 3. |
</phase_requirements>

## Summary

This phase is **not a green-field build** — three of the four requirements sit on top of automation
that is either fully built (WHAT-02) or fully built-but-switched-off (WHAT-01), and the fourth
(WHAT-04) is blocked on real send volume that doesn't exist yet because WHAT-01 isn't running. Only
WHAT-03 (weekly report) is genuinely new code. This mirrors the discovery pattern of Phase 10/11 in
this same project (automation already exists, the job is to prove/activate/measure it, not
reinvent it) — but with an important twist versus Phase 11: for WHAT-01 the live check came back
**false** (`active=0`), so this phase must actually turn something on and prove it fires for real,
not just document what's already true.

Live inspection of n8n's own SQLite database (`~/.n8n/database.sqlite`, `workflow_entity` table —
the actual source of truth for "is this running", not the exported JSON files in
`n8n-local/workflows/`) found: the outreach workflow `waOutreach1a1AQ` is **inactive** and has sent
exactly one test message ever; the inbound bot `8nKTlX4Y7Hy1EUEn` is **active** and already contains
a fully-wired "don't respond with AI, hand off to CRM + notify owner" branch, currently populated
with 1,057 chat_ids (populated once, manually, by `restaurar-tudo.py` — not kept in sync
automatically). Neither of these facts is visible from reading the exported JSON files or the docs
alone; both required querying the live database.

A second major finding: **DIAX CRM has two unrelated WhatsApp integrations in the codebase.**
`CLAUDE.md` documents "WhatsApp: Integrated via Evolution API" (`WhatsAppController` +
`EvolutionApiClient` + `OutreachService.SendWhatsApp*`) — this is real code, but its
`EvolutionApi.ApiKey` is an **empty string** in `appsettings.json` and it points at a different,
unrelated hosted instance. It is unused/dead for the purposes of this phase. The actual live
pipeline for WHAT-01..04 is **WAHA (local Docker, port 3000) ↔ n8n-local (port 5678) ↔
`IntegrationsController.WhatsAppEvent`** (`POST /api/v1/integrations/whatsapp-event`, authenticated
by `X-Integration-Key`, not JWT). Do not route any Phase 13 work through `WhatsAppController` —
that would touch the wrong, disconnected system.

**Primary recommendation:** Treat WHAT-01 as "activate + prove" (not "verify, already true"), treat
WHAT-02 as "verify + close the sync gap" (the AI-deflection logic already works, but nothing keeps
`crmLeads` current), build WHAT-03 as new code following the `weekly_report()` pattern with its own
Friday-afternoon schedule (not piggybacked on `daily_waves.py`'s Friday-morning branch), and expect
WHAT-04 to land `Partial` in this same execution (like EMAL-01/02/04 in Phase 12) because there is
not yet enough real `wa_es` send volume post-activation to honestly validate a response rate within
one planning/execution session — the phase should define the threshold and the query, and check it
against whatever real data exists at verification time.

## Live System State (verified 2026-09-20)

This is load-bearing for planning and cannot be inferred from the JSON files in
`n8n-local/workflows/` alone — the exported files do not reliably carry the live `active` flag or
runtime `staticData` (e.g. `waha-bot-atendente.json`'s exported JSON has no top-level `"active"` key
at all; the real value lives only in the n8n SQLite DB).

**How to query it (reuse this exact pattern — already used by `restaurar-tudo.py`/`deploy-bot.py`):**
```python
import sqlite3, json, os
DB = os.path.expanduser('~/.n8n/database.sqlite')   # C:\Users\acq20\.n8n\database.sqlite
cur = sqlite3.connect(DB).cursor()
cur.execute("select id, name, active from workflow_entity where id in ('waOutreach1a1AQ','8nKTlX4Y7Hy1EUEn')")
print(cur.fetchall())
```

| Workflow | id | `active` (live DB) | staticData (live DB) |
|---|---|---|---|
| WA Outreach 1-a-1 (DIAX leads) | `waOutreach1a1AQ` | **0 (inactive)** | `{sent: 1}` — one test send only |
| WAHA Bot - Atendente IA [local] | `8nKTlX4Y7Hy1EUEn` | **1 (active)** | `{hist:20, leads:18, crmLeads:1057, amigos:5, grupos:1, pausados:1, ...}` |

n8n process is running locally (multiple `node.exe` processes observed; port 5678 is the n8n API/
webhook host per `n8n-local/.env`'s `N8N_PORT=5678`).

**CRM production DB — existing WhatsApp send/reply history (before this phase, from ad-hoc use, not
from the 1-a-1 workflow):**
```sql
-- 17 customers total have ever received a WhatsApp message (whats_app_sent_count > 0)
-- Segment split (DDD 27/28 = wa_es, else wa_br): wa_es=12, wa_br=5
-- Status distribution: 13 Contacted(1), 4 Qualified(2)
-- whats_app_opt_out=1: 0
-- task_items with title LIKE '%WhatsApp respondeu%': 8 (system-wide, all-time)
```
This is a real (if small and pre-automation) baseline: **8 replies logged against 17 sends** ≈ 47%
historical reply rate, entirely from manual/ad-hoc contact before the 1-a-1 cron existed. Treat it
as directional context, not a validated rate for WHAT-04 — it predates the formal outreach cadence
and safety rules (cap 20/day, 1-a-1, check-exists) that `wa-outreach-1a1.json` is meant to enforce.

## Architecture Patterns

### Pattern 1: `crmLeads` deflection set inside the bot's `staticData` (WHAT-02 — already built)
**What:** The bot workflow (`8nKTlX4Y7Hy1EUEn`, node `Guard`, a single big Code node) keeps an
in-memory-but-DB-persisted (`workflow_entity.staticData`, survives restarts) map `s.crmLeads =
{chatId: 1, ...}`. A dedicated webhook event (`{"event":"crmleads","payload":{"chatIds":[...],
"reset":true}}`, same `POST http://localhost:5678/webhook/waha-bot` endpoint used for every other
bot control action) replaces or merges this set.

**When it fires:** On every inbound WhatsApp message, before any LLM call, the Guard code checks
`if (s.crmLeads && s.crmLeads[chatId])`. If true, it **never builds an LLM prompt** — it short-circuits
straight to:
```js
// n8n-local/workflows/local/waha-bot-atendente.json — node "Guard", lines ~319-326
if (s.crmLeads && s.crmLeads[chatId]) {
  if (fromMe) return [{ json: motivo('crm_lead_propria') }];
  if (!texto) return [{ json: motivo('crm_lead_midia') }];
  const OPT = /(nao|n[aã]o|pare|parar|remover|sair|descadastrar|n[aã]o tenho interesse|sem interesse)/i;
  const ev = OPT.test(texto) ? 'optout' : 'reply';
  return [{ json: { prosseguir:false, crmLead:true, crmEvent:ev, chatId, phone: chatId.split('@')[0], texto, nome } }];
}
```
Downstream (`IF "CRM lead?"` → `HTTP POST` → `HTTP POST`), this branch does two things instead of
replying with AI:
1. `POST /api/v1/integrations/whatsapp-event` with `event: reply|optout` (X-Integration-Key auth) —
   the CRM's `IntegrationsController.WhatsAppEvent` creates an **Urgent** `TaskItem`
   (`Title: "💬 WhatsApp respondeu: {name}"`) and calls `RegisterEngagement()` (advances
   Lead/Contacted → Qualified) or `OptOutWhatsApp()`.
2. A node named **"CRM: avisa dono"** sends a WAHA `sendText` **to the owner's own WhatsApp number**
   (`5527996302718@c.us`) with the lead's message — this is the "hand off to human" notification.
   It is a WhatsApp self-DM, **not Telegram**, even though the HANDOFF doc's prose says "encaminhar
   p/ Telegram". Confirm with the user whether to leave as-is (already works, zero-cost) or add a
   real Telegram alert here too for consistency with WHAT-03's channel choice — this is Claude's
   Discretion, not blocking, since the requirement text ("desvia lead... pra humano em vez de
   responder") is satisfied either way.

**Gap to close (this is the real WHAT-02 work, not building AI routing from scratch):** `crmLeads`
was populated **once**, manually, by `restaurar-tudo.py` reading
`docs/email-marketing/previews/whatsapp-leads.json` and POSTing a `crmleads` event with
`reset: True`. Nothing re-runs this automatically. Currently 1,057 chat_ids are loaded — this number
will drift from reality as `export_whatsapp_leads.py` is re-run and new leads enter the wa_es/wa_br
cohorts, or as the 1-a-1 outreach workflow sends to new leads not yet in the set. Any lead contacted
after the last `crmleads` sync will get an **AI reply instead of a deflect** if they respond — this
directly contradicts WHAT-02's requirement. The fix belongs in one of two places (pick one, document
the choice):
  - (a) Have `wa-outreach-1a1.json`'s "CRM: marcar enviado" step (or a new step right after
    `sendText`) POST a `crmleads` event for that one `chat_id` (incremental, no `reset`) immediately
    after every outbound send — cheapest, keeps the set exactly in sync with who's actually been
    contacted.
  - (b) Add a scheduled re-sync (reuse `restaurar-tudo.py`'s `crmleads` step, run standalone,
    scheduled) that reloads the full export on a cadence.
  (a) is recommended: it's a 1-node addition to an already-built workflow and it's event-driven
  (no staleness window), whereas (b) has a "sent but not yet synced" gap during the day.

### Pattern 2: CRM is authoritative, n8n is just transport (both workflows)
**What:** Both workflows treat DIAX CRM as the source of truth via one shared, narrow contract:
`POST /api/v1/integrations/whatsapp-event` with `{customerId|phone, event: sent|reply|optout|failed,
text?, messageId?}`, authenticated with `X-Integration-Key` (config key
`Integrations:WhatsAppEventKey`, falls back to `Integrations:SendEmailKey` if unset — confirmed in
`IntegrationsController.cs` lines 301-304). This is intentionally the same pattern as the email
pipeline's CRM-write-back. `docs/email-marketing/wa_event.py` is a ready-made CLI wrapper for this
endpoint (`python wa_event.py <customerId|phone> sent|reply|optout|failed ["texto"]`) — reuse it for
any manual testing/backfill in this phase rather than hand-rolling `requests.post` calls.

### Recommended Project Structure (WHAT-03 — new work)
```
docs/email-marketing/
├── whatsapp_weekly_report.py   # NEW — mirrors weekly_report() pattern from daily_waves.py
├── export_whatsapp_leads.py    # EXISTING — segment source of truth (wa_es/wa_br DDD logic), reuse ES_DDD constant
├── wa_event.py                 # EXISTING — CRM write-back CLI, reuse for testing
└── previews/
    └── whatsapp-leads.json     # EXISTING export, regenerate before any crmLeads resync
```
A new Windows Scheduled Task (Friday afternoon, e.g. 17:00 BRT — distinct from `DIAX-Email-Waves-Daily`'s 08:40 BRT morning trigger) should call this new script directly. Do **not** bolt it onto
`daily_waves.py`'s existing `if datetime.date.today().weekday() == 4:` branch (see Pitfall 3).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Sending Telegram message for the weekly report | New Telegram HTTP client / bot token handling | `alerta_telegram.py`'s low-level `enviar(msg)` function (`from alerta_telegram import enviar`) | Already reads `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` from `~/.claude/.secrets.env`, already has retry/logging. **Do not use `transicao()`** from the same module — that function is specifically for up/down state-transition alerts (watchdogs) and expects a `caminho_estado` JSON file tracking previous state; a weekly metrics report is not a state transition. |
| Determining wa_es vs wa_br for a customer | New DDD parsing logic | The exact normalization in `export_whatsapp_leads.py::normalize_br()` + `ES_DDD = {'27','28'}` | This is the canonical segment definition already used to produce the 480/1138 numbers in REQUIREMENTS.md — any parallel implementation risks drifting from it (e.g. different phone field precedence between `whats_app` and `phone`). |
| Writing to the CRM after a WhatsApp send/reply/optout | Raw SQL UPDATE from Python, or a new endpoint | `POST /api/v1/integrations/whatsapp-event` via `wa_event.py` or the existing n8n HTTP Request nodes | Endpoint already handles task creation, status advancement (`RegisterEngagement`), opt-out, and is unit-tested (`WhatsAppEventIntegrationTests.cs`). A raw SQL fallback exists in `HANDOFF-whatsapp-leads.md` §5 only for when the API is down — not a first choice. |
| Querying "is this n8n workflow actually running" | Assuming the exported JSON's absence/presence of `"active"` reflects truth | Direct SQLite query against `~/.n8n/database.sqlite`'s `workflow_entity.active` column (see Live System State above) | Confirmed the exported `waha-bot-atendente.json` file has **no** `"active"` key at all, while `wa-outreach-1a1.json` has one (`false`) that may or may not match the live DB after manual toggling in the n8n UI. The DB is the only reliable source. |

**Key insight:** almost everything WHAT-01/02/04 need already has a plumbing layer (n8n webhook
event router, CRM integration endpoint, segment export script). The actual gaps are narrow:
(1) flip a boolean and prove it fires, (2) close a sync gap in an existing set, (3) write one new
report script, (4) define+run one query against data that barely exists yet.

## Common Pitfalls

### Pitfall 1: Confusing "workflow JSON exists in the repo" with "workflow is running"
**What goes wrong:** Treating WHAT-01 as already-satisfied because `wa-outreach-1a1.json` is a
complete, well-built workflow definition sitting in `n8n-local/workflows/local/`.
**Why it happens:** The file has `"active": false` baked into its own export, and multiple docs
(`HANDOFF-whatsapp-leads.md`) describe it as "pronto p/ importar" (ready to import) — past tense
language that reads like it's done. It's imported; it isn't running.
**How to avoid:** Query the live n8n DB (see Live System State) before writing verification steps.
Plan must include an explicit activation step (toggle `active=1`, either via n8n UI or
`UPDATE workflow_entity SET active=1 WHERE id='waOutreach1a1AQ'` following the same direct-SQLite
pattern `restaurar-tudo.py` already uses) plus proof of at least one real cron-triggered send (not
just the CAP=1 manual test already recorded in `staticData.sent`).
**Warning signs:** Any verification step that reads only the exported JSON file, not the live DB.

### Pitfall 2: The workflow's own CAP is hardcoded to 1, not the documented 20/day
**What goes wrong:** Assuming `wa-outreach-1a1.json` already enforces the "Cap 20 msgs/dia" rule
from `HANDOFF-whatsapp-leads.md` §2.
**Why it happens:** The workflow's "Filtrar P1-P3 + cap" node hardcodes
`const MAX_PRIO = 3, CAP = 1;` with a comment `// CAP 20/dia, só P1-P3 (mude MAX_PRIO p/ 4 depois de
medir)` — the comment describes the *intended* production value, the code still has the *test*
value (1).
**How to avoid:** Before activating (Pitfall 1), the plan must explicitly change `CAP` from `1` to
`20` (per the documented safety rule) in the "Filtrar P1-P3 + cap" node — otherwise activating the
cron will only ever send one message per run, which will look like "it's running" while silently
not doing the actual job, and will make WHAT-04's response-rate math (denominator) wrong.
**Warning signs:** Verification that only checks "did the cron fire" without checking how many
messages it actually queued per run.

### Pitfall 3: Piggybacking the weekly WhatsApp report on `daily_waves.py`'s Friday branch
**What goes wrong:** Because D-02 says "mesmo padrão de cadência do A/B de email (Fase 12)", it's
tempting to add the WhatsApp report inside `daily_waves.py`'s existing
`if datetime.date.today().weekday() == 4:` block (where `weekly_report()` for email already lives).
**Why it happens:** Reuse instinct, and superficially it is "the Friday report."
**How to avoid:** `daily_waves.py` runs on the `DIAX-Email-Waves-Daily` scheduled task at **08:40
BRT — morning**, not afternoon. CONTEXT.md's D-02 explicitly says "sexta à **tarde**" (Friday
**afternoon**). Bolting onto the morning run would violate the locked decision. Build the WhatsApp
weekly report as its own script with its own Friday-afternoon Scheduled Task (e.g., 17:00 BRT),
independent of `daily_waves.py`.
**Warning signs:** A plan task that edits `daily_waves.py` for WHAT-03 instead of creating a new
file + new Scheduled Task.

### Pitfall 4: Attributing `Customer.Status` == Qualified to WhatsApp when it could be from email
**What goes wrong:** Using `Status = Qualified` counts as a proxy for "WhatsApp response rate" per
segment.
**Why it happens:** `RegisterEngagement()` (the method that flips Lead/Contacted → Qualified) is
called from **both** the email click path and the WhatsApp reply path (`IntegrationsController`
`case "reply"`), and the `Customer.Status` enum has no channel dimension.
**How to avoid:** For WHAT-03/04 metrics, do not query `Status` alone. Use the discrete,
channel-specific signal instead: `task_items` rows whose `Title` matches
`'💬 WhatsApp respondeu:%'` (this title is only ever written by the WhatsApp reply branch — see
Code Example 3), joined to `customers` for segment/opt-out state. Document this explicitly since
it's a non-obvious join, not a documented "reply log" table.
**Warning signs:** Any SQL for this phase that queries `customers.status = 2` without also touching
`task_items`.

### Pitfall 5: Assuming the two "WhatsApp" mentions in the codebase are the same system
**What goes wrong:** Reading `CLAUDE.md`'s "WhatsApp: Integrated via Evolution API" and routing new
work through `WhatsAppController`/`EvolutionApiClient`/`OutreachService.SendWhatsApp*`.
**Why it happens:** It's the only WhatsApp mention in the project's own `CLAUDE.md` "Modules at a
Glance" table, and it's real, compiling C# code — nothing marks it as unused.
**How to avoid:** Confirmed via `appsettings.json`: `EvolutionApi.ApiKey` is an **empty string**
(`""`), pointing at `https://n8n-evolution-evolution-api.h8tqhp.easypanel.host` — a separate,
unconfigured, legacy integration. All of WHAT-01..04's actual infrastructure (WAHA local Docker +
n8n-local + `IntegrationsController.WhatsAppEvent`) is unrelated to this controller. Do not touch
`WhatsAppController.cs`/`EvolutionApiClient.cs`/`OutreachService.SendWhatsApp*` in this phase.
Consider (optional, non-blocking) flagging the stale `CLAUDE.md` line for a follow-up doc fix, the
same way Phase 12 fixed a ROADMAP/REQUIREMENTS contradiction — but this is not itself a phase
requirement.
**Warning signs:** Any task referencing `WhatsAppController`, `EvolutionApiClient`, `ready-leads`,
`send-campaign`, or `send-followup` endpoints.

## Code Examples

### 1. Check/toggle n8n workflow active state (WHAT-01)
```python
# Source: n8n-local/restaurar-tudo.py + deploy-bot.py (existing pattern in this repo)
import sqlite3, json, os
DB = os.path.expanduser('~/.n8n/database.sqlite')
db = sqlite3.connect(DB)
# read
print(db.execute("select id, name, active from workflow_entity where id='waOutreach1a1AQ'").fetchone())
# activate (only after fixing CAP — Pitfall 2)
db.execute("update workflow_entity set active=1 where id='waOutreach1a1AQ'")
db.commit(); db.close()
```
Prefer activating via the n8n UI (`http://localhost:5678`) if a human is present — it triggers n8n's
internal scheduler registration correctly. Only use the raw SQL path if scripting unattended
(n8n may need a restart/reload to pick up an externally-toggled `active` flag — verify by checking
`workflow_entity.active` again after n8n's next poll, or by watching for the next scheduled
execution in n8n's UI/`execution_entity` table).

### 2. Keep `crmLeads` in sync incrementally (WHAT-02 gap — Pattern 1)
```js
// New node to add right after "CRM: marcar enviado" in n8n-local/workflows/local/wa-outreach-1a1.json
// POST to the bot's own webhook, event=crmleads, incremental (no reset)
{
  "method": "POST",
  "url": "http://localhost:5678/webhook/waha-bot",
  "jsonBody": "={{ JSON.stringify({event:'crmleads', payload:{chatIds:[$('Um por vez').item.json.chat_id]}}) }}"
}
```
This keeps every lead the outreach workflow actually messages permanently in the deflect-to-human
set, with no dependency on re-running `restaurar-tudo.py` or `export_whatsapp_leads.py`.

### 3. Weekly WhatsApp metrics query (WHAT-03/04 — channel-specific, not `Status`-based)
```python
# Reuse the same db.py connection pattern as export_whatsapp_leads.py
import sys, re, datetime as dt
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db as crmdb
ES_DDD = {'27', '28'}  # same constant as export_whatsapp_leads.py — do not redefine independently

cx = crmdb.connect(); cur = cx.cursor()
cur.execute("""
  SELECT c.id, c.phone, c.whats_app, c.whats_app_opt_out, c.last_whats_app_sent_at
  FROM customers c WHERE c.last_whats_app_sent_at >= DATEADD(day,-7,SYSUTCDATETIME())""")
sent_rows = cur.fetchall()
cur.execute("""
  SELECT t.customer_id, t.created_at FROM task_items t
  WHERE t.title LIKE N'%WhatsApp respondeu%' AND t.created_at >= DATEADD(day,-7,SYSUTCDATETIME())""")
reply_rows = cur.fetchall()
cur.execute("""
  SELECT COUNT(*) FROM customers WHERE whats_app_opt_out=1 AND updated_at >= DATEADD(day,-7,SYSUTCDATETIME())""")
optouts_7d = cur.fetchone()[0]   # approximation via AuditableEntity.UpdatedAt — no dedicated opt-out timestamp exists
cx.close()
```
Confirm exact column/table casing (`task_items`, `customers.updated_at`) against the live schema
before implementing — verified present in production DB as of this research (`task_items` table
exists; DIAX's `DiaxDbContext` auto-converts PascalCase → snake_case per `CLAUDE.md`).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| WhatsApp via Evolution API (`WhatsAppController`) | WhatsApp via WAHA + n8n-local | Some point before 2026-09-05 (`HANDOFF-whatsapp-leads.md` dated 05/09/2026 already describes WAHA as the live system and calls the Evolution-based `ready-leads` endpoint "regra antiga") | Evolution API path is dead code with an empty API key; all new WhatsApp automation work (this phase included) must target WAHA/n8n, never `WhatsAppController` |
| Manual/ad-hoc WhatsApp contact (the 17 customers with `whats_app_sent_count>0` today) | Structured 1-a-1 cadence via `wa-outreach-1a1.json` (cap, jitter, check-exists, dedupe) | Designed 05/09/2026, still not activated as of this research (2026-09-20) | WHAT-04's response-rate baseline is thin and pre-dates the safety rails; expect the real, rule-following send volume to still be small even after activation |

## Open Questions

1. **Should the "avisa dono" notification for deflected leads move to Telegram, to match WHAT-03's channel choice (D-01)?**
   - What we know: it currently works via a WAHA `sendText` to the owner's own WhatsApp number, which does satisfy the literal requirement text ("desvia... pra humano em vez de responder").
   - What's unclear: `HANDOFF-whatsapp-leads.md` §5's prose ("encaminhar p/ Telegram") suggests Telegram was the original intent, but the shipped code does WhatsApp self-DM instead.
   - Recommendation: Leave as-is unless the user asks for consistency — it's Claude's Discretion per CONTEXT.md, and changing a live, working node carries some risk relative to the benefit. Flag the decision in the phase's runbook either way.

2. **What response-rate threshold and minimum sample size should gate wa_es→wa_br (WHAT-04)?**
   - What we know: Phase 12 set precedent with `MIN_CLICKS=30` for the email A/B test and a DMARC
     bounce-rate reversal threshold of >5%. There is no existing WhatsApp-specific precedent.
   - What's unclear: With only 12 historical `wa_es` sends (pre-automation) and the outreach workflow
     not yet activated, there may not be enough real volume to reach statistical confidence within
     this phase's execution window — same situation Phase 12 hit with EMAL-01/02/04.
   - Recommendation: Define the query/threshold as a documented, config-driven decision function
     (mirroring `dmarc_escalation.py`'s `decide()` pattern from Phase 12) but expect this requirement
     to close `Partial` (mechanism built and provable, real-degree escalation pending more send
     volume) rather than `Complete` in the same session — this should be surfaced as a likely outcome
     during planning, not treated as a planning failure if it happens.

3. **Does n8n need a restart to pick up an externally-toggled `active` flag in `workflow_entity`?**
   - What we know: `restaurar-tudo.py`/`deploy-bot.py` write directly to this table for `staticData`
     without apparent issues while n8n is running.
   - What's unclear: whether `active` specifically is cached in n8n's in-memory scheduler registry
     and requires either the n8n UI toggle (which goes through the API, not raw SQL) or a service
     restart to actually start firing the cron trigger.
   - Recommendation: Prefer toggling via the n8n UI at `http://localhost:5678` for this one field;
     verify via `execution_entity` (or the UI's execution log) that a real scheduled run happened,
     not just that the DB row says `active=1`.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (api-core/tests) for the one piece of this phase that touches C# (`IntegrationsController.WhatsAppEvent`, already covered); everything else (n8n workflow activation, `crmLeads` sync, weekly report script, escalation query) is ops/config work verified by live proof, not unit tests, matching Phases 10-12's pattern in this project |
| Config file | `api-core/api-core.sln` (dotnet test); no test framework exists for the Python scripts in `docs/email-marketing/` (none of the sibling scripts — `export_whatsapp_leads.py`, `daily_waves.py`, `ab_report.py` — have unit tests either; they use `--dry`/`--selftest` CLI flags for self-verification instead) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~WhatsAppEventIntegrationTests"` (existing, covers sent/reply/optout/failed contract — run this if touching `IntegrationsController.WhatsAppEvent` at all) |
| Full suite command | `cd api-core && dotnet test -c Release` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WHAT-01 | Outreach workflow fires on schedule and sends real messages within the cap | manual/live-proof | Query `workflow_entity.active` + `execution_entity` in `~/.n8n/database.sqlite` after the next scheduled trigger; cross-check `customers.last_whats_app_sent_at` incremented | ❌ no automated test possible (external cron-driven side effect); follow Phase 10/11's live-proof pattern instead |
| WHAT-02 | Bot deflects a crmLeads-listed chat instead of replying with AI, and creates a CRM task | unit (existing) + live-proof (sync gap) | `dotnet test --filter WhatsAppEventIntegrationTests` for the CRM-side contract; live: confirm `crmLeads` count matches current `whatsapp-leads.json` export count | ✅ `WhatsAppEventIntegrationTests.cs` exists for the CRM half; ❌ no test exists (or is possible) for the n8n Guard-node branch logic itself — verify by sending a real/simulated WAHA webhook payload for a known crmLeads chat_id and observing no AI reply is sent |
| WHAT-03 | Weekly report shows enviados/respondidos/opt-out and is delivered via Telegram Friday afternoon | manual/live-proof | Run new script with a `--dry` flag (follow `daily_waves.py`/`ab_report.py` convention) against production DB, inspect printed numbers before wiring the Scheduled Task | ❌ Wave 0 — script doesn't exist yet |
| WHAT-04 | Escalation decision function queries wa_es response rate against a documented threshold | manual/live-proof | Run the decision function in dry-run mode (mirror `dmarc_escalation.py --drill`/`decide()` pattern), inspect output against the documented threshold | ❌ Wave 0 — decision function doesn't exist yet |

### Sampling Rate
- **Per task commit:** run the relevant `--dry`/`--selftest` flag for whichever script was touched; `dotnet test --filter WhatsAppEventIntegrationTests` if `IntegrationsController` was touched.
- **Per wave merge:** `cd api-core && dotnet test -c Release` (only if any C# was touched — WHAT-01/02/03/04 as scoped here mostly do not touch C#, except the optional Pitfall 5 doc fix).
- **Phase gate:** Live proof captured in a runbook (same pattern as `QUALIFIED-avanco-automatico.md`, `DMARC-escalonamento.md`, `ESCALA-EMAIL-runbook.md`) before `/gsd:verify-work`.

### Wave 0 Gaps
- [ ] `docs/email-marketing/whatsapp_weekly_report.py` — does not exist, needed for WHAT-03.
- [ ] A WhatsApp escalation decision script/function (wa_es→wa_br) — does not exist, needed for WHAT-04; can mirror `dmarc_escalation.py`'s `decide()` shape (pure function, evaluated before any write, `--apply` gate) rather than reusing its code directly (different domain).
- [ ] No test framework exists for `docs/email-marketing/*.py` beyond `--dry`/`--selftest` conventions — continue that convention rather than introducing pytest for just this phase (consistent with Phases 10-12).

## Sources

### Primary (HIGH confidence — live systems inspected directly)
- `~/.n8n/database.sqlite` (`workflow_entity` table) — queried live for `active`/`staticData` of both workflows in scope.
- `D:\claude-code\diax-crm\.planning\phases\13-escala-de-whatsapp\13-CONTEXT.md` — locked decisions and discretion areas.
- `D:\claude-code\diax-crm\.planning\REQUIREMENTS.md` — WHAT-01..04 exact wording, bot/segment IDs.
- `D:\claude-code\diax-crm\.planning\STATE.md` and `.planning\ROADMAP.md` — Phase 12 precedent for `Partial` closure pattern, decision-function shape (`dmarc_escalation.py`).
- `D:\claude-code\n8n-local\workflows\local\wa-outreach-1a1.json` — full workflow definition read in full.
- `D:\claude-code\n8n-local\workflows\local\waha-bot-atendente.json` — full workflow definition read in full (822 lines), including the `crmLeads` Guard logic and CRM/avisa-dono branch.
- `D:\claude-code\n8n-local\docker-compose.yml`, `.env`, `n8n.env` — WAHA/n8n local infra config.
- `D:\claude-code\n8n-local\restaurar-tudo.py`, `deploy-bot.py` — canonical pattern for querying/writing n8n's live SQLite state.
- `D:\claude-code\diax-crm\docs\email-marketing\HANDOFF-whatsapp-leads.md`, `export_whatsapp_leads.py`, `wa_event.py`, `reply_watch.py`, `daily_waves.py` (`weekly_report()`), `funnel_analysis.py` — all read in full or targeted-grepped.
- `D:\claude-code\diax-crm\api-core\src\Diax.Api\Controllers\V1\IntegrationsController.cs` (`WhatsAppEvent` action, lines 289-399) and `WhatsAppController.cs` — read in full/relevant sections.
- `D:\claude-code\diax-crm\api-core\src\Diax.Domain\Customers\Customer.cs` (`RegisterContact`, `RegisterEngagement`, `RegisterWhatsAppSent`, `OptOutWhatsApp`) — read directly.
- `D:\claude-code\diax-crm\api-core\src\Diax.Api\appsettings.json` (`EvolutionApi` section, confirming empty `ApiKey`).
- Live production DB query (`db.py` connection, same module used by `export_whatsapp_leads.py`) — counted 17 customers with WhatsApp sends, segment split, status distribution, opt-out count, and `task_items` reply count.
- `D:\claude-code\diax-crm\api-core\tests\Diax.Tests\Application\Integrations\WhatsAppEventIntegrationTests.cs` — confirmed existing test coverage (file exists, not fully read).
- `D:\claude-code\diax-crm\CLAUDE.md` — confirmed the "WhatsApp: Integrated via Evolution API" line and cross-checked it against live config (found stale/misleading for this phase's scope).

No WebSearch/Context7 sources were used — this phase's domain is entirely internal, project-specific infrastructure (a private n8n instance, a private WAHA session, and this project's own CRM code), not a public library/framework where external docs would add value.

## Metadata

**Confidence breakdown:**
- WHAT-01 finding (inactive, CAP=1): HIGH — directly queried from n8n's live DB and the workflow's own code.
- WHAT-02 finding (already built, sync gap): HIGH — directly read the full workflow JSON and the CRM controller code, cross-checked live staticData.
- WHAT-03 finding (genuinely new): HIGH — grepped all plausible existing scripts for WhatsApp coverage, found none.
- WHAT-04 finding (thin baseline, no per-channel reply log): HIGH — directly queried production DB for existing counts; MEDIUM on the exact recommended threshold, since no precedent exists for WhatsApp specifically (documented as Open Question 2).
- Evolution API vs WAHA discrepancy: HIGH — confirmed via source code (`WhatsAppController`, `EvolutionApiClient`) and config (`appsettings.json` empty `ApiKey`), not inference.

**Research date:** 2026-09-20
**Valid until:** Short shelf life for the "Live System State" numbers specifically (staticData counts, active flags, and DB counts will change the moment anyone touches these systems — treat those as a snapshot, not a fact to re-cite later without re-querying). The architectural findings (Patterns, Don't Hand-Roll, Pitfalls, Evolution API dead-code finding) are stable for the life of this milestone (~30 days).
