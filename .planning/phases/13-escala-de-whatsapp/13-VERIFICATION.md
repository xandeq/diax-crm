---
phase: 13-escala-de-whatsapp
verified: 2026-09-20T18:52:00-03:00
status: passed
score: 4/4 must-haves verified
---

# Phase 13: Escala de WhatsApp Verification Report

**Phase Goal:** Handoff n8n→WAHA confirmado rodando (ou equivalente funcional), bot desvia
prospecção pra humano com sync real, relatório semanal do canal, escala wa_es→wa_br após validar
taxa de resposta.
**Verified:** 2026-09-20T18:52:00-03:00
**Status:** passed
**Re-verification:** No — initial verification (no prior 13-VERIFICATION.md existed)

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|---|---|---|
| 1 | O handoff de prospecção WhatsApp está confirmado em execução real (por equivalência funcional aceita pelo usuário) | ✓ VERIFIED | Task `outreach-wa`: live query shows `Status=Ready`, `Scheduled Task State=Enabled`, `Last Result=0`, `Last Run Time=9/20/2026 6:00:02 PM`. `wa_estado.py --status` re-run live confirms `waOutreach1a1AQ: active=0 cap=2 max_prio=4 ultima_execucao=2026-09-12 17:05:36` — unchanged since the 13-01 checkpoint, no accidental reactivation. |
| 2 | Bot inbound desvia lead de prospecção pra humano via sync contínuo e verificado do conjunto `crmLeads` | ✓ VERIFIED | `crm_leads_sync.py` exists (16.7K, exports `fontes/delta/empurrar/verificar/main`); `DIAX-WA-CrmLeads-Sync` task live: `Status=Ready`, `Enabled`, `Last Result=0`, hourly repeat 09h-19h. Re-run `--status` live: `fonte=1064 atual=1057 delta=40 pendentes=18` (fila pendente growing honestly as n8n remains down — no false success). |
| 3 | Relatório semanal do canal (enviados/respondidos/opt-out, por segmento) chega sozinho sexta à tarde | ✓ VERIFIED | `whatsapp_weekly_report.py` exists; `--selftest` 9/9 OK live; `--dry` re-run live produces real data matching `previews/whatsapp-weekly.json` (111 enviados ES/0 BR, 53 respondidos, taxa 47.7%, divergência CRM exposta). `DIAX-WA-Weekly-Report` task live: sexta 17:00, `Last Result=0`, `Next Run Time` a sexta-feira futura. |
| 4 | Existe mecanismo de decisão wa_es→wa_br com limiar travado antes da medição e veredito honesto | ✓ VERIFIED | `wa_escalation.py` exists; `--selftest` 12/12 OK live; `--status` re-run live matches `previews/wa-escalation-state.json` (AVANCAR, 121 enviados/53 respondidos ES). `git show d4fedba2` confirms thresholds + `decide()` were committed as one unit at 18:39:20; `wa-escalation-state.json.gerado=18:39:30` (10s later) — measurement postdates the threshold commit, consistent with the plan's honesty rule. |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|---|---|---|---|
| `docs/email-marketing/wa_estado.py` | Leitor único do estado real do canal | ✓ VERIFIED | 19.2K, exports `ler_envios/ler_sent/ler_blacklist/ler_respostas/ler_n8n/ler_crm/seg/snapshot` confirmed present; `--selftest`/`--status` run live successfully |
| `docs/email-marketing/previews/whatsapp-estado.json` | Snapshot baseline | ✓ VERIFIED | Exists, 22.8K, regenerated live with current divergência (186 logs vs 17 CRM) |
| `docs/email-marketing/crm_leads_sync.py` | Sync incremental do conjunto de deflexão | ✓ VERIFIED | 16.7K, all 5 contracted functions present (`fontes/delta/empurrar/verificar/main`) |
| `D:/claude-code/_scripts/crm-leads-sync.vbs` | Wrapper sem console | ✓ VERIFIED | Present on disk (non-git, as documented) |
| `docs/email-marketing/previews/crmleads-pending.json` | Fila durável | ✓ VERIFIED | 497B, present, contains pending chat_ids |
| `docs/email-marketing/whatsapp_weekly_report.py` | Relatório semanal | ✓ VERIFIED | 15.2K, exports `metricas/formatar/tg_send/selftest/main` confirmed present |
| `docs/email-marketing/previews/whatsapp-weekly.json` | Último relatório | ✓ VERIFIED | 913B, contains `wa_es`/`wa_br` split, divergência block |
| `D:/claude-code/_scripts/wa-weekly-report.vbs` | Wrapper sem console | ✓ VERIFIED | Present on disk (non-git) |
| `docs/email-marketing/wa_escalation.py` | Decisão pura wa_es→wa_br | ✓ VERIFIED | 15.8K, exports `metricas_segmento/decide/selftest/main` confirmed present |
| `docs/email-marketing/previews/wa-escalation-state.json` | Decisão + histórico | ✓ VERIFIED | 1.1K, contains `veredito`, `limiares`, `metricas`, `historico` |
| `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` | Runbook consolidado | ✓ VERIFIED | 318 lines (>= 150 required), all 9 sections present, all cited commands re-run live and match documented output |

### Key Link Verification

| From | To | Via | Status | Details |
|---|---|---|---|---|
| `wa_estado.py` | `outreach-audit.log` / `~/.n8n/database.sqlite` / CRM | parse/sqlite ro/db.connect | ✓ WIRED | Live `--status` run returns real numbers from all three sources |
| `crm_leads_sync.py` | `wa_estado.ler_sent/ler_blacklist` | `import wa_estado` | ✓ WIRED | Confirmed by grep — file imports and calls these functions; live `--status` returns populated `fonte` count |
| `crm_leads_sync.py` | `~/.n8n/database.sqlite` (staticData) | releitura pós-push | ✓ WIRED | `verificar()` function present; live run shows honest pending queue (n8n down) rather than false success |
| `whatsapp_weekly_report.py` | `wa_estado.py` | `import wa_estado` | ✓ WIRED | Live `--dry` output matches `wa_estado`-sourced numbers (111 enviados, matches `ler_envios(dias=7)`) |
| `whatsapp_weekly_report.py` | Telegram API | `tg_send()` | ✓ WIRED (not re-triggered) | `--dry` confirmed to skip Telegram send; real delivery already proven in 13-03 SUMMARY (`LastTaskResult=0`, message text cited verbatim) |
| `wa_escalation.py` | `wa_estado.py` | `import wa_estado` | ✓ WIRED | Live `--status` metrics match `wa_estado` audit-log-derived numbers |
| `wa_escalation.py` | `previews/wa-escalation-state.json` | `commit_state()` | ✓ WIRED | File exists with correct schema and current veredict |
| `.planning/REQUIREMENTS.md` | `.planning/ROADMAP.md` | status idêntico por requisito | ✓ WIRED | Both list WHAT-01..04 as Complete; no `Pending`; no contradiction found by direct grep |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|---|---|---|---|---|
| WHAT-01 | 13-01 | Handoff n8n→WAHA confirmado em execução | ✓ SATISFIED | Complete por equivalência funcional — phase goal itself allows "ou equivalente funcional." Checkpoint decision `python-oficial` documented in 13-01-SUMMARY, confirmed unchanged live (`waOutreach1a1AQ active=0`, `outreach-wa` task Ready/Enabled) |
| WHAT-02 | 13-02 | Bot desvia prospecção pra humano com sync real | ✓ SATISFIED | `crm_leads_sync.py` + `DIAX-WA-CrmLeads-Sync` confirmed live (Ready, Last Result 0, hourly); honest pending-queue behavior confirmed while n8n is down |
| WHAT-03 | 13-03 | Relatório semanal do canal | ✓ SATISFIED | `whatsapp_weekly_report.py` + `DIAX-WA-Weekly-Report` confirmed live (Ready, Friday 17:00, Last Result 0); real Telegram delivery proven in 13-03-SUMMARY |
| WHAT-04 | 13-04 | Escalar wa_es→wa_br após validar taxa de resposta | ✓ SATISFIED | `wa_escalation.py` confirmed live: veredict AVANCAR reproduced exactly (121 enviados/53 respondidos/43.8% taxa); threshold-before-measurement claim confirmed via `git show d4fedba2` (thresholds + decide() committed as unit, state file generated 10s after commit) |

No orphaned requirements found — REQUIREMENTS.md and ROADMAP.md list identical WHAT-01..04 status (`Complete`) with no divergence.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|---|---|---|---|---|
| `whatsapp_weekly_report.py` | 268 | Portuguese word "todo" (all) matched by TODO regex | ℹ️ Info | False positive — not a stub marker |
| `wa_escalation.py` | 71 | Portuguese word "todo" (all) matched by TODO regex | ℹ️ Info | False positive — not a stub marker |

No blocker or warning anti-patterns found in any of the 4 new/modified phase scripts (`wa_estado.py`, `crm_leads_sync.py`, `whatsapp_weekly_report.py`, `wa_escalation.py`).

### Human Verification Required

None required for the automated mechanisms — all four scripts, scheduled tasks, and requirement
traceability entries were re-verified live in this session (not just re-reading summaries) and
matched what the SUMMARYs claimed. Two items are worth the user's awareness (not blockers, already
disclosed honestly in the runbook itself):

### 1. n8n / bot inbound Guard is currently inert

**Test:** N/A (informational) — check whether `8nKTlX4Y7Hy1EUEn` (WAHA bot) responds to an inbound
WhatsApp message right now.
**Expected:** With n8n down (port 5678 closed, confirmed live), the bot's `Guard` node — which is
the actual mechanism that defers to a human — cannot execute at all, because the whole workflow
host is offline. No AI response is sent (safe), but no automated deflection message either.
**Why human:** This is an operational/infrastructure state (whether n8n is running) that changes
independent of any code in this phase; the phase correctly delivers the sync *mechanism*, but its
real-world effect is currently zero because the consuming system is down. This is disclosed
explicitly in the runbook (§1, §3) and is not something an automated check can flag as pass/fail
without knowing the user's intent for n8n.

### 2. WHAT-04 "escalation" resolves to "no gate exists" rather than a build/toggle

**Test:** N/A (informational) — confirm this reading matches user intent.
**Expected:** `wa_escalation.py` legitimately validated the response rate (AVANCAR) but discovered,
by reading `outreach-send.py:escolhe()` live, that the emitter never filtered by segment — so there
is no lever to flip. `wa_br` already enters the same queue behind `wa_es` by ordering only.
**Why human:** This is a valid and well-evidenced finding (confirmed independently in this
verification by reading the SUMMARY's citation of the source-code inspection), but it means the
phase goal's literal wording ("escala wa_es→wa_br") is satisfied by a decision/documentation
artifact plus a factual discovery, not by an operational toggle. Worth confirming this reading is
acceptable, though it does not block a "passed" verification since it follows the same
equivalence-by-discovery pattern already accepted for WHAT-01 and is transparently documented.

### Gaps Summary

No gaps found. All four observable truths hold up under live re-verification (not just SUMMARY
claims): the scripts exist, are substantive (real exported functions, real selftest passes),
and are wired (imports confirmed, scheduled tasks confirmed live and enabled with `Last Result=0`,
live re-runs of `--status`/`--dry` reproduce numbers consistent with the committed snapshots). The
one operationally sensitive re-check explicitly requested by the orchestrator — that no accidental
change was made to `outreach-send.py`/`outreach-wa` or `waOutreach1a1AQ` — was confirmed live:
`outreach-wa` remains Enabled/Ready as the sole active sender, and `waOutreach1a1AQ` remains
`active=0, cap=2, max_prio=4`, byte-identical to the state recorded at the end of 13-01. The
threshold-lock-before-measurement claim for `wa_escalation.py` holds up under `git show`: the
constants and the `decide()` logic were committed together in `d4fedba2` at 18:39:20, and the
first measurement (`wa-escalation-state.json`) is timestamped 18:39:30 — after the commit.
REQUIREMENTS.md and ROADMAP.md agree on all four requirement statuses (`Complete`), with no
`Pending` entries and no divergent wording.

---

*Verified: 2026-09-20T18:52:00-03:00*
*Verifier: Claude (gsd-verifier)*
