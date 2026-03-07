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
}
