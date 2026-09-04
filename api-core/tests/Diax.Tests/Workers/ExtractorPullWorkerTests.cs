using Diax.Application.Customers;
using Diax.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Workers;

public class ExtractorPullWorkerTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();

    private ExtractorPullWorker CreateWorker(ExtractorPullOptions options) =>
        new(
            _scopeFactoryMock.Object,
            Options.Create(options),
            Mock.Of<ILogger<ExtractorPullWorker>>());

    [Fact]
    public async Task Worker_Disabled_DoesNotTriggerImport()
    {
        var worker = CreateWorker(new ExtractorPullOptions { Enabled = false });

        await worker.StartAsync(CancellationToken.None);

        // Com Enabled=false o ExecuteAsync retorna imediatamente, sem loop.
        Assert.NotNull(worker.ExecuteTask);
        await worker.ExecuteTask!;
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);

        // Nenhum scope criado = nenhum ImportLeadsAsync disparado.
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_Enabled_WaitsForDailySlot_NoImmediateImport()
    {
        // Enabled=true mas dentro do poll de 15 min o worker só espera —
        // nunca dispara import imediatamente no startup.
        var worker = CreateWorker(new ExtractorPullOptions { Enabled = true, DailyHourUtc = 15 });

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Options_Defaults_AreSafe()
    {
        var options = new ExtractorPullOptions();

        // Worker nasce DESLIGADO — liga só por config/env (DIAX_ExtractorPull__Enabled).
        Assert.False(options.Enabled);
        // 15 UTC = 12:00 BRT, depois da janela de envio da manhã.
        Assert.Equal(15, options.DailyHourUtc);
        Assert.Equal(10, options.MaxPages);

        // TLDs bloqueados default — e NUNCA .com/.com.br.
        Assert.Equal(
            new[] { ".es", ".fi", ".eu", ".ar", ".cl", ".mx", ".pt" },
            options.BlockedTlds);
        Assert.DoesNotContain(".com", options.BlockedTlds);
        Assert.DoesNotContain(".com.br", options.BlockedTlds);
        Assert.DoesNotContain(".br", options.BlockedTlds);

        Assert.Equal(
            new[] { "sun.com", "blok.ai", "overchat.ai", "redcross.org", "fox.com", "foxtv.com" },
            options.BlockedDomains);
    }
}
