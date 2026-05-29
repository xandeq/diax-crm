---
phase: 2
slug: funda-o-de-agentes
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-28
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Backend-only phase (.NET). No frontend tests this phase.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9 + Moq 4.20 (existing) |
| **Config file** | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| **Quick run command** | `DOTNET_ROLL_FORWARD=Major dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj --no-build --filter "FullyQualifiedName~Agents"` |
| **Full suite command** | `DOTNET_ROLL_FORWARD=Major dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| **Estimated runtime** | ~30–60s (build) + ~1–5s (Agents filter) |

> NOTE: máquina só tem runtime .NET 10; projeto é net8.0 → `DOTNET_ROLL_FORWARD=Major` é obrigatório para rodar testes.

---

## Sampling Rate

- **After every task commit:** Run the quick command (Agents filter)
- **After every plan wave:** Run the full suite
- **Before `/gsd:verify-work`:** Full suite green + `dotnet build Diax.CRM.sln` 0 errors
- **Max feedback latency:** ~90 seconds

---

## Per-Task Verification Map

> Preenchido/refinado pelo planner. Cobertura mínima exigida abaixo (Nyquist):

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 2-xx | TBD | 1 | ORCH-01 | unit | quick (Agents) | ❌ W0 | ⬜ pending |
| 2-xx | TBD | 1 | ORCH-02 | unit | quick (Agents) | ❌ W0 | ⬜ pending |
| 2-xx | TBD | 2 | ORCH-04 | unit | quick (Agents) | ❌ W0 | ⬜ pending |
| 2-xx | TBD | 2 | ORCH-05 | integration | quick (Agents) | ❌ W0 | ⬜ pending |
| 2-xx | TBD | 2 | ORCH-03 | unit | quick (Agents) | ❌ W0 | ⬜ pending |

---

## Wave 0 Requirements

- [ ] `tests/Diax.Tests/Application/Agents/AgentOrchestratorTests.cs` — roteamento por tipo (ORCH-01/02)
- [ ] `tests/Diax.Tests/Application/Agents/AgentToolFrameworkTests.cs` — tool de escrita retorna pending_confirmation; nada gravado antes do confirm (ORCH-04) — **teste crítico de segurança**
- [ ] `tests/Diax.Tests/Application/Agents/AgentConversationTests.cs` — persistência/retomada com AgentType (ORCH-05)

*Existing infra (xUnit+Moq) cobre o resto; sem novo framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 403 RBAC end-to-end + token logging em prod | ORCH-03 | Depende de JWT + grupos reais no DB | Smoke test autenticado pós-deploy (`/wave-qa`) |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
