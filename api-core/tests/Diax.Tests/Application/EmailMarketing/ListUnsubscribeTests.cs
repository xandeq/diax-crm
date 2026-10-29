using System.Net;
using Diax.Api.Controllers.V1;
using Diax.Application.EmailMarketing;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

public class ListUnsubscribeHeadersTests
{
    [Fact]
    public void UrlHttps_GeraOsDoisCabecalhosDaRfc8058()
    {
        var h = ListUnsubscribeHeaders.Build("https://api.test.local/unsubscribe?token=abc&uid=1&email=a%40b.com");

        Assert.NotNull(h);
        Assert.Equal("<https://api.test.local/unsubscribe?token=abc&uid=1&email=a%40b.com>", h!["List-Unsubscribe"]);
        Assert.Equal("List-Unsubscribe=One-Click", h["List-Unsubscribe-Post"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://inseguro.test/unsubscribe")]
    [InlineData("/unsubscribe?token=x")]
    [InlineData("https://x.test/u>\r\nBcc: alguem@x.test")]
    public void UrlInvalidaOuInsegura_NaoGeraCabecalho(string? url)
        => Assert.Null(ListUnsubscribeHeaders.Build(url));
}

public class UnsubscribeOneClickControllerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressions = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IUnsubscribeLinkBuilder _links = EmailTestDefaults.LinkBuilder();
    private readonly Guid _uid = Guid.NewGuid();
    private const string Email = "dono@padaria.test";

    private EmailUnsubscribeController Ctl() => new(
        _customers.Object, _suppressions.Object, _uow.Object, _links,
        NullLogger<EmailUnsubscribeController>.Instance);

    private string Token() => UnsubscribeLinkBuilder.ComputeToken(EmailTestDefaults.SigningKey, _uid.ToString(), Email);

    [Fact]
    public async Task Post_OneClickComTokenValido_DescadastraERetorna200()
    {
        var customer = new Customer("Padaria", Email);
        _customers.Setup(c => c.GetByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var r = await Ctl().UnsubscribeOneClick(Token(), _uid.ToString(), Email, "One-Click", default);

        Assert.IsType<OkResult>(r);
        Assert.True(customer.EmailOptOut);
        _suppressions.Verify(s => s.AddAsync(It.IsAny<EmailSuppression>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Post_TokenInvalido_Retorna401ENaoAltera()
    {
        var customer = new Customer("Padaria", Email);
        _customers.Setup(c => c.GetByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var r = await Ctl().UnsubscribeOneClick("token-falso", _uid.ToString(), Email, "One-Click", default);

        Assert.IsType<UnauthorizedResult>(r);
        Assert.False(customer.EmailOptOut);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Post_SemCorpoOneClick_Retorna400()
    {
        var r = await Ctl().UnsubscribeOneClick(Token(), _uid.ToString(), Email, null, default);
        Assert.IsType<BadRequestObjectResult>(r);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class MailerSendListUnsubscribeTests
{
    [Fact]
    public async Task MailerSend_NaoEnviaHeaders_PlanoNaoProfessionalRejeitaOCampo()
    {
        string? body = null;
        var handler = new DelegatingStub(async (req, ct) =>
        {
            body = await req.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("") };
        });
        var sender = new MailerSendEmailSender(
            new HttpClient(handler),
            Options.Create(new MailerSendSettings { ApiToken = "t", FromEmail = "a@b.test" }),
            NullLogger<MailerSendEmailSender>.Instance);

        var r = await sender.SendAsync(new EmailSendMessage
        {
            RecipientEmail = "x@y.test",
            Subject = "Oi",
            HtmlBody = "<p>oi</p>",
            Headers = ListUnsubscribeHeaders.Build("https://api.test.local/unsubscribe?token=t&uid=u&email=e"),
        });

        Assert.True(r.Success);
        Assert.NotNull(body);
        Assert.DoesNotContain("headers", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("List-Unsubscribe", body);
    }

    private sealed class DelegatingStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> f) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => f(request, ct);
    }
}

public class BrevoListUnsubscribeTests
{
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"messageId\":\"<x@y>\"}")
            };
        }
    }

    [Fact]
    public async Task Brevo_EnviaHeadersNoPayload()
    {
        var handler = new CaptureHandler();
        var sender = new BrevoEmailSender(
            new HttpClient(handler),
            Options.Create(new BrevoSettings { ApiKey = "k", FromEmail = "a@b.test" }),
            NullLogger<BrevoEmailSender>.Instance);

        var r = await sender.SendAsync(new EmailSendMessage
        {
            RecipientEmail = "x@y.test",
            Subject = "Oi",
            HtmlBody = "<p>oi</p>",
            Headers = ListUnsubscribeHeaders.Build("https://api.test.local/unsubscribe?token=t&uid=u&email=e"),
        });

        Assert.True(r.Success);
        Assert.Contains("\"headers\"", handler.Body);
        Assert.Contains("List-Unsubscribe-Post", handler.Body);
        Assert.Contains("List-Unsubscribe=One-Click", handler.Body);
    }
}
