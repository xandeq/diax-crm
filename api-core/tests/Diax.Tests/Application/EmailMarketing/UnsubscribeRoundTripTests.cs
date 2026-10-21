using Diax.Api.Controllers.V1;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Ida e volta real: o link que vai dentro do email, clicado, tem que descadastrar.
/// Regressão de 21/10: o builder gerava só o HMAC e o controller tentava decodificar
/// "unsub:uid:email" de dentro do token → todo clique dava 400.
/// </summary>
public class UnsubscribeRoundTripTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressions = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private EmailUnsubscribeController Controller() => new(
        _customers.Object, _suppressions.Object, _uow.Object,
        EmailTestDefaults.LinkBuilder(), NullLogger<EmailUnsubscribeController>.Instance);

    private static Dictionary<string, string> QueryOf(string url)
    {
        var q = QueryHelpers.ParseQuery(new Uri(url).Query);
        return q.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    }

    [Fact]
    public async Task LinkFromEmail_WhenClicked_SuppressesTheRecipient()
    {
        var userId = Guid.NewGuid();
        const string email = "lead+tag@example.com";
        var q = QueryOf(EmailTestDefaults.LinkBuilder().BuildUnsubscribeUrl(userId, email));

        var result = await Controller().Unsubscribe(q["token"], q["uid"], q["email"], CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        _suppressions.Verify(s => s.AddAsync(
            It.Is<EmailSuppression>(x => x.Email == email && x.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TamperedEmail_IsRejected_AndNothingIsWritten()
    {
        var userId = Guid.NewGuid();
        var q = QueryOf(EmailTestDefaults.LinkBuilder().BuildUnsubscribeUrl(userId, "a@example.com"));

        var result = await Controller().Unsubscribe(q["token"], q["uid"], "outro@example.com", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        _suppressions.Verify(s => s.AddAsync(It.IsAny<EmailSuppression>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "u", "e@x.com")]
    [InlineData("t", null, "e@x.com")]
    [InlineData("t", "u", null)]
    public async Task MissingParameter_IsBadRequest(string? token, string? uid, string? email)
    {
        var result = await Controller().Unsubscribe(token!, uid!, email!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
