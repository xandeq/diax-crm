using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

public class EmailDispatchContractTests
{
    // Helper para criar uma EmailMessage válida
    private static EmailMessage MakeMessage(string fromDomain = "alexandrequeiroz.com.br") =>
        new()
        {
            From = new EmailAddress($"contato@{fromDomain}", "Teste"),
            To = [new EmailAddress("dest@example.com", "Destinatário")],
            Subject = "Teste",
            Html = "<p>Teste</p>"
        };

    [Fact]
    public async Task Dispatch_WithSuccessfulProvider_ReturnsSent()
    {
        // Arrange
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), null, null, Guid.NewGuid().ToString(), false);
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(
               true, EmailDispatchStatus.Sent, "msg_123", "resend", false, []));

        // Act
        var result = await svc.Object.DispatchAsync(req);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(EmailDispatchStatus.Sent, result.Status);
        Assert.Equal("resend", result.ProviderUsed);
        Assert.False(result.AllowUnaligned);
    }

    [Fact]
    public async Task Dispatch_WithIdempotencyKeyAlreadySent_ReturnsDuplicate()
    {
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), "briefing-20260630", null, Guid.NewGuid().ToString(), false);
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(
               true, EmailDispatchStatus.Duplicate, "msg_original", "brevo", false, []));

        var result = await svc.Object.DispatchAsync(req);

        Assert.Equal(EmailDispatchStatus.Duplicate, result.Status);
        Assert.Equal("msg_original", result.MessageId);
    }

    [Fact]
    public async Task Dispatch_WithIdempotencyKeyInFlight_ReturnsInProgress()
    {
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), "briefing-20260630", null, Guid.NewGuid().ToString(), false);
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(
               false, EmailDispatchStatus.InProgress, null, null, false, []));

        var result = await svc.Object.DispatchAsync(req);

        Assert.Equal(EmailDispatchStatus.InProgress, result.Status);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Dispatch_WhenAllProvidersFail_ReturnsAllFailed()
    {
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), null, null, Guid.NewGuid().ToString(), false);
        var attempts = new List<EmailAttemptDetail>
        {
            new("resend", 1, false, null, "timeout", 15000),
            new("brevo",  1, false, null, "rate_limited", 100),
        };
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(false, EmailDispatchStatus.AllFailed, null, null, false, attempts));

        var result = await svc.Object.DispatchAsync(req);

        Assert.Equal(EmailDispatchStatus.AllFailed, result.Status);
        Assert.Equal(2, result.Attempts.Count);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Dispatch_WithAllowUnaligned_True_AndTier1Failed_UsesT2AndFlagsLog()
    {
        // Garante que allowUnaligned=true permite fallback Tier 2 e o result reflete isso
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), null, null, Guid.NewGuid().ToString(), AllowUnaligned: true);
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(
               true, EmailDispatchStatus.Sent, null, "hostgator-smtp", AllowUnaligned: true,
               [new("hostgator-smtp", 1, true, null, null, 890)]));

        var result = await svc.Object.DispatchAsync(req);

        Assert.True(result.Success);
        Assert.True(result.AllowUnaligned); // foi via Tier 2
        Assert.Equal("hostgator-smtp", result.ProviderUsed);
    }

    [Fact]
    public async Task Dispatch_WithAllowUnaligned_False_AndTier1Failed_ReturnsAllFailed_NeverTier2()
    {
        // Garante que allowUnaligned=false nunca tenta Tier 2
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage(), null, null, Guid.NewGuid().ToString(), AllowUnaligned: false);
        var attempts = new List<EmailAttemptDetail> { new("resend", 1, false, null, "timeout", 15000) };
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(false, EmailDispatchStatus.AllFailed, null, null, false, attempts));

        var result = await svc.Object.DispatchAsync(req);

        Assert.Equal(EmailDispatchStatus.AllFailed, result.Status);
        // Verifica que nenhuma tentativa usou provider Tier 2
        Assert.DoesNotContain(result.Attempts, a =>
            a.Provider.Contains("smtp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Dispatch_WithUnknownSenderDomain_ReturnsRejected()
    {
        var svc = new Mock<IEmailDispatchService>();
        var req = new EmailDispatchRequest(MakeMessage("unknown.com.br"), null, null, Guid.NewGuid().ToString(), false);
        svc.Setup(s => s.DispatchAsync(req, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new EmailDispatchResult(false, EmailDispatchStatus.Rejected, null, null, false, []));

        var result = await svc.Object.DispatchAsync(req);

        Assert.Equal(EmailDispatchStatus.Rejected, result.Status);
        Assert.False(result.Success);
    }
}
