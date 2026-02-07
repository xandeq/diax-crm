using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Diax.Domain.Common;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Endpoint temporário de diagnóstico para investigar mismatch de UserId.
/// REMOVER após resolver o problema de Income.NotFound / Expense.NotFound.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/debug")]
[Produces("application/json")]
public class DebugController : BaseApiController
{
    private readonly DiaxDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public DebugController(DiaxDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet("ownership")]
    public async Task<IActionResult> GetOwnershipDiagnostics(CancellationToken ct)
    {
        // 1. Email do JWT
        var jwtEmail = User.FindFirstValue(ClaimTypes.Email)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        // 2. UserId via ResolveUserIdAsync (mesmo método usado pelos controllers)
        var resolvedUserId = await ResolveUserIdAsync(_db, ct);

        // 3. UserId via CurrentUserService (usado pelo Global Query Filter)
        var currentServiceUserId = _currentUserService.UserId;

        // 4. User no banco (sem query filter)
        var userInDb = await _db.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Email == (jwtEmail ?? ""))
            .Select(u => new { u.Id, u.Email, u.IsActive })
            .FirstOrDefaultAsync(ct);

        // 5. Sample records (sem query filter para ver o que existe)
        var sampleIncome = await _db.Incomes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.UserId, i.Description, i.CreatedAt })
            .FirstOrDefaultAsync(ct);

        var sampleExpense = await _db.Expenses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.UserId, e.Description, e.CreatedAt })
            .FirstOrDefaultAsync(ct);

        // 6. Contagens COM e SEM query filter
        var totalIncomesWithFilter = await _db.Incomes.AsNoTracking().CountAsync(ct);
        var totalIncomesWithoutFilter = await _db.Incomes.AsNoTracking().IgnoreQueryFilters().CountAsync(ct);

        var totalExpensesWithFilter = await _db.Expenses.AsNoTracking().CountAsync(ct);
        var totalExpensesWithoutFilter = await _db.Expenses.AsNoTracking().IgnoreQueryFilters().CountAsync(ct);

        // 7. Contagem de registros por UserId (top 5)
        var incomesByUser = await _db.Incomes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .GroupBy(i => i.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(ct);

        var expensesByUser = await _db.Expenses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(ct);

        return Ok(new
        {
            jwtEmail,
            resolvedUserId,
            currentServiceUserId,
            match = resolvedUserId == currentServiceUserId,
            userInDb,
            sampleIncome,
            sampleExpense,
            totalIncomesWithFilter,
            totalIncomesWithoutFilter,
            totalExpensesWithFilter,
            totalExpensesWithoutFilter,
            incomesByUser,
            expensesByUser
        });
    }
}
