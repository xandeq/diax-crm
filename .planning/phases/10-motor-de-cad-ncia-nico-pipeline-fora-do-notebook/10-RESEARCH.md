# Phase 10: Motor de Cadência Único + Pipeline Fora do Notebook - Research

**Researched:** 2026-09-19
**Domain:** Python script consolidation (dead-code archival + live-import extraction) + lightweight infra health-check/alerting
**Confidence:** HIGH (all findings verified by reading the actual code, grepping the actual tree, and inspecting live Windows Task Scheduler state — no library/framework unknowns in this phase)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Escopo real do SCRP-01 (revisado após descoberta em discuss-phase)**

`daily_waves.py` **já é** o motor real — roda via Task Scheduler seg-sex 08:40 BRT, com follow-up
multi-toque (FUP1/2/3), circuit breaker, filtro de lead estrangeiro/role-mailbox, digest de
hot-leads no Telegram. Isso NÃO é reescrito do zero.

- **D-01:** `daily_waves.py` continua sendo o motor. O trabalho é extrair as funções/constantes que
  ele hoje importa de `fire_today_20.py` (`DIR`, `base`, `HEALTHY`, `PER_PROVIDER`, `ts`,
  `load_env`, `login`, `gen_image`, `fetch_leads`, `assign_providers`, `ensure_breaker_closed`),
  `fire_w1_2707.py` (`build_html_v2`, `SEGMENTS`) e `fire_w2w3_2707.py` (`WAVES`) para dentro de
  `waves_lib.py` (ou um módulo novo, ex. `waves_core.py`, se a mistura de responsabilidades pedir).
- **D-02:** Depois da extração, `fire_today_20.py`, `fire_w1_2707.py` e `fire_w2w3_2707.py` (e os
  outros ~76 scripts `fire_*.py`/`fire_w*.py` sem nenhum import ativo de `daily_waves.py`) são
  movidos para `docs/email-marketing/archive/` — não deletados, ficam acessíveis pra consulta de
  histórico de campanha antiga.
- **D-03:** Antes de mover qualquer arquivo, confirmar por `grep -rn "from fire_\|import fire_"` em
  todo `docs/email-marketing/` que nenhum outro script além de `daily_waves.py` depende deles — a
  extração precisa cobrir 100% dos imports reais antes do arquivamento, não só os 3 já identificados
  nesta sessão.

**Escopo real do OBSV-01 (revisado — sem migração de infra)**

- **D-04:** Não migra nada de máquina. WAHA, n8n e o Task Scheduler continuam rodando no notebook do
  usuário. O que este phase entrega é **alerta**, não eliminação do ponto único de falha.
- **D-05:** Alerta dispara especificamente quando **WAHA ou n8n estão fora do ar** — não quando o
  Task Scheduler atrasa (isso já tem tolerância própria em `waves_lib.compute_wave_slots`) nem
  quando `daily_waves.py` roda mas envia 0 emails (fora de escopo desta fase, considerado
  aceitável/já coberto por outro sinal). Canal: o bot Telegram que já existe
  (`TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` em `~/.claude/.secrets.env`, já usado por
  `opportunity_detector` e pelo skill `dealhunter`) — reusar, não criar bot novo.
- **D-06:** Verificação de saúde: WAHA via `GET http://127.0.0.1:3000/api/health` (ou endpoint
  equivalente do WAHA — confirmar na pesquisa) checando o container `waha` já mapeado em
  `waha-hub/docker-compose.yml`; n8n via `N8N_WEB_URL`/API do n8n já em `.secrets.env`. Rodar como
  job periódico (mesma família de agendamento que já existe — Task Scheduler local, dado que D-04
  não migra infra).

### Claude's Discretion
- Nome exato do módulo novo (se `waves_lib.py` crescer demais, criar um segundo módulo é aceitável)
- Frequência exata do health-check (ex. a cada 15-30min é razoável; não foi discutido em detalhe)
- Formato exato da mensagem de alerta no Telegram

### Deferred Ideas (OUT OF SCOPE)
- Migrar waves/vigia/n8n/WAHA pra rodar numa VPS (eliminar o ponto único de falha de vez) — considerar se o alerta mostrar que cai com frequência
- Alertar também em "rodou mas 0 emails enviados" (circuit breaker aberto) — fora de escopo desta fase por decisão explícita do usuário
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SCRP-01 | Motor único parametrizado substitui os 79+ scripts `fire_*.py` soltos em `docs/email-marketing/` | Full dependency graph mapped below (45 live-import files, not 3); exact extraction targets confirmed live in `daily_waves.py`; confirmed all 3 source files are import-safe (`__main__`-guarded) |
| OBSV-01 | Pipeline de outreach deixa de depender só do notebook via alerta quando WAHA/n8n caem | Confirmed WAHA endpoint, confirmed which n8n instance is actually in scope (there are TWO — see critical correction below), found 3 existing local reference implementations to copy the pattern from instead of hand-rolling |

</phase_requirements>

## Summary

`daily_waves.py` (793 lines) is genuinely the production engine — it runs daily via Task Scheduler,
builds waves, runs 3-stage follow-ups, syncs hot leads to CRM tasks, and does Friday housekeeping.
It imports 10 names from `fire_today_20.py`, 2 from `fire_w1_2707.py`, and 1 from `fire_w2w3_2707.py`,
exactly as CONTEXT.md states. Critically, **`SEGMENTS` and `WAVES` are not legacy leftovers — they
are the live data source for 12 of the 20 niches in `daily_waves.py`'s `ROTATION` list** (logistica,
arquitetura, estudio, medico, engenharia, advocacia, escola, marketing, moveis, academia, otica,
pousada), harvested into `COPY_BANK` at import time via `_harvest()`. `build_html_v2` is the actual
HTML template used for every campaign sent today, not a historical artifact. All three source files
guard their side-effecting code behind `if __name__ == '__main__':`, so extraction is a pure
copy-paste/refactor with no landmine of code executing on import.

The `docs/email-marketing/` dependency graph is much larger than the "3 files" identified in
discuss-phase: **45 files** import from `fire_today_20.py`, not 3. Most (~40) are the numbered
one-off campaign scripts (`fire_w1_21.py`...`fire_w11_20.py`, `fire_w1_24.py`, etc.) that are
themselves candidates for archival — but **6 files are NOT named `fire_*` and are NOT dead**:
`debug_campaign.py`, `import_extrator_bridge.py`, `replenish_leads.py` (called live from
`daily_waves.py` itself every Friday), `rescrape_21.py`, `rescrape_27.py`, `rescrape_tavily_27.py`,
`scrape_and_import.py`. A naive "archive every `fire_*.py`" pass would correctly leave these 6 alone
(they're not fire-named), but if `fire_today_20.py` itself gets archived, all 6 break because they do
`from fire_today_20 import load_env, login, base, ts` — a name overlap with the exact set D-01 already
plans to extract to the new module. This must be handled explicitly (see Common Pitfalls).

For OBSV-01, the research surfaces a **correction to D-06**: `N8N_WEB_URL` in `.secrets.env` points to
a cloud-hosted n8n on EasyPanel (`https://n8n-n8n.h8tqhp.easypanel.host`), used by VNG/other projects
— it is **not** the local n8n relevant to DIAX's WhatsApp pipeline. The local n8n (the one WAHA's
inbound webhook hits, `host.docker.internal:5678/webhook/waha-bot`) has no `.secrets.env` entry; its
address is hardcoded as `localhost:5678` in `n8n-local/n8n.env` and its scripts. More importantly,
**self-healing watchdogs for both WAHA and local n8n already exist and are already scheduled**
(`n8n-local/waha-watchdog.py`, `n8n-local/watchdog-n8n.ps1`, Task Scheduler task `n8n-watchdog` is
`Running`) — but neither sends a Telegram alert on failure (WAHA's watchdog alerts via WAHA's own
WhatsApp `sendText`, a chicken-and-egg problem if the session itself is dead; n8n's watchdog only logs
locally). A third, more directly reusable reference exists: `_scripts/vps-watchdog.py`, registered as
the live Task Scheduler task `VPS-Watchdog` (every 5 min, `pythonw.exe`), which already implements the
exact pattern D-05/D-06 ask for — transition-based Telegram alerts (only alerts on state *change*,
not every poll, to avoid spam) reading `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` directly from
`~/.claude/.secrets.env`. This is the pattern to copy, not the ad-hoc `tg_send()` inside
`daily_waves.py`.

**Primary recommendation:** Treat this phase as two independent, low-risk mechanical changes: (1) a
careful extract-then-archive refactor of `docs/email-marketing/` guided by the full 45-file import
graph below, not the original 3-file list, with special handling for the 6 non-`fire_*`-named
consumers; and (2) a new watchdog script cloned from `_scripts/vps-watchdog.py`'s transition-alert
pattern, checking local WAHA (`127.0.0.1:3000`) and local n8n (`localhost:5678`) — explicitly *not*
the cloud n8n in `.secrets.env` — registered as a new Task Scheduler task the same way `VPS-Watchdog`
is (`pythonw.exe` direct invocation, no vbs wrapper needed).

## Full Dependency Graph (supersedes the "3 files" in CONTEXT.md)

`grep -rn "from fire_\|import fire_" docs/email-marketing/` (all `.py` files) found:

### Files that import from `fire_today_20.py` (45 total)

**The 3 already identified (extraction targets, D-01):**
- `daily_waves.py` — `DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login, gen_image, fetch_leads, assign_providers, ensure_breaker_closed` (top-level) + `import fire_today_20 as ft` again inside `refresh_healthy()` (line 235) to mutate `ft.HEALTHY[:] = healthy` in place
- `fire_w1_2707.py` — same 10 names (it's itself an extraction source, and also a consumer)
- `fire_w2w3_2707.py` — same 10 names + `from fire_w1_2707 import build_html_v2`

**Dead-end campaign scripts — safe to archive as a batch (40 files), never imported by anything else:**
`fire_w10_20.py`, `fire_w11_20.py`, `fire_w1_21.py`, `fire_w1_24.py`, `fire_w1_25.py`, `fire_w1_27.py`,
`fire_w2_21.py`, `fire_w2_24.py`, `fire_w2_25.py`, `fire_w2_27.py`, `fire_w3_21.py`, `fire_w3_24.py`,
`fire_w3_25.py`, `fire_w3_27.py`, `fire_w4_20.py`, `fire_w4_21.py`, `fire_w4_24.py`, `fire_w4_25.py`,
`fire_w4_27.py`, `fire_w5_20.py`, `fire_w5_21.py`, `fire_w5_24.py`, `fire_w5_25.py`, `fire_w5_27.py`,
`fire_w6_20.py`, `fire_w6_21.py`, `fire_w6_24.py`, `fire_w6_25.py`, `fire_w6_27.py`, `fire_w7_20.py`,
`fire_w7_24.py`, `fire_w7_25.py`, `fire_w7_27.py`, `fire_w8_20.py`, `fire_w8_24.py`, `fire_w8_25.py`,
`fire_w8_27.py`, `fire_w9_20.py` (confirmed: none of these names appear as an import target anywhere
in the tree — no one imports *from* them).

**The 6-file landmine — NOT `fire_*`-named, NOT dead, would break silently if `fire_today_20.py` is archived without care:**
| File | Imports from `fire_today_20` | Still active? |
|------|------------------------------|----------------|
| `debug_campaign.py` | `import fire_today_20 as ft` (module alias, broader use) | Manual debug tool, low risk but grep it for exact usage before touching |
| `import_extrator_bridge.py` | `load_env, login, base` | STATE.md: "fallback/paralelo" to `ExtractorPullWorker` — not scheduled, but not deleted either |
| `replenish_leads.py` | `load_env, login, base, ts` | **LIVE** — called dynamically by `daily_waves.py` line 779 (`import replenish_leads; replenish_leads.run(cap=120)`) every Friday when stock runway < 120 |
| `rescrape_21.py` | `load_env, login, base, ts` | Ad-hoc rescrape utility, not scheduled |
| `rescrape_27.py` | `load_env, login, base, ts` | Ad-hoc rescrape utility, not scheduled |
| `rescrape_tavily_27.py` | `load_env, login, base, ts` | Ad-hoc rescrape utility, not scheduled |
| `scrape_and_import.py` | `load_env, login, base, ts` | Ad-hoc import utility, not scheduled |

**Implication:** every one of the 6 names these files need (`load_env, login, base, ts`) is already
inside D-01's extraction list. Once extracted to the new module, these 6 files' import lines must be
updated to point there too (e.g. `from waves_core import load_env, login, base, ts`) — OR
`fire_today_20.py` can be left behind as a **1-line re-export shim** instead of being physically moved,
which avoids touching 6 extra files at the cost of one file staying outside `archive/`. Both are valid;
recommend the shim only if the planner wants to minimize file-touch count in this phase — otherwise
updating the 6 imports is the "clean" option D-02 seems to intend (archive folder is genuinely just
dead campaigns).

## Architecture: What Extraction Actually Means

### Why the extraction is not cosmetic

`daily_waves.py`'s `COPY_BANK` (its niche/copy database) is built at **import time**:
```python
def _harvest():
    bank = {}
    for cfg in SEG_W1: bank[cfg['seg']] = cfg      # from fire_w1_2707.SEGMENTS
    for w in W23:
        for cfg in w['segments']: bank[cfg['seg']] = cfg   # from fire_w2w3_2707.WAVES
    return bank

COPY_BANK = _harvest()
COPY_BANK.update({ 'clinica': {...}, 'loja': {...}, ... })   # 8 more niches defined inline
```
`ROTATION` has 20 niches; 8 are defined inline in `daily_waves.py`, **12 exist only inside `SEGMENTS`
(4 niches) and `WAVES` (8 niches across its 2 waves)**. If extraction drops a field or changes dict
shape, `_harvest()` won't crash (dict access is lenient) — it will silently produce broken/incomplete
copy for those 12 niches the next time they're due to fire. Verification must diff `COPY_BANK.keys()`
before/after against the full `ROTATION` list, not just "does it import."

`build_html_v2` (from `fire_w1_2707.py`) is called directly at `daily_waves.py` line 331 for **every
campaign sent today** — it is not sample/reference code, it is the current production template
function.

### Cross-module mutable-state gotcha (must preserve exactly)

`daily_waves.refresh_healthy()` does:
```python
import fire_today_20 as ft
...
ft.HEALTHY[:] = healthy   # in-place slice-assign — same list object `HEALTHY` bound in daily_waves
```
This works today because `from fire_today_20 import (..., HEALTHY, ...)` at the top of `daily_waves.py`
binds the *same list object* that `ft.HEALTHY[:] = ...` mutates in place (slice assignment, not
rebinding). `assign_providers()` (defined inside `fire_today_20.py`) does `HEALTHY[i % len(HEALTHY)]`,
resolving `HEALTHY` against its own module's globals at call time.

**Constraint for extraction:** `HEALTHY`, `PER_PROVIDER`, and `assign_providers` must all land in the
**same** destination module (whichever one is chosen), and `refresh_healthy()`'s `import fire_today_20 as ft`
must become `import <new_module> as wc` pointing at that same module. Splitting `HEALTHY` into one
module and `assign_providers` into another would silently desync the in-place mutation from the
function that reads it.

### Recommended module split (Claude's Discretion area — this is a recommendation, not a decision)

`waves_lib.py`'s own docstring states its contract: *"Funções puras do orquestrador diário — testáveis
offline."* Its existing 6 functions (`compute_wave_slots`, `brt_label`, `is_foreign_lead`,
`in_target_geo`, `is_role_mailbox`, `personalize_html`, `fup_candidates`) are all pure — no network
I/O, no globals mutation, offline-testable. The D-01 extraction set is a mix:
- **Pure/config:** `DIR`, `base`, `ts`, `SEGMENTS`, `WAVES` — fit `waves_lib.py`'s contract fine.
- **I/O-performing:** `load_env` (reads a file + mutates `os.environ`), `login` (network), `gen_image`
  (network, 2 providers + PIL resize + upload), `fetch_leads` (network, paginated), `ensure_breaker_closed`
  (network), `assign_providers` (pure but tightly coupled to `HEALTHY` — see above), `build_html_v2`
  (pure string-building, but it's a large HTML template function, arguably "content" not "logic").

Recommend a **new module** (e.g. `waves_core.py`) for the I/O-performing set (`load_env, login,
gen_image, fetch_leads, ensure_breaker_closed, HEALTHY, PER_PROVIDER, assign_providers, DIR, base, ts`),
keeping `waves_lib.py`'s "pure functions" docstring contract honest. `SEGMENTS`/`WAVES`/`build_html_v2`
(content + template) can go into either — a third option, `waves_content.py`, keeps template/copy data
separate from both infra and pure-logic, but is not required; putting them in `waves_core.py` next to
the fetch/login layer is also reasonable since they are only ever consumed together at wave-build time.

## Health-Check / Alert Architecture (OBSV-01)

### Correction to D-06: there are two n8n instances, only one is in scope

| | Cloud n8n (EasyPanel/VPS) | Local n8n (notebook) |
|---|---|---|
| URL | `https://n8n-n8n.h8tqhp.easypanel.host` (in `.secrets.env` as `N8N_WEB_URL`) | `http://localhost:5678` (hardcoded in `n8n-local/n8n.env`, no `.secrets.env` entry) |
| Used by | VNG/other projects' automations; also monitored by `_scripts/vps-watchdog.py` already | DIAX's WhatsApp bot workflows (`n8n-local/workflows/waha-bot-atendente.json`, outreach handoff) |
| Relevant to Phase 10? | No — already monitored, and not part of DIAX's outreach pipeline | **Yes** — this is the "n8n" in OBSV-01's "waves, vigia, n8n, WAHA" list |

Using `N8N_WEB_URL`/`N8N_API_KEY` per D-06's literal text would monitor the wrong n8n (already
covered by an unrelated watchdog) and miss the one that actually backs DIAX's WhatsApp pipeline. The
planner should target `http://localhost:5678/healthz` (confirmed working health endpoint — this is
exactly what `n8n-local/watchdog-n8n.ps1` already polls, `Invoke-WebRequest -Uri "http://localhost:5678/healthz"`,
200 = healthy).

### WAHA health endpoint — confirmed via running config, not guessed

- Local WAHA container (`n8n-local/docker-compose.yml`): binds `127.0.0.1:3000:3000`, `WAHA_API_KEY`
  env var required (`X-Api-Key` header). `n8n-local/waha-watchdog.py` uses
  `GET /api/sessions/default` (not `/api/health`) to read `.status`, expecting `WORKING`. This is a
  richer signal than a generic health endpoint — "container up" vs. "WhatsApp session actually
  paired and working" are different failure modes, and the existing watchdog already distinguishes
  them. D-06's `GET /api/health` candidate is untested in this codebase; `GET /api/sessions/default`
  is the endpoint actually in production use today and is the safer choice to replicate.
- Requires `WAHA_API_KEY` from `.secrets.env` (or `n8n-local/.env`) as `X-Api-Key` header — WAHA
  returns 401 without it, which could otherwise be misread as "WAHA is down."

### Existing self-healing infrastructure — don't duplicate, extend

| Component | Location | What it does today | Gap vs. OBSV-01 |
|---|---|---|---|
| `waha-watchdog.py` | `n8n-local/` | Checks Docker → container → session status; self-heals (start/restart session); on unrecoverable failure, sends alert via **WAHA's own `sendText`** to the owner's WhatsApp | No Telegram alert; alert channel depends on the very service that's down (chicken-and-egg if WAHA is fully dead, not just session-degraded) |
| `watchdog-n8n.ps1` | `n8n-local/` | Polls `/healthz` every 5 min (via Task Scheduler `n8n-watchdog`, confirmed `Running`); on failure, kills node.exe, restarts the `n8n-local` Task Scheduler task, re-runs `restaurar-tudo.py` | **No alert of any kind** — only writes to a local log file. Silent self-healing, which is exactly the "falha em silêncio" (silent failure) the phase goal calls out. |
| `_scripts/vps-watchdog.py` | `_scripts/` | Polls VPS-hosted n8n `/healthz` + WAHA root URL; transition-based Telegram alert (🔴 on down, 🟢 on recovery, includes downtime duration); registered as live Task Scheduler task `VPS-Watchdog`, every 5 min, `pythonw.exe` direct (no console window) | This *is* the pattern to copy — it already does everything D-05/D-06 ask for, just pointed at the wrong (cloud) targets |

**Recommendation:** rather than writing a health-check script from scratch, either (a) add a Telegram
call to `watchdog-n8n.ps1`'s existing failure branch and to `waha-watchdog.py`'s failure branch (minimal
diff, reuses proven self-healing logic), or (b) clone `_scripts/vps-watchdog.py`'s structure
(transition-based alert, reads secrets directly, `pythonw`-safe stdout handling) into a new script
pointed at `127.0.0.1:3000` (WAHA session status) and `localhost:5678/healthz` (n8n). Option (b) matches
CONTEXT.md's explicit "script/job NOVO e independente" framing more literally; option (a) is less code
and reuses battle-tested self-healing. Either is consistent with the locked decisions — this is a good
candidate to surface as an open question for the planner/user rather than silently pick one.

**Note:** these `n8n-local`/`_scripts` files live outside the `diax-crm` git repository (a separate
folder under `D:\claude-code`). If the phase's deliverable must live inside `docs/email-marketing/`
(as CONTEXT.md's canonical refs imply — the WAHA/n8n alert is listed as new code alongside
`daily_waves.py`), the new script should be a fresh file there that reads the same `.secrets.env`, not
a modification of the `n8n-local` scripts, even if it borrows their logic. This is a placement decision
worth confirming with the plan.

### Confirmed Telegram pattern (triangulated from 3 independent working implementations)

All three — `daily_waves.tg_send()`, `dealhunter.notifier.send_telegram()`, `_scripts/vps-watchdog.py`'s
`telegram()` — use the identical shape:
```python
requests.post(f'https://api.telegram.org/bot{TELEGRAM_BOT_TOKEN}/sendMessage',
              json={'chat_id': TELEGRAM_CHAT_ID, 'text': text, 'parse_mode': 'HTML'},
              timeout=20)  # or urllib.request equivalent if avoiding a `requests` dependency
```
`vps-watchdog.py`'s variant is the most relevant reference for a Task-Scheduler-run watchdog: it reads
`TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` directly from `~/.claude/.secrets.env` via a tiny inline parser
(no dependency on `daily_waves.load_env()` or any DIAX-specific module), uses `urllib.request` (stdlib
only, no `requests` import needed), and — most importantly — **only alerts on state transition** (first
failure after being up, first success after being down), persisting `{"vivo": bool, "desde": timestamp}`
to a small JSON state file next to the script. This avoids alert spam if the health check runs every
15-30 minutes and WAHA/n8n are down for hours.

## Task Scheduler — confirmed live state (not just the checked-in XML)

The checked-in `docs/email-marketing/DIAX-Email-Waves-Daily.xml` export does **not** match the live
registration — the actual live task wraps the call through a `.vbs` helper:
```
DIAX-Email-Waves-Daily:  wscript.exe //B //Nologo "D:\claude-code\_scripts\run-exe-hidden.vbs"
                         "C:\Program Files\Python314\python.exe"
                         "D:\claude-code\diax-crm\docs\email-marketing\daily_waves.py"
```
while the checked-in XML shows a direct `python.exe` `<Exec>` with no vbs wrapper. **Do not trust the
checked-in XML as ground truth for how the task actually runs** — query the live Task Scheduler state
if the plan needs to reason about current invocation behavior.

Two invocation patterns exist side-by-side today for periodic scripts on this machine:
1. **vbs-wrapped** (`run-exe-hidden.vbs` via `wscript.exe`) — used by `DIAX-Email-Waves-Daily` (older pattern)
2. **Direct `pythonw.exe`** (self-contained, no wrapper) — used by `DIAX-Reply-Watch` and `VPS-Watchdog`; the script itself handles `sys.stdout is None` (pythonw has no console) by redirecting to a log file, as seen in `_scripts/vps-watchdog.py` lines 26-40

**Recommendation:** the new health-check script should follow pattern 2 (direct `pythonw.exe`, no vbs
wrapper) — it is the newer, simpler, already-proven pattern (`VPS-Watchdog` task confirmed `Ready`
state, `LastTaskResult: 0`, running every ~5 min via a `Repetition` trigger, not a `CalendarTrigger`).

Confirmed via live `Get-ScheduledTask`: no scheduled task currently references `waha-watchdog.py` by
name (only `n8n-watchdog` for the n8n side is actively `Running`) — the WAHA-side self-healing script
exists on disk but may not currently be scheduled. Worth a quick manual check before assuming it's
live-monitoring today.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Telegram alert send | New HTTP client / SDK | Copy the 6-line pattern from `_scripts/vps-watchdog.py`'s `telegram()` (or `daily_waves.tg_send()`) | 3 independent working implementations already exist in this exact codebase/environment; identical shape; `requests` or stdlib `urllib.request` both proven to work |
| WAHA/n8n polling + self-healing | New watchdog logic from scratch | `n8n-local/waha-watchdog.py` (session-status polling + auto-restart) and `watchdog-n8n.ps1` (healthz polling + task restart) already do this — extend with a Telegram call rather than reimplementing the polling/retry logic | Reinventing risks missing edge cases already handled (Docker-not-running, session STOPPED vs WORKING vs SCAN_QR_CODE, node.exe zombie processes) |
| Transition-based (not spam) alerting | Ad-hoc "alert every failed check" | `_scripts/vps-watchdog.py`'s state-file pattern (`{"vivo": bool, "desde": ts}`, alert only on flip) | Prevents Telegram spam if WAHA/n8n stay down for hours between 15-30 min polls |
| Secrets loading in a standalone watchdog script | Import `daily_waves.load_env()` (pulls in the whole email-marketing import chain) | `_scripts/vps-watchdog.py`'s tiny inline `sec()` parser that reads only the 2 needed keys from `.secrets.env` | Decouples the watchdog from the fire_* refactor entirely — it should keep working even mid-refactor |

**Key insight:** Every piece of this phase already has a working reference implementation somewhere
in this environment. The risk here is not "how do we build this" (trivial), it's "don't miss a live
dependency while archiving 76 files" and "don't duplicate self-healing logic that already exists 2
folders away."

## Common Pitfalls

### Pitfall 1: Archiving `fire_today_20.py` breaks 6 non-`fire_*`-named files
**What goes wrong:** A pass that identifies "files named `fire_*.py` with zero imports from
`daily_waves.py`" and moves them to `archive/` will correctly skip `import_extrator_bridge.py`,
`replenish_leads.py`, etc. (right call, they're not fire-named) — but if `fire_today_20.py` itself is
then also archived (it *is* fire-named), those 6 files' `from fire_today_20 import ...` breaks at their
next run.
**Why it happens:** The dependency check in CONTEXT.md's D-03 (`grep "from fire_\|import fire_"`)
finds files that import *from* `fire_*`, but the archival criterion in D-02 is framed in terms of
*files matching the `fire_*.py` naming pattern* — these are two different sets that only partially
overlap.
**How to avoid:** Either (a) leave `fire_today_20.py` as a re-export shim instead of moving it
physically, or (b) update all 6 files' import lines to the new module in the same commit as the
archival move. Verify with `python -c "import <each of the 6>"` (or at least `py_compile`) after the
move, not just `import daily_waves`.
**Warning signs:** `replenish_leads.py` failing silently on the next Friday run (it's called inside a
`try/except Exception` in `daily_waves.py` line 783 — a broken import there degrades to a logged
warning + Telegram message "ESTOQUE BAIXO e reposição automática FALHOU", not a crash, so this could
go unnoticed for a while).

### Pitfall 2: `_harvest()`/`COPY_BANK` losing niches silently, not loudly
**What goes wrong:** Extracting `SEGMENTS`/`WAVES` with a slightly different dict shape (e.g. renaming
a key, dropping a niche) doesn't raise an `ImportError` — `daily_waves.py`'s `_harvest()` just builds a
smaller `COPY_BANK`, and `ROTATION` niches not in `COPY_BANK` will `KeyError` at `cfg = COPY_BANK[niche]`
inside `build_waves()`'s while-loop only when that niche's turn comes up in the rotation cursor — which
can be days later, not on the next run.
**Why it happens:** Dict-based "content bank" pattern has no static verification; the failure is
deferred to whichever day the rotation cursor reaches the broken niche.
**How to avoid:** After extraction, run `daily_waves.py --dry --force` (dry mode) and confirm the DRY
log's plan covers/cycles through the full 20-niche `ROTATION`, or write a one-off assertion
(`assert set(ROTATION) <= set(COPY_BANK)`) as a manual verification step.
**Warning signs:** A specific niche's wave silently stops appearing in the Telegram daily digest.

### Pitfall 3: Monitoring the wrong n8n
**What goes wrong:** Following D-06 literally (`N8N_WEB_URL`/`N8N_API_KEY` from `.secrets.env`) monitors
the cloud EasyPanel n8n, which (a) is unrelated to DIAX's WhatsApp pipeline and (b) is already monitored
by `_scripts/vps-watchdog.py`. The actually-relevant local n8n (`localhost:5678`) would go unmonitored,
defeating the purpose of OBSV-01.
**How to avoid:** Point the new check at `http://localhost:5678/healthz`, confirmed as the address
`n8n-local`'s own existing watchdog already uses successfully.

### Pitfall 4: WAHA 401 mistaken for "down"
**What goes wrong:** WAHA requires `X-Api-Key: <WAHA_API_KEY>` on its API endpoints; a check that omits
the header gets a 401, which if not distinguished from "no response"/"connection refused" would produce
a false "WAHA is down" alert every single poll.
**How to avoid:** Include the API key (from `.secrets.env` or `n8n-local/.env`); treat 401 as a
different failure category if it must be surfaced at all (it would indicate a config/key mismatch, not
an outage).

## Validation Architecture

`docs/email-marketing/` has **zero existing test files** (confirmed: no `test_*.py`/`*_test.py`, no
`pytest.ini`/`conftest.py` anywhere in the tree). All scripts here are plain Python, run via Windows
Task Scheduler, outside the `.NET`/xUnit test suite entirely. This mirrors Phase 9's approach
(`docs/lead-quality/BACKFILL-EVIDENCE.md` — human-verified operational evidence, not automated tests).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None — plain scripts, no test runner in this part of the tree |
| Config file | none |
| Quick run command | `python daily_waves.py --dry` (dry-run: builds the full plan, writes no data, no emails sent, no Telegram alert) |
| Full suite command | N/A — verification here is operational/manual, same as Phase 9 |

### Phase Requirements → Verification Map
| Req ID | Behavior | Verification Type | Command | Notes |
|--------|----------|--------------------|---------|-------|
| SCRP-01 | `daily_waves.py` still builds full 20-niche plan after extraction | Manual/dry-run | `python daily_waves.py --dry --force` (bypass weekend guard) | Compare DRY log's niche list against `ROTATION`; also `py_compile` the 6 landmine files |
| SCRP-01 | Archived files don't break anything importing them | Manual | `python -c "import <module>"` for each of the 6 landmine files post-move | No automated CI gate exists for this directory |
| OBSV-01 | New health-check script correctly detects WAHA/n8n down | Manual (fault injection) | Stop the `waha`/`n8n-local` container or task, run the new script once, confirm Telegram message arrives; restart, confirm recovery message | No sandbox/staging for WAHA session — testing against the real local instance is unavoidable, do it during a low-traffic window |
| OBSV-01 | Alert doesn't spam | Manual | Run the script twice in a row while a failure persists; confirm second run does NOT re-alert (transition-only logic) | Verify the state JSON file updates correctly |

### Wave 0 Gaps
- No test framework exists for this directory and none is proposed — this phase's scope (script
  consolidation + a watchdog) does not warrant introducing pytest here; verification stays manual,
  consistent with Phase 9's precedent.
- If the plan wants a stronger regression guard for the extraction: a minimal `py_compile`/`ast`-based
  "these N names still exist and are still importable from module X" smoke check would be cheap to add
  and catches the Pitfall 1/2 class of failure mechanically — worth considering but not mandatory.

## Sources

### Primary (HIGH confidence — direct code/config inspection)
- `D:\claude-code\diax-crm\docs\email-marketing\daily_waves.py` — read in full (793 lines)
- `D:\claude-code\diax-crm\docs\email-marketing\waves_lib.py` — read in full
- `D:\claude-code\diax-crm\docs\email-marketing\fire_today_20.py` — read in full
- `D:\claude-code\diax-crm\docs\email-marketing\fire_w1_2707.py` — read in full
- `D:\claude-code\diax-crm\docs\email-marketing\fire_w2w3_2707.py` — read in full
- `grep -rn "from fire_\|import fire_" docs/email-marketing/**/*.py` — full 45-file result set
- `D:\claude-code\n8n-local\waha-watchdog.py`, `watchdog-n8n.ps1`, `start-n8n.cmd`, `n8n.env`, `docker-compose.yml`
- `D:\claude-code\waha-hub\docker-compose.yml`
- `D:\claude-code\_scripts\vps-watchdog.py`
- `D:\claude-code\skills\dealhunter\dealhunter\notifier.py`
- `C:\Users\acq20\.claude\.secrets.env` — grepped for `TELEGRAM_*`/`N8N_*` keys
- Live `Get-ScheduledTask` output for `VPS-Watchdog`, `n8n-watchdog`, `DIAX-Email-Waves-Daily`, `DIAX-Reply-Watch`, `n8n-local` (confirms actual invocation commands, differs from checked-in XML export)
- `git status --short docs/email-marketing/reply_watch.py` — confirmed uncommitted changes still present (STATE.md blocker still valid)
- `.planning/REQUIREMENTS.md`, `.planning/STATE.md`, `.planning/config.json`

### Secondary (MEDIUM confidence)
- None used — no external library/framework claims in this phase, no WebSearch needed.

### Tertiary (LOW confidence)
- Whether `debug_campaign.py` and `import_extrator_bridge.py` are still manually invoked by the user
  occasionally — inferred from STATE.md wording ("fallback/paralelo") and absence from Task Scheduler,
  not independently confirmed with the user.

## Metadata

**Confidence breakdown:**
- Dependency graph (SCRP-01 extraction scope): HIGH — verified by full-tree grep + reading all 3 source files in full
- Archive safety: HIGH for the 40 dead-end scripts (verified none are import targets) — MEDIUM for the 6 landmine files' current "still needed" status (STATE.md-inferred, not user-confirmed for 4 of them)
- OBSV-01 health-check design: HIGH for endpoints/patterns (verified against live running config and live Task Scheduler state) — MEDIUM for "which implementation option (extend existing watchdogs vs. new standalone script)" since that's explicitly left to Claude's Discretion / needs a placement decision

**Research date:** 2026-09-19
**Valid until:** Stable — this is internal codebase archaeology, not fast-moving external dependencies. Re-verify only if `docs/email-marketing/` or `n8n-local/` changes materially before planning executes (e.g. `reply_watch.py`'s known uncommitted changes land, or someone else touches the fire_* tree).
