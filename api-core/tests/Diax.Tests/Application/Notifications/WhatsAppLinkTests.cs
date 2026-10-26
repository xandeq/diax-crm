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

    [Fact]
    public void WithText_EncodesMessage_WhenValid()
    {
        var url = WhatsAppLink.WithText("(27) 99924-3877", "Dúvida sobre \"Site & SEO\"?");
        Assert.Equal("https://wa.me/5527999243877?text=D%C3%BAvida%20sobre%20%22Site%20%26%20SEO%22%3F", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0800 & cia")]
    public void WithText_Null_WhenPhoneMissingOrInvalid(string? phone)
    {
        Assert.Null(WhatsAppLink.WithText(phone, "oi"));
    }
}
