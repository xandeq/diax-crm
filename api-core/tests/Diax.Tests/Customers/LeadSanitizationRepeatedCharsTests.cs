using Diax.Application.Customers.Services;
using Xunit;

namespace Diax.Tests.Customers;

/// <summary>
/// CollapseRepeatedChars reduz 3+ letras repetidas a 2 (lixo de scraping tipo "Saaaaúde"),
/// mas também transformava "www." em "ww." nas notas importadas do Extrator
/// ("Website: https://ww.financialnet.com.br", visto em produção 08/10/2026).
/// </summary>
public class LeadSanitizationRepeatedCharsTests
{
    private readonly LeadSanitizationService _sut = new();

    private static RawLeadData Raw(string? notes = null, string name = "Contabilidade Teste") =>
        new(name, "contato@financialnet.com.br", null, null, name, notes);

    [Theory]
    [InlineData("Website: https://www.financialnet.com.br Status no Extrator: novo ID Extrator: 107356")]
    [InlineData("Website: http://www.machadodepaulaadv.com.br/")]
    [InlineData("site www.exemplo.com.br e e-mail aaa@www.exemplo.com.br")]
    public void Notes_KeepUrlsAndEmailsIntact(string notes)
    {
        var result = _sut.SanitizeAndClassify(Raw(notes));

        Assert.Equal(notes, result.Notes);
    }

    [Fact]
    public void Notes_StillCollapseRepeatedLettersInPlainWords()
    {
        var result = _sut.SanitizeAndClassify(Raw("Clinica de Saaaaude ok"));

        Assert.Equal("Clinica de Saaude ok", result.Notes);
    }
}
