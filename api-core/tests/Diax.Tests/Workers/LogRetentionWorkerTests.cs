using Diax.Application.Audit;
using Diax.Application.ErrorLogs;
using Diax.Application.Logs;
using Diax.Infrastructure.Workers;
using Diax.Shared.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Diax.Tests.Workers;

public class LogRetentionWorkerTests
{
    private static IConfiguration Config(string? days) => new ConfigurationBuilder()
        .AddInMemoryCollection(days is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?> { [LogRetentionWorker.ConfigKey] = days })
        .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("29")]
    public void ResolveRetentionDays_DisabledOrBelowMinimum_ReturnsNull(string? value)
    {
        Assert.Null(LogRetentionWorker.ResolveRetentionDays(Config(value)));
    }

    [Fact]
    public void ResolveRetentionDays_Valid_ReturnsDays()
    {
        Assert.Equal(90, LogRetentionWorker.ResolveRetentionDays(Config("90")));
    }

    [Fact]
    public async Task RunCleanupAsync_CallsAllThreeCleanupsWithSameRetention()
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(s => s.CleanupOldLogsAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(3));
        var app = new Mock<IAppLogService>();
        app.Setup(s => s.CleanupAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(5));
        var error = new Mock<IErrorLogService>();
        error.Setup(s => s.CleanupAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(7));

        var sp = new ServiceCollection()
            .AddSingleton(audit.Object)
            .AddSingleton(app.Object)
            .AddSingleton(error.Object)
            .BuildServiceProvider();

        var result = await LogRetentionWorker.RunCleanupAsync(sp, 90, CancellationToken.None);

        Assert.Equal((3, 5, 7), result);
    }
}
