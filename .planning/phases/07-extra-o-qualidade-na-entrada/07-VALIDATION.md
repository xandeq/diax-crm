---
phase: 7
slug: extra-o-qualidade-na-entrada
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-09-05
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + Moq + FluentAssertions (.NET 8 / net10.0 test target) |
| **Config file** | `api-core/tests/Diax.Tests/Diax.Tests.csproj` (existente) |
| **Quick run command** | `dotnet test -c Release --no-build --filter "FullyQualifiedName~ExtractorIntegration"` |
| **Full suite command** | `dotnet test -c Release --no-build` (a partir de `api-core/`) |
| **Estimated runtime** | ~25s full suite (707 testes hoje); ~2s filtrado |

⚠️ **Build/test SEMPRE com `-c Release`** — Smart App Control da máquina bloqueia DLL de teste
Debug (`0x800711C7`). Rodar `dotnet build -c Release` antes de `dotnet test --no-build`.

---

## Sampling Rate

- **After every task commit:** `dotnet test -c Release --no-build --filter "FullyQualifiedName~ExtractorIntegration"`
- **After every plan wave:** `dotnet test -c Release --no-build` (suíte completa)
- **Before `/gsd:verify-work`:** suíte completa verde (baseline atual: 707/707)
- **Max feedback latency:** 30 segundos

---

## Per-Task Verification Map

Preenchido pelo planner ao criar os PLAN.md. Estrutura esperada:

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 07-01-xx | 01 | 1 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~MxCheck"` | ❌ W0 | ⬜ pending |
| 07-01-xx | 01 | 1 | EXTR-02 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~ExtractorIntegration"` | ✅ | ⬜ pending |
| 07-02-xx | 02 | 2 | EXTR-03 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~WebsiteKind"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Abstração de resolução DNS (ex.: `IDnsResolver` sobre o `LookupClient` do DnsClient.NET) —
      **obrigatória** para que os testes de EXTR-01 rodem sem rede real. Sem ela não há como
      testar "timeout passa / NXDOMAIN rejeita" (D-02) de forma determinística.
- [ ] Fakes/stubs do resolver cobrindo os 4 cenários: MX válido · sem MX (NXDOMAIN) ·
      Null MX (RFC 7505, `Exchange.Value == "."`) · timeout (`DnsResponseException` com
      `Code == ConnectionTimeout`)
- [ ] Fixture de `ExtractorPullOptions` já existe (`CreateSut(ExtractorPullOptions?)` em
      `ExtractorIntegrationServiceTests.cs`) — reutilizar, não recriar

*Infra de teste (xUnit/Moq) já existe; Wave 0 é só a abstração de DNS + fakes.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DNS de saída funciona no SmarterASP (UDP/53 liberado) | EXTR-01 | Ambiente de shared hosting não é reproduzível em teste; pesquisa não achou documentação pública | Após deploy, disparar um import e conferir no `CustomerImport` da rodada se o contador "sem MX" ficou plausível (não 100% dos leads, o que indicaria DNS bloqueado e tudo caindo em "não verificado") |
| Migration aplica limpo em produção | EXTR-03 | `update-db.ps1` roda contra o SQL Server de produção; não há ambiente de staging | Rodar `update-db.ps1`, conferir que as colunas `WebsiteKind` e `ExternalId` existem e que nenhum dado existente foi perdido |

---

## Validation Sign-Off

- [ ] Todas as tasks têm verificação `<automated>` ou dependência declarada de Wave 0
- [ ] Continuidade de amostragem: sem 3 tasks consecutivas sem verify automatizado
- [ ] Wave 0 cobre a abstração de DNS (sem ela, EXTR-01 não é testável)
- [ ] Sem flags de watch-mode
- [ ] Latência de feedback < 30s
- [ ] `nyquist_compliant: true` no frontmatter

**Approval:** pending
