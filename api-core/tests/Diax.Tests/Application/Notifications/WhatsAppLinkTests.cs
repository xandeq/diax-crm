using Diax.Application.Notifications;

namespace Diax.Tests.Application.Notifications;

public class WhatsAppLinkTests
{
    [Theory]
    [InlineData("(27) 99924-3877", "https://wa.me/5527999243877")]
    [InlineData("27999243877", "https://wa.me/5527999243877")]
    [InlineData("+55 27 99924-3877", "https://wa.me/5527999243877")]
    [InlineData("5527999243877", "https://wa.me/5527999243877")]
    [InlineData("(21) 2737-7721", "https://wa.me/552127377721")]  // fixo 10 dígitos
    [InlineData("021 2737-7721", "https://wa.me/552127377721")]   // zero à esquerda
    public void From_NormalizesBrazilianPhones(string input, string expected)
    {
        Assert.Equal(expected, WhatsAppLink.From(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0800-0022")]        // curto demais após limpeza
    [InlineData("12345")]
    [InlineData("418868707564831")]  // longo demais (lixo de scraping)
    [InlineData("sem numero")]
    public void From_ReturnsNull_ForInvalid(string? input)
    {
        Assert.Null(WhatsAppLink.From(input));
    }

    [Fact]
    public void AsHtml_WrapsInAnchor_WhenValid()
    {
        var html = WhatsAppLink.AsHtml("(27) 99924-3877");
        Assert.Contains("href=\"https://wa.me/5527999243877\"", html);
        Assert.Contains("(27) 99924-3877", html);
    }

    [Fact]
    public void AsHtml_EscapesPlainText_WhenInvalid()
    {
        Assert.Equal("0800 &amp; cia", WhatsAppLink.AsHtml("0800 & cia"));
    }
}
