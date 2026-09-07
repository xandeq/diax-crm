# Phase 8: Import — Dedup e Score em Tempo Real - Research

**Researched:** 2026-09-07
**Domain:** EF Core dedup logic + synchronous scoring, inside an existing .NET 8 Clean Architecture codebase (no new libraries)
**Confidence:** HIGH — every claim below is backed by a direct file:line read of the actual repo, not training-data assumption. No Context7/WebSearch was needed: this is 100% in-repo investigation.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Colisão de ExternalId (discutido com o usuário)**
- **D-01:** Mesmo `ExternalId`, e-mail diferente → **atualiza o e-mail do lead existente**. O Extrator é fonte da verdade do contato; se o negócio trocou de e-mail, queremos falar com o novo.
- **D-02:** ⚠️ **Risco de compliance que a D-01 cria, e que o planner DEVE tratar:** se o e-mail ANTIGO tinha opt-out (`EmailOptOut`) ou estava em `email_suppressions`, trocar o e-mail NÃO pode ressuscitar o contato. O opt-out/supressão precisa ser carregado para o e-mail novo (ou o lead permanece bloqueado). Isso não é opcional — é o mesmo tipo de furo que o incidente de 30/08 (leads lixo emailados) produziu.
- **D-03:** `ExternalId` casa com o lead A e o e-mail casa com o lead B (leads DIFERENTES no CRM) → **o e-mail vence**. Enriquece o lead B, e o conflito é registrado (contador/log) para inspeção posterior. Racional: o e-mail é o canal real de envio e é a chave usada por supressões e opt-out; deixar o `ExternalId` reescrever o e-mail de um lead com histórico de envio seria pior.
- **D-04:** Lead JÁ existente que casa por e-mail e traz `ExternalId` → **grava o `ExternalId` nele** (caminho de enriquecimento). Backfill orgânico: a rodada de 05/09 casou 362 leads por e-mail, então a base se popula sozinha em poucos dias, sem script de mutação em massa.

### Claude's Discretion

O usuário escolheu discutir apenas a colisão de `ExternalId`. As decisões abaixo ficam com o planner, guiadas pelo que já foi decidido acima:

- **Backfill em massa dos 8.056 leads com `external_id` nulo:** NÃO fazer. A D-04 resolve organicamente e evita mutação em massa em produção. Se o planner concluir que é insuficiente, levantar como questão em aberto em vez de decidir sozinho por um script de backfill.
- **Escopo do fix de `source=Scraping` (IMPT-02):** o scout mostrou que o dedup de Scraping NÃO está ausente — o bloco de validação roda só `if (DryRun || Source == Import)` (`CustomerImportService.cs` ~linha 86), mas existe um caminho de enriquecimento na persistência que casa por e-mail (foi o que produziu os 362 updates de 05/09). Então IMPT-02 é sobre **unificar o comportamento**, não sobre criar dedup do zero. O planner deve mapear a divergência real entre os dois caminhos antes de propor a correção.
- **`lead_score` no import (IMPT-03):** ⚠️ **questionar o valor antes de implementar.** No momento do import não existe engajamento, e com o scoring rebalanceado da Phase 7 o bloco de fit soma no máximo 25 pontos — abaixo do limiar de Warm (30). Ou seja, o score calculado no import será SEMPRE `Cold`, para todo lead, até o `LeadScoringWorker` rodar às 06h BRT. Implementar isso literalmente entrega um valor constante. O planner deve avaliar se o requisito se cumpre melhor (a) calculando mesmo assim, para o lead nunca ficar com score nulo/zero, ou (b) reinterpretando o requisito. Se (b), parar e levantar a questão — não redefinir requisito sozinho.

### Deferred Ideas (OUT OF SCOPE)

- **Script de backfill em massa do `external_id`** — decidido contra (D-04 resolve organicamente). Reavaliar só se o ritmo orgânico se mostrar insuficiente.
- **Endpoint proxy para a API do OpenRouter** em `api.alexandrequeiroz.com.br` — pedido pelo usuário em 07/09 para ser feito "em algum momento", interrompendo esta fase. **Não é escopo da Phase 8.**

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| IMPT-01 | `Customer.ExternalId` com índice único, dedup real por ID externo do Extrator | Column + filtered unique index ALREADY in production (Phase 7, migration `20260906101839_AddLeadQualitySignals`). Gap is 100% application code: `GetByExternalIdAsync` doesn't exist, `CustomerImportService` never queries by it. See Architecture Patterns §1-2. |
| IMPT-02 | `POST /customers/import` com `source=Scraping` deduplica de verdade, paridade com `source=Import` | Scraping's email-based enrich path already works (proven by existing passing test `Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport`, line 434). The real remaining gap is the SAME code as IMPT-01 — adding ExternalId as a matching key closes the one case email-dedup can't handle (email changed between pulls). See Architecture Patterns §3 for the fork-by-fork map. |
| IMPT-03 | `lead_score` calculado no momento do import | `LeadScoringService.CalculateScore` (line 111) is a `static`, stateless, per-customer method — already callable without touching the batch (`RecomputeAllAsync`) machinery. The "always Cold" mechanism is explained with file:line evidence in Architecture Patterns §4 — this is a math fact of the Phase-7-rebalanced formula, not a bug to patch. |

</phase_requirements>

## Summary

This phase touches almost no new technology — it is a pure application-logic change inside `CustomerImportService.cs`, `Customer.cs`, `ICustomerRepository`/`CustomerRepository`, and `ExtractorIntegrationService.cs`. The database schema IMPT-01 needs (`customers.external_id`, filtered unique index `IX_Customers_ExternalId`) was **already created and deployed to production in Phase 7** (migration `20260906101839_AddLeadQualitySignals`, confirmed by reading both the migration file and `CustomerConfiguration.cs`). This is the single most important finding for planning: **Phase 8 needs zero new EF Core migrations for IMPT-01**, unless the planner decides to persist the D-03 conflict counter as a DB column (optional — logging is a lighter alternative, see Open Questions).

IMPT-01 and IMPT-02 turn out to be the same code change viewed from two angles. Scraping's dedup is not "missing" — the persist-loop enrich path (`CustomerImportService.cs` line 407-440) already re-finds and updates existing customers by email for every source, proven by an existing passing test. What's missing is a *second* matching key (`ExternalId`) tried before email, to catch the one case email-based matching structurally cannot: the same business changing its scraped email between passes. Implementing `GetByExternalIdAsync` + a resolution order (ExternalId → email → phone) in `CustomerImportService` satisfies both requirements with one code path — the planner should not scope them as two separate implementations.

IMPT-03 is a genuine open design question, not a research gap. `LeadScoringService.CalculateScore` is a pure static function of `(Customer, CustomerEngagementSummary?, DateTime)` — trivially callable at import time with `engagement: null`. But doing so mathematically always returns Cold: the "fit" subscore (site/phone/eligibility/DDD) caps at 25, engagement contributes 0 at import (no emails sent yet), and Status is always freshly `Lead` (never `Qualified`) — so the score can never reach the 30-point Warm threshold. CONTEXT.md explicitly instructs the planner to pause and ask the user how to interpret IMPT-03 rather than silently redefining it; this research confirms the mechanism is real and unavoidable within the current scoring formula.

**Primary recommendation:** Implement one unified dedup resolution (ExternalId → email → phone) inside the existing `CustomerImportService.ImportAsync` persist loop, add `GetByExternalIdAsync` to `ICustomerRepository`, extend `ImportCustomerRow` with an optional `ExternalId` field populated by `ExtractorIntegrationService.MapToImportRow`, and call `LeadScoringService.CalculateScore` statically at customer-creation time for IMPT-03 — but confirm with the user first whether a guaranteed-Cold score satisfies IMPT-03's intent before writing that task.

## Standard Stack

No new libraries. This phase is 100% internal application/domain code using the stack already in place:

| Component | Version | Purpose | Evidence |
|-----------|---------|---------|----------|
| .NET / EF Core | net8.0 / EF Core 8.0.11 | ORM, migrations | `api-core/src/Diax.Infrastructure/Diax.Infrastructure.csproj` |
| xUnit | 2.9.2 | Test framework | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| Moq | 4.20.72 | Mocking | same csproj |
| SQL Server | `sql1002.site4now.net` / `db_aaf0a8_diaxcrm` | Production DB, target of all EF operations | `CLAUDE.md` |

**⚠️ Test library correction:** the root `CLAUDE.md` (system-level doc, possibly stale) says "Test libraries: xUnit, Moq, FluentAssertions." This is **wrong for this project as it stands today** — confirmed by reading the actual `.csproj` (no FluentAssertions package reference) and every existing test in `CustomerImportServiceTests.cs` (plain xUnit `Assert.Equal`/`Assert.True`/`Assert.Contains`, zero `Should()` calls). `08-CONTEXT.md` and the phase's own `additional_context` already flag this correctly. **Trust CONTEXT.md over the root CLAUDE.md on this point** — new tests must use `Assert`, not `Should()`.

**No installation needed** — no new NuGet packages for this phase.

## Architecture Patterns

### §1 — `ExternalId` schema: already live, do not re-create

`Customer.cs` (line 79-86) already declares the property, and `CustomerConfiguration.cs` (line 140-146) already declares the filtered unique index:

```csharp
// api-core/src/Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs:143-146
builder.HasIndex(c => c.ExternalId)
    .IsUnique()
    .HasFilter("[external_id] IS NOT NULL")
    .HasDatabaseName("IX_Customers_ExternalId");
```

This was applied via migration `20260906101839_AddLeadQualitySignals.cs` (line 74-79, `CreateIndex ... unique: true, filter: "[external_id] IS NOT NULL"`), already run against production per the Phase 7 checkpoint (`07-07-PLAN.md` wave 6). The `[external_id]` bracket syntax is the raw SQL Server column name after the project's PascalCase→snake_case convention — this is the correct EF Core 8 way to express a NULL-tolerant unique index on SQL Server (SQL Server treats all NULLs as *distinct* only under a filtered index; a plain unique index treats them as duplicates and would reject the second NULL row). `Customer.SetExternalId(string? externalId)` (`Customer.cs` line 298-301) already exists and normalizes whitespace to null, matching the filter's assumption.

**Type:** `ExternalId` is `string?`, `nvarchar(64)`. The source value (`ExtractorLead.Id`, `ExtractorService.cs:247`) is a C# `long` — so the mapping is `lead.Id.ToString()`, not a direct numeric column. This is a deliberate choice already baked into the schema (Phase 7); the planner does not need to revisit it.

**Action needed:** none on the schema/migration side, unless the planner adds a NEW column (e.g., a persisted D-03 conflict counter — see Open Questions). If that happens, follow the existing PascalCase→snake_case + nullable-int-with-default pattern from the same migration (`geo_rejected_count` etc., lines 35-54 of the migration file) and generate it with `api-core/scripts/add-migration.ps1`, apply with `update-db.ps1` — never `dotnet ef database update` directly (per `CLAUDE.md`).

### §2 — Dedup resolution order: exact code path to change

Today, `ICustomerRepository` (`api-core/src/Diax.Domain/Customers/ICustomerRepository.cs`) has `GetByEmailAsync` (line 14), `GetByPhoneAsync` (line 24), but **no `GetByExternalIdAsync`**. `CustomerRepository.cs` (line 16-33) implements the first two as simple `FirstOrDefaultAsync` predicates — the pattern to copy:

```csharp
// api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs:16-20 (existing pattern to mirror)
public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
{
    return await DbSet.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
}
```

Add: `Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)` to the interface and a matching `FirstOrDefaultAsync(c => c.ExternalId == externalId, ...)` implementation.

`CustomerImportService.ImportAsync` has **two structurally identical loops** that both do email-then-phone lookup: the validation-only loop (lines 93-276, gated to `DryRun || Source == Import`) and the real persist loop (lines 329-690, runs for every source). The persist loop is the one that matters for dedup (`existingCustomer` resolution at lines 407-440):

```csharp
// api-core/src/Diax.Application/Customers/CustomerImportService.cs:407-440 (current — email/phone only)
Customer? existingCustomer = null;
if (hasEmail)
{
    if (!seenEmailsInBatch.Add(sanitized.Email!)) { skippedCount++; ...continue; }
    existingCustomer = await _customerRepository.GetByEmailAsync(sanitized.Email!, cancellationToken);
}
else if (primaryPhone != null)
{
    if (!seenPhonesInBatch.Add(primaryPhone)) { skippedCount++; ...continue; }
    existingCustomer = await _customerRepository.GetByPhoneAsync(primaryPhone, cancellationToken);
}
```

**Recommended resolution order** (implements D-01 through D-04), inserted before the email/phone block:

```csharp
// Pseudocode — exact shape for the planner to task out
Customer? externalIdMatch = null;
if (!string.IsNullOrWhiteSpace(row.ExternalId))
{
    if (!seenExternalIdsInBatch.Add(row.ExternalId))   // batch-level dedup, mirrors seenEmailsInBatch
    {
        skippedCount++;
        errors.Add(new ImportError(i + 1, row.Email, $"Duplicata no lote: ExternalId '{row.ExternalId}' aparece mais de uma vez"));
        continue;
    }
    externalIdMatch = await _customerRepository.GetByExternalIdAsync(row.ExternalId, cancellationToken);
}

// ... existing email/phone lookup produces `emailOrPhoneMatch` ...

Customer? existingCustomer;
bool externalIdConflict = false;
if (externalIdMatch != null && emailOrPhoneMatch != null && externalIdMatch.Id != emailOrPhoneMatch.Id)
{
    // D-03: different Customer rows — email wins. Do NOT touch externalIdMatch.
    existingCustomer = emailOrPhoneMatch;
    externalIdConflict = true;   // count/log, see Open Questions
}
else if (externalIdMatch != null)
{
    existingCustomer = externalIdMatch;
    // D-01: email may need to change — see §... below for the opt-out guard (D-02)
}
else
{
    existingCustomer = emailOrPhoneMatch;
    // D-04: if this row carries an ExternalId and existingCustomer.ExternalId is null, backfill it
}
```

**D-01/D-02 guard (email swap + opt-out carry-over):** `Customer.UpdateBasicInfo` (`Customer.cs` line 256-268) only touches `Name/Email/PersonType/CompanyName/Document` — it does **not** touch `EmailOptOut`. This means the opt-out flag survives an in-place email update automatically, *as long as the implementation updates the existing row rather than deleting/recreating it*. Verified: every existing bounce/opt-out code path (`SendGridWebhookController.cs:279-282`, `ResendWebhookController.cs:252-255`, `EmailUnsubscribeController.cs:86`) calls `customer.OptOutEmail()` directly on the `Customer` row found by the email *at the time of the event* — so `EmailOptOut=true` is a durable property of the row, not of the email string. The **defensive gap** is `email_suppressions` (`EmailSuppression` entity, keyed by exact email string, `IEmailSuppressionRepository.IsSuppressedAsync(userId, email)`): the persist loop's existing suppression check (line 452-467) checks the row's **new incoming** email, never the customer's **old** email. In the normal case this doesn't matter (if `EmailOptOut` was set, it stays true regardless of the email swap). But if a suppression entry exists for the old email *without* `EmailOptOut` ever having been set on that row (a possible-but-rare orphan case — e.g., manually inserted via the suppression admin UI, or a suppression that predates the customer record), the swap would go unguarded. **Recommended mitigation:** before executing the email swap on an `externalIdMatch`, also call `_suppressionRepository.IsSuppressedAsync(userId, existingCustomer.Email)` for the OLD email and force `existingCustomer.OptOutEmail()` if true — belt-and-suspenders, cheap (one extra async call, only on the ExternalId-match-with-different-email branch).

### §3 — Scraping vs Import: every fork, and whether to unify

| Fork | Location | Only for `Import`? | Unify in Phase 8? | Reason |
|------|----------|---------------------|--------------------|--------|
| Pilot 10-lead limit + `PilotImportBlocked` event | `CustomerImportService.cs:67-78` | Yes | **No — keep divergent** | Explicit "piloto controlado" cap for manual cold-list uploads; Scraping pulls hundreds/day by design |
| Full validation block (ValidationStatus/ConsentStatus required, hard duplicate reject) | `CustomerImportService.cs:86-313` | Yes (`Source == Import`, or any `DryRun`) | **No — keep divergent** | Extrator rows never carry `ValidationStatus`/`ConsentStatus` (those are cold-list-vendor fields); requiring them would reject 100% of Scraping rows |
| Hard duplicate REJECT (`existingCustomer != null` → error, no enrich) | `CustomerImportService.cs:243-250` | Yes | **No — keep divergent** | Import's whole point is "never touch an existing record" (pilot safety net); Scraping's whole point is enrichment on re-pull. Unifying would break one or the other |
| Email/phone/**ExternalId** matching + enrich-in-place | `CustomerImportService.cs:407-440` (persist loop) | **No — already shared by both** | **This is the actual IMPT-01/IMPT-02 work** | Both sources go through this loop; adding `ExternalId` as a matching key here fixes both requirements with one change |
| `LogPilotEventAsync` calls | `CustomerImportService.cs:82, 286-303, 712-714` | Yes | **No — keep divergent** | Audit-log noise specific to the pilot program; irrelevant to a daily automated pull |

**Conclusion for the planner:** there is no separate "fix Scraping's dedup" task distinct from "add ExternalId dedup." Task them as one unit of work. The regression test that proves IMPT-02 is the *same* test family that proves IMPT-01 (see Test Strategy below) — specifically, extend the existing `Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport` test (line 434) with a variant where the second pull's email differs from the first but `ExternalId` is identical.

### §4 — IMPT-03: the "always Cold" mechanism, with evidence

`LeadScoringService.CalculateScore` (`LeadScoringService.cs:111-147`) is `public static int CalculateScore(Customer lead, CustomerEngagementSummary? engagement, DateTime nowUtc)` — no DI, no repository access, trivially callable from anywhere with a `Customer` instance:

```csharp
// api-core/src/Diax.Application/Customers/LeadScoringService.cs:111-147
public static int CalculateScore(Customer lead, CustomerEngagementSummary? engagement, DateTime nowUtc)
{
    var score = 0;
    // Fit de cadastro (máx 25)
    if (!string.IsNullOrWhiteSpace(lead.Website)) score += 5;
    if (!string.IsNullOrWhiteSpace(lead.Phone) || !string.IsNullOrWhiteSpace(lead.WhatsApp)) score += 5;
    if (lead.IsEligibleForCampaigns) score += 5;
    if (HasLocalDdd(lead.WhatsApp) || HasLocalDdd(lead.Phone)) score += 10;
    // Engajamento de email (máx 35) — engagement is null at import time → 0
    if (engagement != null) { ... }
    // Intenção qualificada — Status is always freshly Lead at import → 0
    if (lead.Status is CustomerStatus.Qualified or CustomerStatus.Negotiating) score += 15;
    if (lead.EmailOptOut) score -= 15;       // false for a brand-new lead
    if (lead.WhatsAppOptOut) score -= 15;    // false for a brand-new lead
    return Math.Clamp(score, 0, 100);
}
```

At import time, `engagement` is always `null` (no `EmailEventRepository` entries exist yet for a lead that hasn't been emailed), `lead.Status` is always `CustomerStatus.Lead` (set unconditionally in the constructor, `Customer.cs:233`), and `EmailOptOut`/`WhatsAppOptOut` are always `false` for a new row. So the ONLY reachable subscore is "fit de cadastro," hard-capped at 25 points by the comment on line 104-109 of `LeadScoringService.cs` itself ("vale no máximo 25, sempre abaixo do limiar Warm por si só"). `WarmThreshold = 30` (line 27). **25 < 30 always** — this is a closed-form mathematical guarantee, not something that varies by lead data. The segment-assignment ternary (`score >= HotThreshold ? Hot : score >= WarmThreshold ? Warm : Cold`, inlined at `LeadScoringService.cs:75-77` inside `RecomputeAllAsync`, **not extracted into a reusable helper**) will therefore always resolve to `Cold` for any lead scored at import.

This confirms CONTEXT.md's warning is accurate and mechanically unavoidable within the current formula — it is not something a smarter implementation of "call CalculateScore at import" can route around. The two paths CONTEXT.md offers (calculate anyway so `LeadScore` is never `null`, vs. reinterpreting the requirement) are the only real options; this research does not surface a third option that would let import-time score reach Warm without changing the scoring formula itself (which is explicitly out of scope — Phase 7 already rebalanced it, and re-touching it here would ripple into the 06h daily worker's Hot/Warm/Cold counts, an unrelated system already in production).

**Call-site guidance if the user picks "calculate anyway":** do NOT inject the full `LeadScoringService` (which pulls in `IEmailEventRepository`, `ITaskRepository`, `ILogger<LeadScoringService>` as constructor dependencies — see `LeadScoringService.cs:41-53`) into `CustomerImportService` just to call one static method. Call `LeadScoringService.CalculateScore(customer, null, DateTime.UtcNow)` directly as a static invocation, then apply the same threshold ternary used in `RecomputeAllAsync` (line 75-77) — extracting that ternary into a small shared static helper (e.g. `LeadScoringService.SegmentForScore(int score)`) avoids copy-pasting the `>= 60 / >= 30` literals a second time (see Don't Hand-Roll).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Filtered unique index for nullable dedup column | A new migration "creating" `external_id` | Nothing — already exists (`CustomerConfiguration.cs:143-146`, applied via `20260906101839_AddLeadQualitySignals`) | Re-adding it would fail with a duplicate-column/index error against production |
| In-batch duplicate detection for a new key | A bespoke `Dictionary`/`List.Contains` scan | `HashSet<string> seenExternalIdsInBatch`, mirroring `seenEmailsInBatch`/`seenPhonesInBatch` (`CustomerImportService.cs:325-326`) | Established pattern in the same method; O(1) lookup, same idiom reviewers already recognize |
| Hot/Warm/Cold threshold check | A second inline ternary at the import call site | Extract `RecomputeAllAsync`'s inline ternary (`LeadScoringService.cs:75-77`) into a small static helper and call it from both places | Two independent copies of `>= 60 / >= 30` WILL drift the day someone rebalances thresholds again (already happened once, Phase 7) |
| Customer lookup by ExternalId | Ad-hoc LINQ inside `CustomerImportService` | `ICustomerRepository.GetByExternalIdAsync`, implemented in `CustomerRepository.cs` mirroring `GetByEmailAsync`/`GetByPhoneAsync` (lines 16-33) | Keeps the repository as the single query surface, consistent with existing DI/testing (Moq mocks the interface, not EF directly) |

**Key insight:** almost everything this phase needs already has an established sibling pattern in the same two files (`CustomerImportService.cs`, `CustomerRepository.cs`). The risk is not "we don't know how" — it's copy-pasting a pattern without also extracting the parts that are about to be duplicated a second time (segment threshold logic).

## Common Pitfalls

### Pitfall 1: Assuming a new migration is needed for `ExternalId`
**What goes wrong:** planner tasks out "create migration for `Customer.ExternalId`," runner runs `add-migration.ps1`, gets a no-op diff or (worse) a name collision, wastes a cycle.
**Why it happens:** the phase description talks about "ExternalId com índice único" as if it's new; it was actually built in Phase 7 as a coordinated migration (D-07) specifically so Phase 8 wouldn't need one.
**How to avoid:** planner should explicitly note in the plan that IMPT-01's schema is DONE — the only migration risk in Phase 8 is IF a new persisted column is chosen for the D-03 conflict counter (optional, see Open Questions).
**Warning signs:** `add-migration.ps1` producing an empty `Up()`/`Down()` for `customers.external_id`, or an EF error `Column already exists`.

### Pitfall 2: Breaking `ImportCustomerRow` positional-record callers
**What goes wrong:** `ImportCustomerRow` (`Dtos/BulkImportDtos.cs:9-22`) is a positional record with 13 params today, constructed positionally in at least 3 places (`ExtractorIntegrationService.cs:369-379`, `ApifyIntegrationService.cs`, and every test in `CustomerImportServiceTests.cs`, e.g. `new("Lead A", "a@test.com", Phone: "27999000001")`). Inserting a new param anywhere but the very end, or making it non-nullable, breaks every positional caller at compile time.
**How to avoid:** add `string? ExternalId = null` as the LAST parameter of the record.

### Pitfall 3: Deleting/recreating the Customer row instead of updating in place
**What goes wrong:** loses `EmailOptOut`, `Notes`, `Tags`, `CreatedAt`, and the whole audit trail — directly violates Success Criterion 4 ("Nenhum Customer existente perde histórico/timeline") and reopens the D-02 compliance hole (a fresh row has `EmailOptOut = false` by default).
**How to avoid:** the resolution logic in §2 above must always route to `existingCustomer.UpdateBasicInfo(...)` / `existingCustomer.SetExternalId(...)` — never `new Customer(...)` when any match (ExternalId OR email OR phone) was found.
**Warning signs:** a test asserting `_customerRepository.AddAsync` was called when an ExternalId match existed.

### Pitfall 4: Suppression check only covers the incoming email, not the customer's prior email
**What goes wrong:** described in detail in §2 (D-01/D-02 guard) — a rare orphan-suppression scenario could let a swap through unguarded if `EmailOptOut` was never set on the row despite a suppression entry existing for the old email.
**How to avoid:** add the defensive old-email suppression check on the ExternalId-match-with-email-change branch specifically (not needed on every row — only on the swap path).
**Warning signs:** none observable without a targeted test — this is exactly the kind of bug that stays invisible until an incident report (see `docs/email-marketing/INCIDENTE-leads-extrator-2026-08-30.md`, the D-02 motivating precedent).

### Pitfall 5: Treating IMPT-03's "always Cold" result as a bug and trying to fix the formula
**What goes wrong:** rebalancing `LeadScoringService.CalculateScore`'s weights to let import-time scores reach Warm/Hot would change the score for EVERY lead system-wide, including the 06h daily `LeadScoringWorker` recompute — an unrelated, already-in-production surface. CONTEXT.md explicitly forbids this ("Se (b), parar e levantar a questão — não redefinir requisito sozinho").
**How to avoid:** do not touch `LeadScoringService.CalculateScore`'s point values in this phase. If IMPT-03 needs a different outcome, that's a scope conversation with the user, not a formula tweak.

### Pitfall 6: Injecting `LeadScoringService` into `CustomerImportService` for its DI dependencies
**What goes wrong:** `LeadScoringService`'s constructor (`LeadScoringService.cs:41-53`) requires `ICustomerRepository`, `IEmailEventRepository`, `ITaskRepository`, `IUnitOfWork`, `ILogger<LeadScoringService>` — pulling the whole service in just to call the static `CalculateScore` method adds unnecessary test-mock surface to every `CustomerImportServiceTests` test and creates a dependency that has nothing to do with import.
**How to avoid:** call `LeadScoringService.CalculateScore(customer, null, DateTime.UtcNow)` as a static invocation — it needs no instance.

## Code Examples

### Repository method to add (mirrors existing pattern exactly)
```csharp
// api-core/src/Diax.Domain/Customers/ICustomerRepository.cs — add near GetByEmailAsync (line 14)
Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

// api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs — add near line 20
public async Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
{
    return await DbSet.FirstOrDefaultAsync(c => c.ExternalId == externalId, cancellationToken);
}
```

### ImportCustomerRow extension (append-only, non-breaking)
```csharp
// api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs:9-22
public record ImportCustomerRow(
    string Name,
    string Email,
    string? Phone = null,
    string? WhatsApp = null,
    string? CompanyName = null,
    string? Notes = null,
    string? Tags = null,
    string? Website = null,
    string? City = null,
    string? CurrentTool = null,
    string? MainPain = null,
    string? ValidationStatus = null,
    string? ConsentStatus = null,
    string? ExternalId = null);   // ← NEW, must stay last
```

### `MapToImportRow` wiring (ExtractorIntegrationService.cs:328-380)
```csharp
// existing: noteParts.Add($"ID Extrator: {lead.Id}") at line 362 — keep for human readability
// add:
return new ImportCustomerRow(
    Name: name,
    Email: lead.Email ?? string.Empty,
    Phone: lead.Phone,
    WhatsApp: lead.WhatsApp ?? lead.Phone,
    CompanyName: lead.CompanyName,
    Notes: noteParts.Count > 0 ? string.Join("\n", noteParts) : null,
    Tags: string.Join(",", tags),
    Website: lead.Website,
    ExternalId: lead.Id > 0 ? lead.Id.ToString() : null   // ← NEW
);
```

### Existing test to model new tests after (verbatim precedent)
```csharp
// api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs:433-473
[Fact]
public async Task Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport()
{
    var rows = new List<ImportCustomerRow>
    {
        new("Lead A", "a@test.com", Phone: "27999000001"),
        new("Lead B", "b@test.com", Phone: "27999000002"),
    };
    var request = new BulkImportRequest(rows, LeadSource.Scraping);

    var created = new List<Customer>();
    _customerRepoMock
        .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
        .Callback<Customer, CancellationToken>((c, _) => created.Add(c))
        .ReturnsAsync((Customer c, CancellationToken _) => c);

    var first = await _sut.ImportAsync(request, "pull-1.json");
    Assert.Equal(2, first.SuccessCount);

    _customerRepoMock
        .Setup(r => r.GetByEmailAsync("a@test.com", It.IsAny<CancellationToken>()))
        .ReturnsAsync(created[0]);
    // ... second pull assertions
}
```
This is the exact shape new IMPT-01/02 tests should follow — extend it with a variant where the second pull sends a *different* email but the same `ExternalId`, and setup `GetByExternalIdAsync` instead of/alongside `GetByEmailAsync`.

## Migration Safety

**No new migration is required for IMPT-01.** The column and filtered unique index are already in production:

| Step | Status |
|------|--------|
| Add nullable `external_id` column | ✅ Done — migration `20260906101839_AddLeadQualitySignals.cs:14-19`, applied per Phase 7 checkpoint |
| Backfill existing ~2300 Scraping-sourced customers | ❌ Explicitly decided AGAINST (CONTEXT.md Claude's Discretion) — organic backfill via D-04 (email-match enrichment) is the chosen mechanism |
| Add filtered unique index | ✅ Done — same migration, lines 74-79 |

**Backfill feasibility (for context only, not a task):** whether the ~2300 already-imported customers' `Notes` field contains a recoverable `lead.Id` (via the `"ID Extrator: {lead.Id}"` line, `ExtractorIntegrationService.cs:361-362`, present in the code since before both the 04/09 and 05/09 production pulls per git history) is unresolved by this research — the phase's own `additional_context` states production notes are shaped as `Origem: Extrator de Dados Cidade: X Estado: Y Fonte:` with **no** `ID Extrator:` marker, which does not match what the current code produces. This is a genuine discrepancy this research could not settle without a live read-only query against production (`SELECT COUNT(*) FROM customers WHERE notes LIKE '%ID Extrator:%' AND external_id IS NULL`). It does not block planning since backfill is explicitly out of scope — flagged here only so the planner doesn't accidentally rely on the `additional_context` claim as fact if the topic resurfaces (see Open Questions).

**If the planner adds a NEW column** (e.g., persisted D-03 conflict counter): follow the exact pattern of `20260906101839_AddLeadQualitySignals.cs` lines 28-54 (`AddColumn<int>` with `defaultValue: 0`, `nullable: false`), generate via `api-core/scripts/add-migration.ps1`, apply via `api-core/scripts/update-db.ps1` — never `dotnet ef database update` directly against `sql1002.site4now.net`.

## Test Strategy

**Framework:** xUnit 2.9.2 + Moq 4.20.72. **No FluentAssertions** — use `Assert.Equal`/`Assert.True`/`Assert.False`/`Assert.Contains` only (see Standard Stack correction above).

**File:** `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` — all new tests go here, following the existing constructor-injected-mocks pattern (lines 26-89: one `Mock<T>` field per dependency, wired in the constructor, `_sut` built once per test class instance).

**Pattern to follow exactly:** see `Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport` (line 434, reproduced above) for the "two sequential `ImportAsync` calls, second one re-finds via a repository mock" shape, and `Import_ShouldReject_WhenLeadIsDuplicateInDB` (line 408) for the single-match assertion shape.

**Build/run command (always `-c Release` — Smart App Control blocks Debug test DLLs):**
```bash
cd api-core
dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release
```

**New tests to add:**
- `Import_ExternalIdMatch_UpdatesEmail_WhenEmailChanged` (D-01) — setup `GetByExternalIdAsync` to return an existing customer with a different email than the incoming row; assert `UpdateBasicInfo` effect (new email persisted) and `AddAsync` never called.
- `Import_ExternalIdMatch_PreservesEmailOptOut_WhenEmailChanged` (D-02) — same setup, but existing customer has `EmailOptOut = true`; assert the updated customer still has `EmailOptOut == true` after the swap.
- `Import_ExternalIdAndEmailMatchDifferentCustomers_EmailWins_LogsConflict` (D-03) — setup `GetByExternalIdAsync` → customer A, `GetByEmailAsync` → customer B (different Id); assert the row enriches B, and A's `ExternalId` is untouched.
- `Import_EmailMatchWithExternalId_BackfillsExternalIdOnExistingCustomer` (D-04) — setup `GetByExternalIdAsync` → null, `GetByEmailAsync` → existing customer with `ExternalId == null`; assert `SetExternalId` effect after import.
- `Import_Scraping_DoublePull_SameExternalId_DifferentEmail_NoDuplicateCreated` (IMPT-01 + IMPT-02 combined regression) — direct extension of the existing `Import_DoublePull_IsIdempotent...` test with a changed email on the second pull.
- IMPT-03 test(s) — deferred until the "calculate anyway vs. reinterpret" question is resolved with the user; do not write until that's settled.

**Baseline:** 833 tests passing before this phase starts (per `08-CONTEXT.md`). All new work must keep this suite green.

## Risks and Pitfalls

(Consolidated from Common Pitfalls above — presented here per the requested research-focus format.)

1. **Migration duplication risk** → Mitigation: plan explicitly states IMPT-01 schema is already live; no migration task unless a new column is added.
2. **Breaking `ImportCustomerRow` callers** → Mitigation: new field appended last, nullable, default `null`.
3. **Losing audit trail / EmailOptOut via delete+recreate** → Mitigation: resolution logic always updates in place, never `new Customer(...)` when any match exists.
4. **Orphan suppression not caught on email swap** → Mitigation: defensive old-email suppression check on the swap branch only.
5. **"Fixing" the always-Cold score by rebalancing weights** → Mitigation: explicitly forbidden by CONTEXT.md; raise as a question instead.
6. **Over-injecting `LeadScoringService`** → Mitigation: call `CalculateScore` statically.
7. **In-batch ExternalId collision** (two rows in the same pull sharing an ExternalId — shouldn't happen given the Extrator's own uniqueness, but paranoia is cheap) → Mitigation: `seenExternalIdsInBatch` HashSet, mirroring existing email/phone batch dedup.
8. **D-03 conflict silently invisible** → Mitigation: at minimum, `ILogger` a structured warning with both customer IDs and the row's email/ExternalId; consider a persisted counter only if the user wants queryable history (see Open Questions).

## Open Questions

1. **IMPT-03 interpretation — calculate at import knowing it's always Cold, or redefine the requirement?**
   - What we know: the math is unavoidable within the current (Phase-7-rebalanced) scoring formula — confirmed with file:line evidence in Architecture Patterns §4.
   - What's unclear: whether the user considers "LeadScore is populated (even if always 0-25/Cold) instead of null" a satisfying delivery of IMPT-03, or whether they want the requirement reinterpreted (e.g., "score gets recalculated in near-real-time after the FIRST engagement event, not literally at import" — which would be a different, larger feature).
   - Recommendation: this is explicitly flagged in CONTEXT.md as a "stop and ask" item. The planner should raise it as the first question to the user before writing IMPT-03 tasks, rather than picking an interpretation.

2. **D-03 conflict observability — log line or persisted counter?**
   - What we know: `ImportRejectionCounts`/`CustomerImport.RecordRejectionCounts` (`CustomerImport.cs:98`) already has a proven pattern for persisted per-round counters (geo/low-quality-email/no-mx/duplicate), and CONTEXT.md's "Reusable Assets" note flags it as a candidate.
   - What's unclear: whether a 5th counter column (`external_id_conflict_count`) is worth a migration for what should be a rare event, versus just a structured `ILogger` warning that's greppable in Serilog/production logs.
   - Recommendation: default to `ILogger` warning (zero schema cost, consistent with D-05 from Phase 7 — "banco em 467/500 MB de quota" was the exact reason a per-lead rejection table was rejected there too). Only add a counter if the user specifically wants queryable/dashboard-visible D-03 history.

3. **Notes-format discrepancy for already-imported customers (see Migration Safety).**
   - What we know: current code embeds `"ID Extrator: {lead.Id}"` in `Notes`, present since before both production pulls (git history).
   - What's unclear: whether the actual ~2300 already-imported production rows match this format or the `"Origem: Extrator de Dados..."` format described in the phase's `additional_context`.
   - Recommendation: irrelevant to task-planning since backfill is out of scope either way — flagging only so a future backfill conversation starts from a verified fact, not a possibly-stale claim. A single read-only `SELECT` against production would settle it in seconds if it ever matters.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.2 + Moq 4.20.72 (no FluentAssertions) |
| Config file | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| Quick run command | `cd api-core && dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release` |
| Full suite command | `cd api-core && dotnet test -c Release` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| IMPT-01 | ExternalId match updates existing Customer in place (email changed) | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_UpdatesEmail" -c Release` | ❌ new test, same file |
| IMPT-01 | Opt-out survives email swap via ExternalId match | unit | `dotnet test --filter "FullyQualifiedName~Import_ExternalIdMatch_PreservesEmailOptOut" -c Release` | ❌ new test |
| IMPT-01 | ExternalId/email conflict resolves to email, conflict observable | unit | `dotnet test --filter "FullyQualifiedName~ExternalIdAndEmailMatchDifferentCustomers" -c Release` | ❌ new test |
| IMPT-01 | Email match backfills ExternalId | unit | `dotnet test --filter "FullyQualifiedName~BackfillsExternalIdOnExistingCustomer" -c Release` | ❌ new test |
| IMPT-02 | Scraping double-pull with changed email, same ExternalId, no duplicate | unit | `dotnet test --filter "FullyQualifiedName~Scraping_DoublePull_SameExternalId" -c Release` | ❌ new test (extends existing `Import_DoublePull_IsIdempotent...`, line 434) |
| IMPT-03 | LeadScore populated at import (pending scope decision) | unit | TBD — blocked on Open Question 1 | ❌ not yet designed |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release`
- **Per wave merge:** `dotnet test -c Release` (full 833+ baseline)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
None — existing test infrastructure (`CustomerImportServiceTests.cs`, mock-based constructor pattern, `-c Release` convention) covers all phase requirements. No new framework, fixture, or config file needed; only new `[Fact]` methods in the existing file plus mock setups for the new `GetByExternalIdAsync` method.

## Sources

### Primary (HIGH confidence — direct file reads of the actual repo)
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` — full read, all line numbers verified
- `api-core/src/Diax.Domain/Customers/Customer.cs` — full read
- `api-core/src/Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs` — full read, confirms filtered unique index already live
- `api-core/src/Diax.Infrastructure/Data/Migrations/20260906101839_AddLeadQualitySignals.cs` — full read, confirms migration already applied
- `api-core/src/Diax.Application/Customers/LeadScoringService.cs` — full read
- `api-core/src/Diax.Infrastructure/Workers/LeadScoringWorker.cs` — full read
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` — full read
- `api-core/src/Diax.Application/Customers/Dtos/BulkImportDtos.cs` — full read
- `api-core/src/Diax.Domain/Customers/ICustomerRepository.cs` — full read
- `api-core/src/Diax.Infrastructure/Data/Repositories/CustomerRepository.cs` — partial read (first 45 lines, dedup methods)
- `api-core/src/Diax.Domain/EmailMarketing/EmailSuppression.cs`, `IEmailSuppressionRepository.cs` — full read
- `api-core/src/Diax.Application/EmailMarketing/Pro/SuppressionService.cs` — full read
- `api-core/src/Diax.Api/Controllers/V1/SendGridWebhookController.cs`, `ResendWebhookController.cs`, `EmailUnsubscribeController.cs` — grep-verified `OptOutEmail()` + `EmailSuppression.ForEmail` pairing
- `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` — full test list read, two representative tests read in full
- `api-core/tests/Diax.Tests/Diax.Tests.csproj` — grep-verified package references (no FluentAssertions)
- `api-core/src/Diax.Application/Customers/Services/ExtractorService.cs` — grep-verified `ExtractorLead.Id` type (`long`)
- Git history (`git log -p`) on `ExtractorIntegrationService.cs` — confirmed "ID Extrator" note text present since the file's first commit (Apr 19 2026)

### Secondary (MEDIUM confidence)
- `.planning/phases/08-import-—-dedup-e-score-em-tempo-real/08-CONTEXT.md` — user decisions, treated as authoritative per task instructions
- `.planning/phases/07-extra-o-qualidade-na-entrada/07-CONTEXT.md` — confirms D-07 (migration coordination) and its rationale

### Tertiary (LOW confidence — flagged, not used as fact)
- The task's own `additional_context` claim about production `Notes` format (`"Origem: Extrator de Dados Cidade: X Estado: Y Fonte:"`) — contradicts what the current code produces; noted as an open discrepancy in Open Questions §3, not resolved by this research (would require a live production query).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries, versions confirmed via `.csproj` grep
- Architecture (dedup resolution, scoring call site): HIGH — every claim backed by file:line reads, one existing passing test used as evidence
- Migration safety: HIGH — migration file read directly, confirms already-applied state
- Pitfalls: HIGH — each pitfall traced to a specific existing code behavior, not speculative
- IMPT-03 scope question: N/A — correctly left open per explicit CONTEXT.md instruction, not a research gap

**Research date:** 2026-09-07
**Valid until:** stable — this is internal application code with no external dependency drift risk; valid until the next phase touches the same files (would need a quick re-read of `CustomerImportService.cs` for line-number drift, not a full re-research)

---

*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Research completed: 2026-09-07*
