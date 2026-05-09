# Cash Flow Projection — Integration Endpoint for InvestIQ

**Date:** 2026-04-29
**Status:** Approved scope; ready for `writing-plans`
**Companion doc:** [InvestIQ-side design](../../../investiq/docs/plans/2026-04-29-cash-parking-feature-design.md)

DIAX side of the bidirectional integration. InvestIQ already pushes portfolio data to DIAX (working via `InvestIQController`); now DIAX exposes cash flow projection so InvestIQ can recommend cash parking based on real upcoming bills.

## What to build

### 1. Controller

`api-core/src/Diax.Api/Controllers/V1/IntegrationsController.cs`

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integrations")]
[AllowAnonymous]
public class IntegrationsController : ControllerBase
{
    [HttpGet("cash-flow-projection")]
    public async Task<IActionResult> GetCashFlowProjection(
        [FromHeader(Name = "X-Integration-Key")] string integrationKey,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct);
}
```

Auth: validate `integrationKey` against `Integrations:InvestIQKey` config. Return 401 on mismatch, 503 if config missing.

Tenant: read `Integrations:DefaultUserId` from config; resolve to `User.Id`. No JWT required.

### 2. Service

`api-core/src/Diax.Application/Integrations/CashFlowProjectionIntegrationService.cs`

- Reuse existing `CashFlowProjectionService.ProjectDailyBalances(...)` — do not reimplement
- Reuse the `availableToInvest` calculation already present in `PersonalControlSummary` (`personalControlService.ts` mirrors it; keep the C# canonical source)
- Compute `nextBigOutflow` = next single Expense or Recurring occurrence ≥ R$ 1.000 in window

### 3. DTO

`api-core/src/Diax.Application/Integrations/Dtos/CashFlowProjectionResponse.cs`

```csharp
public record CashFlowProjectionResponse(
    decimal CurrentBalance,
    decimal AvailableToInvest,
    NextBigOutflow? NextBigOutflow,
    List<DailyBalanceItem> DailyProjection
);

public record NextBigOutflow(DateTime Date, decimal Amount, string Description);
public record DailyBalanceItem(
    DateTime Date,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal ClosingBalance,
    bool IsNegative,
    bool HasHighPriorityExpense);
```

### 4. Config

`appsettings.json`:
```json
"Integrations": {
  "InvestIQKey": "",
  "DefaultUserId": ""
}
```

Production via env var: `DIAX_Integrations__InvestIQKey`, `DIAX_Integrations__DefaultUserId` (priority 1 in DIAX config pipeline).

### 5. Tests

- **Unit** (`Diax.UnitTests`): mock repos; verify `availableToInvest` parity with PersonalControl page; verify `nextBigOutflow` filtering
- **Integration** (`Diax.IntegrationTests`): controller with/without header, with bad key, with missing config

## Non-goals

- No write endpoints — InvestIQ is read-only consumer
- No webhook — InvestIQ pulls (Variant A in companion doc)
- No GraphQL — single REST endpoint

## Symmetry with existing InvestIQ → DIAX integration

| Direction | Endpoint | Auth | Cache |
|---|---|---|---|
| InvestIQ → DIAX (existing) | `/integrations/portfolio-summary` | `X-Integration-Key` | DIAX `IMemoryCache` 1h |
| DIAX → InvestIQ (new) | `/api/v1/integrations/cash-flow-projection` | `X-Integration-Key` | InvestIQ Redis 1h |

Same auth pattern, same TTL, mirror direction. Each side stores the other's key in its own secrets.

## Effort estimate

~2 hours including tests.
