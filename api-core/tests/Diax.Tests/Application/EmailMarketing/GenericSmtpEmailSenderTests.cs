using System.Threading;
using System.Threading.Tasks;
using Diax.Application.EmailMarketing;
using Diax.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

public class GenericSmtpEmailSenderTests
{
    private static EmailSendMessage ValidMessage() => new()
    {
        RecipientName = "Teste",
        RecipientEmail = "teste@example.com",
        Subject = "Assunto",
        HtmlBody = "<p>Corpo</p>"
    };

    private static GenericSmtpEmailSender BuildSender(SmtpProviderSettings settings) =>
        new(settings, NullLogger<GenericSmtpEmailSender>.Instance);

    [Fact]
    public async Task SendAsync_WithEmptyHost_ReturnsFail()
    {
        var settings = new SmtpProviderSettings
        {
            Host = "",
            Username = "user@example.com",
            Password = "senha123",
            DefaultFromEmail = "from@example.com"
        };

        var sender = BuildSender(settings);
        var result = await sender.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("incompleta", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_WithEmptyUsername_ReturnsFail()
    {
        var settings = new SmtpProviderSettings
        {
            Host = "smtp.example.com",
            Username = "",
            Password = "senha123",
            DefaultFromEmail = "from@example.com"
        };

        var sender = BuildSender(settings);
        var result = await sender.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("incompleta", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_WithEmptyPassword_ReturnsFail()
    {
        var settings = new SmtpProviderSettings
        {
            Host = "smtp.example.com",
            Username = "user@example.com",
            Password = "",
            DefaultFromEmail = "from@example.com"
        };

        var sender = BuildSender(settings);
        var result = await sender.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("incompleta", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_WithEmptyFromEmail_ReturnsFail()
    {
        var settings = new SmtpProviderSettings
        {
            Host = "smtp.example.com",
            Username = "user@example.com",
            Password = "senha123",
            DefaultFromEmail = ""
        };

        var sender = BuildSender(settings);
        var result = await sender.SendAsync(ValidMessage(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("incompleta", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }
}
