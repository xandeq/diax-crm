using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Customers.Enums;
using FluentAssertions;

namespace Diax.UnitTests.Customers;

public class LeadSanitizationServiceTests
{
    private readonly LeadSanitizationService _sut;

    public LeadSanitizationServiceTests()
    {
        _sut = new LeadSanitizationService();
    }

    [Fact]
    public void SanitizeAndClassify_WithBrokenEncoding_ShouldFixEncodingAndTitleCase()
    {
        // Assemble
        var data = new RawLeadData("JoÃ£o Da Silva", "joao@gmail.com", "11999999999", null, "AÃ§ougue legal", "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.Name.Should().Be("João Da Silva");
        result.CompanyName.Should().Be("Açougue Legal");
    }

    [Theory]
    [InlineData("MVeis e Decorações", "Móveis e Decorações")]
    [InlineData("MaiS de Praia", "Maiôs de Praia")]
    [InlineData("AcessRios de VerO", "Acessórios de Verão")]
    [InlineData("BiquNis de Fita", "Biquínis de Fita")]
    [InlineData("EscritRio de Advocacia", "Escritório de Advocacia")]
    public void SanitizeAndClassify_WithMojibakeWords_ShouldDecodeWithoutDictionary(string input, string expected)
    {
        var data = new RawLeadData(input, "email@real.com", "12345", null, "", "");
        var result = _sut.SanitizeAndClassify(data);
        result.Name.ToLowerInvariant().Should().Be(expected.ToLowerInvariant());
    }

    [Theory]
    [InlineData("Loja de Biquínis Maiôs Saídas de Praia e Acessórios. Compre Agora e Arrase no Verão!", "Loja de Biquínis Maiôs Saídas de Praia e Acessórios")]
    [InlineData("Clinica Teste - Peça já o seu orçamento", "Clinica Teste")]
    [InlineData("Store | Mais vendido do Mercado LIVRE", "Store")]
    public void SanitizeAndClassify_WithSlogansAndPromos_ShouldTruncateSlogans(string input, string expected)
    {
        var data = new RawLeadData(input, "email@real.com", "123", "", "", "");
        var result = _sut.SanitizeAndClassify(data);
        result.Name.ToLowerInvariant().Should().Be(expected.ToLowerInvariant());
    }

    [Fact]
    public void SanitizeAndClassify_WithDuplicateWords_ShouldRemoveDuplicates()
    {
        var data = new RawLeadData("Loja Teste Maria Loja Teste", "mail", "12", "", "", "");
        var result = _sut.SanitizeAndClassify(data);
        result.Name.Should().Be("Loja Teste Maria");
    }

    [Theory]
    [InlineData("test@mailinator.com")]
    [InlineData("test@yopmail.com")]
    [InlineData("myemail@somesite.xyz")]
    [InlineData("hello@world.top")]
    public void SanitizeAndClassify_WithSuspiciousDomains_ShouldMarkAsSuspiciousAndInvalidate(string email)
    {
        // Assemble
        var data = new RawLeadData("John Doe", email, "1231231234", null, "Acme", "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.HasSuspiciousDomain.Should().BeTrue();
        result.IsEmailValid.Should().BeFalse();
        result.Email.Should().BeNull();
        result.IsEligibleForCampaigns.Should().BeFalse();
    }

    [Theory]
    [InlineData("test@gmail.com&source=linktree")]
    [InlineData("test@gmail.com?ref=web")]
    [InlineData("test@gmail.com\"")]
    public void SanitizeAndClassify_WithGarbageAppendedToEmail_ShouldTrimGarbage(string rawEmail)
    {
        // Assemble
        var data = new RawLeadData("John Doe", rawEmail, "1231231234", null, "Acme", "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.Email.Should().Be("test@gmail.com");
        result.IsEmailValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Linktree")]
    [InlineData("GuiaMais")]
    [InlineData("Erro 404")]
    public void SanitizeAndClassify_WithDirectoryOrGenericCompany_ShouldReject(string companyName)
    {
        // Assemble
        var data = new RawLeadData("Some Directory", "fake@gmail.com", "12345", null, companyName, "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.ShouldReject.Should().BeTrue();
        result.RejectionReason.Should().Contain("Directory");
    }

    [Fact]
    public void SanitizeAndClassify_WithNoEmailAndNoPhone_ShouldReject()
    {
        // Assemble
        var data = new RawLeadData("Ghost User", "", "", "", "Ghost Corp", "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.ShouldReject.Should().BeTrue();
        result.RejectionReason.Should().Contain("commercial viable contact");
    }

    [Fact]
    public void SanitizeAndClassify_WithHighQualityData_ShouldReturnHighQuality()
    {
        // Assemble
        var data = new RawLeadData("Bill Gates", "bill@microsoft.com", "1199999999", null, "Microsoft", "");

        // Act
        var result = _sut.SanitizeAndClassify(data);

        // Assert
        result.Quality.Should().Be(LeadQuality.High);
        result.EmailType.Should().Be(EmailType.PersonalDirect);
        result.IsEligibleForCampaigns.Should().BeTrue();
    }

    // ===== NOVOS TESTES: Domínios estrangeiros =====

    [Theory]
    [InlineData("gomeza@vithas.es")]          // Espanha
    [InlineData("info@forum-pet.de")]          // Alemanha
    [InlineData("contact@nesx.co")]            // Colômbia
    [InlineData("user@empresa.ar")]            // Argentina
    [InlineData("hello@shop.mx")]              // México
    [InlineData("test@clinic.fr")]             // França
    public void SanitizeAndClassify_WithForeignCountryDomain_ShouldInvalidateEmail(string email)
    {
        var data = new RawLeadData("Foreign Lead", email, "1234567890", null, "Foreign Corp", "");
        var result = _sut.SanitizeAndClassify(data);

        result.HasSuspiciousDomain.Should().BeTrue();
        result.IsEmailValid.Should().BeFalse();
        result.Email.Should().BeNull();
        result.IsEligibleForCampaigns.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@empresa.com.br")]   // Brasil composto
    [InlineData("user@empresa.com")]      // .com global
    [InlineData("user@empresa.org")]      // .org global
    [InlineData("user@empresa.net")]      // .net global
    [InlineData("user@empresa.io")]       // .io tech
    [InlineData("user@empresa.pt")]       // Portugal (mesmo idioma)
    public void SanitizeAndClassify_WithValidDomains_ShouldAcceptEmail(string email)
    {
        var data = new RawLeadData("Valid Lead", email, "1234567890", null, "Valid Corp", "");
        var result = _sut.SanitizeAndClassify(data);

        result.IsEmailValid.Should().BeTrue();
        result.Email.Should().NotBeNull();
    }

    // ===== NOVOS TESTES: Plataformas/Portais como nome =====

    [Theory]
    [InlineData("Jusbrasil")]
    [InlineData("Mercado Livre")]
    [InlineData("Doctoralia")]
    [InlineData("Reclame Aqui")]
    [InlineData("iFood")]
    public void SanitizeAndClassify_WithPlatformNames_ShouldReject(string name)
    {
        var data = new RawLeadData(name, "user@platform.com", "12345", null, name, "");
        var result = _sut.SanitizeAndClassify(data);

        result.ShouldReject.Should().BeTrue();
        result.RejectionReason.Should().Contain("Directory");
    }

    // ===== NOVOS TESTES: Frases de busca do Google Maps =====

    [Theory]
    [InlineData("Hospitais E Clínicas Em Viana")]
    [InlineData("Restaurantes Em São Paulo")]
    [InlineData("Clínicas E Consultórios Em Curitiba")]
    [InlineData("Farmácias Em Belo Horizonte")]
    public void SanitizeAndClassify_WithSearchPhrases_ShouldReject(string name)
    {
        var data = new RawLeadData(name, "contact@empresa.com.br", "1199999999", null, "", "");
        var result = _sut.SanitizeAndClassify(data);

        result.ShouldReject.Should().BeTrue();
        result.RejectionReason.Should().Contain("search phrase");
    }

    [Theory]
    [InlineData("Clínica São Lucas")]           // Nome real de clínica
    [InlineData("Hospital Santa Maria")]        // Nome real de hospital
    [InlineData("Restaurante Do João")]          // Nome real
    public void SanitizeAndClassify_WithRealBusinessNames_ShouldNotRejectAsSearchPhrase(string name)
    {
        var data = new RawLeadData(name, "contato@clinica.com.br", "1234567890", null, "", "");
        var result = _sut.SanitizeAndClassify(data);

        result.ShouldReject.Should().BeFalse();
    }

    // ===== NOVOS TESTES: U+FFFD e caracteres repetidos =====

    [Fact]
    public void SanitizeAndClassify_WithReplacementCharacter_ShouldStripAndFix()
    {
        // "Casa Escrit\uFFFDÓóóóóório M\uFFFDVeis" → strip FFFD → "Casa EscritÓóóóóório MVeis" → collapse → "Casa Escritório Móveis"
        var data = new RawLeadData("Casa Escrit\uFFFDÓóóóóório M\uFFFDVeis", "email@test.com", "123", null, "", "");
        var result = _sut.SanitizeAndClassify(data);

        result.Name.ToLowerInvariant().Should().Contain("escrit");
        result.Name.ToLowerInvariant().Should().Contain("veis");
        // Should NOT contain the replacement character
        result.Name.Should().NotContain("\uFFFD");
    }

    [Fact]
    public void SanitizeAndClassify_WithExcessiveRepeatedChars_ShouldCollapse()
    {
        var data = new RawLeadData("Testeeeeee Lojaaaaaa", "email@test.com", "123", null, "", "");
        var result = _sut.SanitizeAndClassify(data);

        // "Testeeeeee" should collapse to "Testee" (max 2 consecutive), "Lojaaaaaa" → "Lojaa"
        result.Name.Should().NotContain("eee");
        result.Name.Should().NotContain("aaa");
    }

    // ===== NOVOS TESTES: Mais mojibake PT-BR =====

    [Theory]
    [InlineData("Clnica Mdica", "Clínica Médica")]
    [InlineData("Farmcia Popular", "Farmácia Popular")]
    [InlineData("Comrcio de Imveis", "Comércio De Imóveis")]
    [InlineData("Acadmia de Sade", "Academia De Saúde")]
    [InlineData("Servios de Informtica", "Serviços De Informática")]
    public void SanitizeAndClassify_WithNewMojibakeWords_ShouldDecode(string input, string expected)
    {
        var data = new RawLeadData(input, "email@real.com", "12345", null, "", "");
        var result = _sut.SanitizeAndClassify(data);
        result.Name.ToLowerInvariant().Should().Be(expected.ToLowerInvariant());
    }
}
