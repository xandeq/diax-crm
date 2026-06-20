using Diax.Application.EmailMarketing;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

public class PilotCircuitBreakerTests
{
    [Fact]
    public void RecordFailure_WithRateLimit429_DoesNotOpenBreaker()
    {
        var breaker = new PilotCircuitBreaker();

        // 10 falhas de rate-limit seguidas não devem abrir o circuito
        for (var i = 0; i < 10; i++)
        {
            breaker.RecordFailure("Resend API error 429: {\"statusCode\":429,\"name\":\"rate_limit_exceeded\"}");
        }

        Assert.False(breaker.IsOpen);
        Assert.Equal(0.0, breaker.CurrentErrorRate); // 429 é transitório e não entra na janela de erro
    }

    [Theory]
    [InlineData("HTTP 429 Too Many Requests")]
    [InlineData("rate limit exceeded")]
    [InlineData("provider returned 429")]
    public void RecordFailure_WithVariousRateLimitMessages_AreIgnored(string msg)
    {
        var breaker = new PilotCircuitBreaker();

        breaker.RecordFailure(msg);
        breaker.RecordFailure(msg);
        breaker.RecordFailure(msg);

        Assert.False(breaker.IsOpen);
        Assert.Equal(0.0, breaker.CurrentErrorRate);
    }

    [Fact]
    public void RecordFailure_WithRealErrors_StillOpensBreakerAt30Percent()
    {
        var breaker = new PilotCircuitBreaker();

        // 7 sucessos + 3 falhas reais = 30% de erro na janela de 10 -> abre
        for (var i = 0; i < 7; i++) breaker.RecordSuccess();
        breaker.RecordFailure("SMTP timeout");
        breaker.RecordFailure("SMTP timeout");
        breaker.RecordFailure("SMTP timeout");

        Assert.True(breaker.IsOpen); // taxa de erro real atingiu o limite de 30%
    }

    [Fact]
    public void RateLimitFailures_DoNotDiluteRealErrorRate()
    {
        var breaker = new PilotCircuitBreaker();

        // Falhas reais entremeadas de 429: os 429 são ignorados, então a janela
        // efetiva são só os erros reais e o circuito abre.
        breaker.RecordSuccess();
        breaker.RecordFailure("429 too many requests"); // ignorado
        breaker.RecordFailure("connection refused");
        breaker.RecordFailure("429 too many requests"); // ignorado
        breaker.RecordFailure("connection refused");

        // Janela real: [success, fail, fail] => 66% -> aberto
        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void Reset_ClosesOpenBreakerAndClearsWindow()
    {
        var breaker = new PilotCircuitBreaker();
        breaker.Open("forçado para teste");
        Assert.True(breaker.IsOpen);

        breaker.Reset();

        Assert.False(breaker.IsOpen);
        Assert.Null(breaker.Reason);
        Assert.Equal(0.0, breaker.CurrentErrorRate);
        Assert.Equal(0, breaker.WebhookFailureCount);
    }

    [Fact]
    public void CriticalAuthError_OpensImmediately()
    {
        var breaker = new PilotCircuitBreaker();

        breaker.RecordFailure("401 unauthorized: invalid api key");

        Assert.True(breaker.IsOpen); // erros de autenticação são críticos e abrem na hora
    }

    [Theory]
    [InlineData("429 Too Many Requests - invalid api key")]
    [InlineData("rate limit: unauthorized")]
    [InlineData("429: authentication failed")]
    public void CriticalError_TakesPrecedenceOverRateLimit(string msg)
    {
        var breaker = new PilotCircuitBreaker();

        // Mensagem com token de rate-limit E de erro crítico: o crítico vence e abre.
        breaker.RecordFailure(msg);

        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void TwoRealFailures_BelowWindowThreshold_DoNotOpen()
    {
        var breaker = new PilotCircuitBreaker();

        // Menos de 3 envios na janela: nunca abre, mesmo a 100% de erro.
        breaker.RecordFailure("connection refused");
        breaker.RecordFailure("connection refused");

        Assert.False(breaker.IsOpen);
    }

    [Fact]
    public void RateLimit_DoesNotAddToWindow_VerifiedByErrorRate()
    {
        var breaker = new PilotCircuitBreaker();

        breaker.RecordSuccess();
        breaker.RecordSuccess();
        breaker.RecordFailure("429 too many requests"); // ignorado, janela = [true, true]

        Assert.Equal(0.0, breaker.CurrentErrorRate);
        Assert.False(breaker.IsOpen);
    }

    [Fact]
    public void RecordFailure_WithNull_DoesNotThrow()
    {
        var breaker = new PilotCircuitBreaker();

        var ex = Record.Exception(() => breaker.RecordFailure(null!));

        Assert.Null(ex);
        Assert.False(breaker.IsOpen);
    }
}
