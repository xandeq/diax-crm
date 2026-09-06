using System.Linq;
using Diax.Application.Customers.WebsiteClassification;
using Diax.Domain.Customers.Enums;
using Xunit;

namespace Diax.Tests.Customers;

public class WebsiteClassifierTests
{
    [Theory]
    [InlineData(null, WebsiteKind.Unknown)]
    [InlineData("", WebsiteKind.Unknown)]
    [InlineData("   ", WebsiteKind.Unknown)]
    [InlineData("-", WebsiteKind.Unknown)]
    [InlineData("n/a", WebsiteKind.Unknown)]
    [InlineData("null", WebsiteKind.Unknown)]
    [InlineData("none", WebsiteKind.Unknown)]
    [InlineData("http://", WebsiteKind.Unknown)]
    [InlineData(":::", WebsiteKind.Unknown)]
    [InlineData("localhost", WebsiteKind.Unknown)]
    [InlineData("https://www.econodata.com.br/empresa/123", WebsiteKind.Directory)]
    [InlineData("facebook.com/minhaclinica", WebsiteKind.Directory)]
    [InlineData("https://linktr.ee/fulano", WebsiteKind.Directory)]
    [InlineData("https://instagram.com/loja", WebsiteKind.Directory)]
    [InlineData("HTTPS://WWW.CLINIGUIA.COM.BR/x", WebsiteKind.Directory)]
    [InlineData("https://clinicaodontovix.com.br", WebsiteKind.OwnSite)]
    [InlineData("www.padariadobairro.com.br", WebsiteKind.OwnSite)]
    [InlineData("https://minhaloja.nuvemshop.com.br", WebsiteKind.Directory)]
    public void Classify_MatchesPythonTruthTable(string? url, WebsiteKind expected)
        => Assert.Equal(expected, WebsiteClassifier.Classify(url));

    [Fact]
    public void DirectoryHosts_MatchesPythonCanonicalList()
    {
        // DIRECTORY_HOSTS de docs/email-marketing/site_check.py tem 41 entradas, sem duplicatas.
        Assert.Equal(41, WebsiteClassifier.DirectoryHostList.Count);
        Assert.Equal(41, WebsiteClassifier.DirectoryHostList.Distinct().Count());
    }
}
