using Diax.Api.Configuration;
using Diax.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Diax.Tests.Infrastructure;

public class DatabaseReadyHealthCheckTests
{
    [Fact]
    public void Evaluate_ConectaSemPendencia_Healthy() =>
        Assert.Equal(HealthStatus.Healthy, DatabaseReadyHealthCheck.Evaluate(true, 0).Status);

    [Fact]
    public void Evaluate_MigrationPendente_Unhealthy()
    {
        var r = DatabaseReadyHealthCheck.Evaluate(true, 2);
        Assert.Equal(HealthStatus.Unhealthy, r.Status);
        Assert.Equal("2 pending migration(s)", r.Description);
    }

    [Fact]
    public void Evaluate_SemConexao_Unhealthy() =>
        Assert.Equal(HealthStatus.Unhealthy, DatabaseReadyHealthCheck.Evaluate(false, 0).Status);

    [Fact]
    public async Task BancoInalcancavel_Unhealthy_SemVazarExcecao()
    {
        // Porta 1 no loopback: recusa na hora, sem rede externa.
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseSqlServer("Server=tcp:127.0.0.1,1;Database=x;Trusted_Connection=True;Connect Timeout=2;Encrypt=false")
            .Options;
        await using var db = new DiaxDbContext(options);

        var r = await new DatabaseReadyHealthCheck(db).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, r.Status);
        Assert.Equal("database unreachable", r.Description);
    }
}
