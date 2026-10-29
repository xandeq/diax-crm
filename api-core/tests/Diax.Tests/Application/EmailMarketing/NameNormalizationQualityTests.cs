using Diax.Application.EmailMarketing.Pro;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Casos tirados da prévia real de produção (GET /leads/normalization/preview): lixo de scraping
/// ("Cliente Instagram Logo", títulos de página) saía com score 95. Lixo é removido ou fica abaixo de 80.
/// </summary>
public class NameNormalizationQualityTests
{
    private readonly NameNormalizationService _sut = new();

    [Theory]
    [InlineData("Rogério Pontes Cliente Instagram Logo", null, "Rogério Pontes")]
    [InlineData("Diana Cliente", "apmdiana@gmail.com", "Diana")]
    [InlineData("Lu Lima Cliente", "tvlulima@gmail.com", "Lu Lima")]
    [InlineData("Mark Rastreadores CLIENTE", null, "Mark Rastreadores")]
    [InlineData("Elaine Vitalcor Cliente", "elainesouza@zammi.com.br", "Elaine Vitalcor")]
    [InlineData("Dom Restaurante – Gastronomia Brasileira", "trabalheconosco@domrestaurante.com", "Dom")]
    [InlineData("Clínica Kairós – Vitória, Es", "clinicakairos.mev@gmail.com", "Kairós")]
    public void ScrapingNoise_IsRemovedFromTheSuggestedName(string raw, string? email, string expectedName)
    {
        var result = _sut.Normalize(raw, email);

        Assert.Equal(expectedName, result.NormalizedName);
    }

    [Theory]
    [InlineData("Logo")]
    [InlineData("Instagram")]
    [InlineData("WhatsApp")]
    [InlineData("Home")]
    [InlineData("Página inicial")]
    [InlineData("Cliente Instagram Logo")]
    public void OnlyNoise_ProducesNoSuggestion(string raw)
    {
        var result = _sut.Normalize(raw);

        Assert.Equal(0, result.Score);
        Assert.Equal("", result.NormalizedName);
    }

    [Theory]
    [InlineData("Pet Shop Abertos 24 Horas Em Fundão, Es")]
    [InlineData("Dentistas, Clínicas Odontológicas Em Vila Velha")]
    [InlineData("Prisma Consultoria Que Gera Resultados")]
    [InlineData("Encontre Um Contador")]
    [InlineData("Loja De Biquínis, Maiôs, Saídas De Praia E Acessórios")]
    [InlineData("Casa Escritóóório Móveis Sob Medida Premium")]
    public void PhrasesAndLongTitles_NeverReachHighConfidence(string raw)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.Score < 80, $"'{raw}' → '{result.NormalizedName}' score {result.Score}");
    }

    [Theory]
    [InlineData("Maria da Silva", "Maria Da Silva")]
    [InlineData("João Pedro", "João Pedro")]
    [InlineData("Ana Paula Souza Lima", "Ana Paula Souza Lima")]
    [InlineData("Cristina Milanez", "Cristina Milanez")]
    public void RealPersonNames_StayHighConfidence(string raw, string expected)
    {
        var result = _sut.Normalize(raw);

        Assert.Equal(expected, result.NormalizedName);
        Assert.True(result.Score >= 80);
    }

    [Fact]
    public void NormalizeName_FirstName_SkipsNoiseAndTitleTail()
    {
        Assert.Equal("Rogério", _sut.NormalizeName("Cliente Instagram Rogério Pontes"));
        Assert.Equal("Kairós", _sut.NormalizeName("Clínica Kairós – Vitória, Es"));
    }
}
