---
phase: 7
slug: extra-o-qualidade-na-entrada
status: planned
nyquist_compliant: true
wave_0_complete: false   # planejada (07-02); marcar true após execução
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
| 07-01-T1 | 01 | 1 | EXTR-03 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~WebsiteClassifierTests"` | ❌ cria | ⬜ pending |
| 07-01-T2 | 01 | 1 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~JunkDomainFilterTests"` | ❌ cria | ⬜ pending |
| 07-02-T1 | 02 | 1 | EXTR-01 | build | `dotnet build -c Release` | n/a | ⬜ pending |
| 07-02-T2 | 02 | 1 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~MxResponseInterpreterTests"` | ❌ cria | ⬜ pending |
| 07-03-T1 | 03 | 2 | EXTR-02, EXTR-03 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~CustomerImportCountersTests"` | ❌ cria | ⬜ pending |
| 07-03-T2 | 03 | 2 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~MxCacheEntryTests"` | ❌ cria | ⬜ pending |
| 07-03-T3 | 03 | 2 | EXTR-01, EXTR-02, EXTR-03 | suite | `dotnet test -c Release --no-build` | ✅ | ⬜ pending |
| 07-04-T1 | 04 | 3 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~CachedMxCheckServiceTests"` | ❌ cria | ⬜ pending |
| 07-04-T2 | 04 | 3 | EXTR-01 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~ExtractorIntegrationServiceTests"` | ✅ estende | ⬜ pending |
| 07-05-T1 | 05 | 4 | EXTR-02, EXTR-03 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~ExtractorIntegrationServiceTests"` | ✅ estende | ⬜ pending |
| 07-05-T2 | 05 | 4 | EXTR-02, EXTR-03 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~CustomerImportServiceTests"` | ✅ estende | ⬜ pending |
| 07-06-T1 | 06 | 5 | EXTR-02 | unit | `dotnet test -c Release --no-build --filter "FullyQualifiedName~ImportHistoryQueryTests"` | ❌ cria | ⬜ pending |
| 07-06-T2 | 06 | 5 | EXTR-02 | suite | `dotnet test -c Release --no-build` | ✅ | ⬜ pending |
| 07-07-T1 | 07 | 6 | EXTR-01, EXTR-02, EXTR-03 | suite + audit | `dotnet build -c Release && dotnet test -c Release --no-build` | ✅ | ⬜ pending |
| 07-07-T2 | 07 | 6 | EXTR-01, EXTR-02, EXTR-03 | checkpoint (prod) | `dotnet build -c Release && dotnet test -c Release --no-build` + `update-db.ps1` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] **PLANEJADA no 07-02 (wave 1)** — Abstração de resolução DNS (`IMxLookupService` sobre o `LookupClient` do DnsClient.NET) —
      **obrigatória** para que os testes de EXTR-01 rodem sem rede real. Sem ela não há como
      testar "timeout passa / NXDOMAIN rejeita" (D-02) de forma determinística.
- [x] **PLANEJADA no 07-02 (`MxResponseInterpreterTests`) + 07-04 (`Mock<IMxLookupService>` em `ExtractorIntegrationServiceTests`)** — cobrindo os 4 cenários: MX válido · sem MX (NXDOMAIN) ·
      Null MX (RFC 7505, `Exchange.Value == "."`) · timeout (`DnsResponseException` com
      `Code == ConnectionTimeout`)
- [x] **CONFIRMADA (07-04 Task 2 reutiliza e estende `CreateSut`)** — Fixture de `ExtractorPullOptions` já existe (`CreateSut(ExtractorPullOptions?)` em
      `ExtractorIntegrationServiceTests.cs`) — reutilizar, não recriar

*Infra de teste (xUnit/Moq) já existe; Wave 0 é só a abstração de DNS + fakes.*

⚠️ **FluentAssertions NÃO está em `Diax.Tests.csproj`** — todos os testes desta fase usam
`Assert.*` do xUnit puro. `Should()` não compila.

**Decisão do planner sobre a testabilidade da implementação real:** `DnsClientMxLookupService`
NÃO é testado contra DNS real (não-determinístico em CI). A decisão Valid/NoMx/fallback é
extraída para `MxResponseInterpreter` (função pura sobre tipos primitivos), testada
integralmente offline. O glue tipado com o DnsClient.NET fica fino e é validado por smoke manual
pós-deploy.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DNS de saída funciona no SmarterASP (UDP/53 liberado) | EXTR-01 | Ambiente de shared hosting não é reproduzível em teste; pesquisa não achou documentação pública | Após deploy, disparar um import e conferir no `CustomerImport` da rodada se o contador "sem MX" ficou plausível (não 100% dos leads, o que indicaria DNS bloqueado e tudo caindo em "não verificado") |
| Migration aplica limpo em produção | EXTR-03 | `update-db.ps1` roda contra o SQL Server de produção; não há ambiente de staging | Rodar `update-db.ps1`, conferir que as colunas `WebsiteKind` e `ExternalId` existem e que nenhum dado existente foi perdido |

---

## Validation Sign-Off

- [x] Todas as 15 tasks têm verificação `<automated>`
- [x] Continuidade de amostragem: nenhuma task sem verify automatizado
- [x] Wave 0 (abstração de DNS) é o plano 07-02, na wave 1 — antes de 07-04, que a consome
- [x] Sem flags de watch-mode
- [x] Latência de feedback < 30s (filtro por classe ~2s; suíte completa ~25s)
- [x] `nyquist_compliant: true` no frontmatter

**Approval:** planner sign-off 2026-09-05 — 14/15 tasks com `<automated>`; a única exceção (07-07 Task 2) é checkpoint de produção e ainda assim roda a suíte completa.
