using Diax.Application.EmailMarketing;
using Diax.Infrastructure.Email;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Breaker POR PROVIDER do dispatch unificado — cobre P0-4 (half-open/auto-close/reset;
/// antes era latching: uma vez aberto, só fechava com restart) e P0-5 (bounce não é
/// erro crítico de autenticação).
/// </summary>
public class EmailProviderCircuitBreakerTests
{
    private static readonly TimeSpan TestCooldown = TimeSpan.FromMilliseconds(60);

    [Fact]
    public void SingleBounce_DoesNotOpenBreaker()
    {
        // Regressão P0-5: 1 bounce (problema do DESTINATÁRIO) abria o breaker para
        // sempre e derrubava o provider inteiro até restart.
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);

        breaker.RecordFailure("brevo", "recipient address produced a hard bounce");

        Assert.False(breaker.IsOpen("brevo"));
    }

    [Fact]
    public void CriticalAuthError_OpensImmediately()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);

        breaker.RecordFailure("brevo", "401 invalid api key");

        Assert.True(breaker.IsOpen("brevo"));
    }

    [Fact]
    public void ErrorRateAboveThreshold_Opens()
    {
        var breaker = new EmailProviderCircuitBreaker(TimeSpan.FromMinutes(5));

        breaker.RecordFailure("brevo", "erro 500");
        breaker.RecordFailure("brevo", "erro 500");
        breaker.RecordFailure("brevo", "erro 500"); // 3/3 = 100% >= 30%

        Assert.True(breaker.IsOpen("brevo"));
    }

    [Fact]
    public void RateLimit429_IsIgnored()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);

        for (var i = 0; i < 5; i++)
            breaker.RecordFailure("brevo", "429 too many requests");

        Assert.False(breaker.IsOpen("brevo"));
    }

    [Fact]
    public async Task AfterCooldown_AllowsExactlyOneProbe()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);
        breaker.RecordFailure("brevo", "401 unauthorized");
        Assert.True(breaker.IsOpen("brevo"));

        await Task.Delay(TestCooldown + TimeSpan.FromMilliseconds(40));

        Assert.False(breaker.IsOpen("brevo")); // half-open: libera UMA prova
        Assert.True(breaker.IsOpen("brevo"));  // as demais continuam bloqueadas
    }

    [Fact]
    public async Task ProbeSuccess_ClosesBreakerCompletely()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);
        breaker.RecordFailure("brevo", "401 unauthorized");
        await Task.Delay(TestCooldown + TimeSpan.FromMilliseconds(40));

        Assert.False(breaker.IsOpen("brevo")); // prova liberada
        breaker.RecordSuccess("brevo");        // prova ok → fecha

        Assert.False(breaker.IsOpen("brevo"));
        Assert.False(breaker.IsOpen("brevo")); // fechado de verdade, não half-open
    }

    [Fact]
    public async Task ProbeFailure_ReopensWithFreshCooldown()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);
        breaker.RecordFailure("brevo", "401 unauthorized");
        await Task.Delay(TestCooldown + TimeSpan.FromMilliseconds(40));

        Assert.False(breaker.IsOpen("brevo")); // prova liberada
        breaker.RecordFailure("brevo", "erro 500 na prova");

        Assert.True(breaker.IsOpen("brevo")); // reaberto imediatamente
    }

    [Fact]
    public void Reset_ClosesOpenBreakerAndClearsWindow()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);
        breaker.RecordFailure("brevo", "401 unauthorized");
        Assert.True(breaker.IsOpen("brevo"));

        breaker.Reset("brevo");

        Assert.False(breaker.IsOpen("brevo"));
        // Janela limpa: uma falha normal isolada não reabre
        breaker.RecordFailure("brevo", "erro 500");
        Assert.False(breaker.IsOpen("brevo"));
    }

    [Fact]
    public void GetStates_ExposesOpenStateAndReason()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);
        breaker.RecordFailure("brevo", "401 unauthorized");
        breaker.RecordSuccess("sendgrid");

        var states = breaker.GetStates();

        Assert.True(states["brevo"].IsOpen);
        Assert.Contains("autenticação", states["brevo"].Reason);
        Assert.False(states["sendgrid"].IsOpen);
    }

    [Fact]
    public void ProvidersAreIndependent()
    {
        var breaker = new EmailProviderCircuitBreaker(TestCooldown);

        breaker.RecordFailure("brevo", "401 unauthorized");

        Assert.True(breaker.IsOpen("brevo"));
        Assert.False(breaker.IsOpen("sendgrid"));
    }
}

/// <summary>Classificador compartilhado — fonte única para os dois breakers.</summary>
public class EmailErrorClassifierTests
{
    [Theory]
    [InlineData("recipient hard bounce")]
    [InlineData("soft_bounce detected")]
    [InlineData("message bounced")]
    public void Bounce_IsNotCriticalAuthError(string error)
    {
        // Regressão P0-5: "bounce" saiu da lista de erros críticos.
        Assert.False(EmailErrorClassifier.IsCriticalAuthError(error));
    }

    [Theory]
    [InlineData("401 Unauthorized")]
    [InlineData("invalid api key")]
    [InlineData("authentication failed")]
    [InlineData("falha de autenticação")]
    public void AuthErrors_AreCritical(string error)
    {
        Assert.True(EmailErrorClassifier.IsCriticalAuthError(error));
    }

    [Theory]
    [InlineData("429 Too Many Requests")]
    [InlineData("rate limit exceeded")]
    public void PureRateLimit_IsIgnorable(string error)
    {
        Assert.True(EmailErrorClassifier.IsIgnorable(error));
    }

    [Fact]
    public void RateLimitWithAuthSignal_IsNotIgnorable()
    {
        Assert.False(EmailErrorClassifier.IsIgnorable("429 ... invalid api key"));
        Assert.True(EmailErrorClassifier.IsCriticalAuthError("429 ... invalid api key"));
    }
}
