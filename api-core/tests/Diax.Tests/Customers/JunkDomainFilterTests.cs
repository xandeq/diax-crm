using Diax.Application.Customers.Services;
using Xunit;

namespace Diax.Tests.Customers;

public class JunkDomainFilterTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("semponto", true)]
    [InlineData("localhost", true)]
    [InlineData("google_maps", true)]
    [InlineData("instagram.local", true)]
    [InlineData("algo.localhost", true)]
    [InlineData("x.invalid", true)]
    [InlineData("x.test", true)]
    [InlineData("x.example", true)]
    [InlineData("x.internal", true)]
    [InlineData("x.lan", true)]
    [InlineData("example.com", true)]
    [InlineData("wixpress.com", true)]
    [InlineData("sentry-next.wixpress.com", true)]
    [InlineData("sentry.io", true)]
    [InlineData("gmail.con", true)]
    [InlineData("google.com", true)]
    [InlineData("wordpress.com", true)]
    [InlineData("wix.com", true)]
    [InlineData("squarespace.com", true)]
    [InlineData("godaddy.com", true)]
    [InlineData("hostgator.com.br", true)]
    [InlineData("mailinator.com", true)]
    [InlineData("noemail.com.br", true)]
    [InlineData("sememail.org", true)]
    [InlineData("nomail.net", true)]
    [InlineData("placeholder.io", true)]
    [InlineData("WIXPRESS.COM", true)]
    [InlineData("clinicaodontovix.com.br", false)]
    [InlineData("gmail.com", false)]
    [InlineData("padariadobairro.com.br", false)]
    public void IsJunk_MatchesPythonTruthTable(string? domain, bool expected)
        => Assert.Equal(expected, JunkDomainFilter.IsJunk(domain));

    [Fact]
    public void IsJunkEmail_ExtractsDomainFromEmail_Junk()
        => Assert.True(JunkDomainFilter.IsJunkEmail("contato@wixpress.com"));

    [Fact]
    public void IsJunkEmail_ExtractsDomainFromEmail_NotJunk()
        => Assert.False(JunkDomainFilter.IsJunkEmail("contato@clinicaodontovix.com.br"));

    [Fact]
    public void IsJunkEmail_MalformedEmail_ReturnsFalse()
        => Assert.False(JunkDomainFilter.IsJunkEmail("semarroba"));
}
