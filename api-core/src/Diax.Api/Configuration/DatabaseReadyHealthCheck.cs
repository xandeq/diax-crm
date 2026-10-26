using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Diax.Api.Configuration;

/// <summary>
/// /health/ready: o banco responde e não há migration pendente. O "self" só prova que o processo
/// subiu; uma migration que falhou no startup (Program.cs segue de pé mesmo assim) só aparece aqui.
/// Não expõe nomes de migration nem mensagem de exceção (endpoint anônimo).
/// </summary>
public sealed class DatabaseReadyHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly DiaxDbContext _db;

    public DatabaseReadyHealthCheck(DiaxDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cts.Token);
            var pending = canConnect ? (await _db.Database.GetPendingMigrationsAsync(cts.Token)).Count() : 0;
            return Evaluate(canConnect, pending);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Evaluate(canConnect: false, pendingMigrations: 0);
        }
    }

    public static HealthCheckResult Evaluate(bool canConnect, int pendingMigrations) =>
        !canConnect ? HealthCheckResult.Unhealthy("database unreachable")
        : pendingMigrations > 0 ? HealthCheckResult.Unhealthy($"{pendingMigrations} pending migration(s)")
        : HealthCheckResult.Healthy("database ready");
}
