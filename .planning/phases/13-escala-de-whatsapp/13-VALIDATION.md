---
phase: 13
slug: escala-de-whatsapp
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-09-20
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`api-core/tests`) for the one C# touchpoint (`IntegrationsController.WhatsAppEvent`, already covered by existing tests) — everything else (n8n workflow state, `outreach-send.py`, `crmLeads` sync, weekly report, escalation decision) is ops/config work verified by live proof, same pattern as Phases 10-12. |
| **Config file** | `api-core/api-core.sln` for dotnet test; no framework for `docs/email-marketing/*.py` — `--dry`/`--selftest`/`--status` CLI convention instead. |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~WhatsAppEventIntegrationTests"` (if `IntegrationsController` touched) + relevant script's `--dry`/`--status` flag |
| **Full suite command** | `cd api-core && dotnet test -c Release` (only if C# touched — most of this phase doesn't) |
| **Estimated runtime** | ~10-30s for dotnet test subset; ~5-10s per script `--dry`/`--status` check |

Note: this phase's planner found the live system diverges from `13-RESEARCH.md` in load-bearing
ways (a second, already-running sender `outreach-send.py` was discovered, and `waOutreach1a1AQ`
was found deliberately disabled after a real double-send incident, not merely "never activated").
Live re-verification at execution time takes precedence over RESEARCH.md's numbers wherever they
conflict — this is expected and documented, not a research failure.

---

## Sampling Rate

- **After every task commit:** Run the touched script's `--dry`/`--status`/`--selftest` flag; `dotnet test --filter WhatsAppEventIntegrationTests` only if `IntegrationsController` was touched.
- **After every plan wave:** Full manual walkthrough per each plan's own `<verify>` blocks; Wave 1's checkpoint (13-01) gates before any change to sender state (CAP/active flags).
- **Before `/gsd:verify-work`:** Live proof captured in a runbook (`ESCALA-WHATSAPP-runbook.md`, same pattern as Phases 10-12's runbooks).
- **Max feedback latency:** ~30 seconds for automated checks; live n8n/WAHA state checks are one-off, not loop-blocking.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 13-01-01..03 | 01 | 1 | WHAT-01 | live-proof + checkpoint | `wa_estado.py --status` (reads n8n sqlite + audit log + CRM) | ✅ (created) | ⬜ pending |
| 13-02-01..02 | 02 | 2 | WHAT-02 | live-proof (scheduled) | `crm_leads_sync.py --dry` + `schtasks /query DIAX-WA-CrmLeads-Sync` | ✅ (created) | ⬜ pending |
| 13-03-01..02 | 03 | 2 | WHAT-03 | live-proof | `whatsapp_weekly_report.py --dry` against real DB + `git status daily_waves.py` (must be empty) | ✅ (created) | ⬜ pending |
| 13-04-01..03 | 04 | 3 | WHAT-01..04 | live-proof + honesty gate | `wa_escalation.py --check` (dry) + `git diff` proving thresholds locked before data seen | ✅ (created) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

Pre-existing coverage (not new, part of what proves WHAT-02's CRM half correct):
`WhatsAppEventIntegrationTests.cs` already covers sent/reply/optout/failed contract for
`IntegrationsController.WhatsAppEvent` — no changes needed there unless this phase touches that
controller (expected: it doesn't).

---

## Wave 0 Requirements

- [x] `docs/email-marketing/wa_estado.py` — single-reader state tool (n8n sqlite + audit log + CRM), created as this phase's own Wave 1 deliverable (13-01)
- [x] `docs/email-marketing/crm_leads_sync.py` (rewrite) — recurring incremental sync, created as Wave 2 deliverable (13-02)
- [x] `docs/email-marketing/whatsapp_weekly_report.py` — did not exist, created as Wave 2 deliverable (13-03)
- [x] `docs/email-marketing/wa_escalation.py` — did not exist, created as Wave 3 deliverable (13-04), mirrors `dmarc_escalation.py`'s `decide()` shape (pure function, thresholds locked before data, `--apply` gate)

*All Wave 0 gaps are this phase's own deliverables across its 3 waves — no external test-framework gap exists (consistent with Phases 10-12's no-pytest precedent for this directory).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|--------------------|
| Sender-conflict decision made explicitly, not silently | WHAT-01 | Real production risk — two competing WhatsApp senders on same dedupe, one already proven broken (caused a real double-send incident 12/09) | 13-01 Task 2 checkpoint presents 3 options (`python-oficial`/`n8n-reativar`/`segurar`) with the conflict risk stated; user picks, Task 3 executes only the chosen branch |
| crmLeads sync keeps deflection list current | WHAT-02 | Depends on real CRM lead data and real n8n staticData, no staging environment | `crm_leads_sync.py --dry` inspection + confirm `DIAX-WA-CrmLeads-Sync` scheduled task exists and ran (`schtasks /query`) |
| Weekly report shows correct real numbers, delivered Friday afternoon, not conflicting with email's Friday-morning report | WHAT-03 | Real campaign/task data; timing conflict risk with existing `daily_waves.py` Friday branch | Run `whatsapp_weekly_report.py --dry`, inspect against real CRM task counts; confirm `DIAX-Email-Waves-Daily` trigger time unchanged (`schtasks /query`) and `daily_waves.py` absent from `git status` |
| Escalation verdict is honest given thin sample (17 total WhatsApp contacts ever) | WHAT-04 | Real data, no way to synthesize a larger sample | `wa_escalation.py --check` (dry) run against real data; `git diff` between threshold-definition commit and final commit must show thresholds unchanged after seeing the result (anti-tuning proof) |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (`--dry`/`--status`/`--selftest`/`schtasks`/`git diff`) or are `checkpoint:decision` with a documented options table
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covered — all 4 new scripts are this phase's own deliverables across its 3 waves
- [x] No watch-mode flags
- [x] Feedback latency <30s for automated checks; live n8n/WAHA/CRM checks are one-off by nature
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-09-20
