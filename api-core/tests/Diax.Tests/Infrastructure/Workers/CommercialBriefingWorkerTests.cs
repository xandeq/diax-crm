using Diax.Infrastructure.Workers;

namespace Diax.Tests.Infrastructure.Workers;

public class CommercialBriefingWorkerTests
{
    [Fact]
    public void Enviado_MarcaODia() =>
        Assert.True(CommercialBriefingWorker.ShouldMarkRun(sent: true, attemptsToday: 1));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FalhaAntesDoLimite_NaoMarca_TentaDeNovo(int attempts) =>
        Assert.False(CommercialBriefingWorker.ShouldMarkRun(sent: false, attemptsToday: attempts));

    [Fact]
    public void FalhaNoLimite_MarcaEDesisteAteAmanha() =>
        Assert.True(CommercialBriefingWorker.ShouldMarkRun(sent: false, attemptsToday: CommercialBriefingWorker.MaxAttemptsPerDay));
}
