using Diax.Application.Ai.HumanizeText;
using FluentAssertions;

namespace Diax.UnitTests.Ai.HumanizeText;

public class HumanizeTextPromptBuilderTests
{
    [Theory]
    [InlineData("humanize_text_light")]
    [InlineData("humanize_text_professional")]
    [InlineData("humanize_text_marketing")]
    [InlineData("humanize_text_documentation")]
    public void BuildSystemPrompt_KnownTones_ReturnsNonEmptyPrompt(string tone)
    {
        // Act
        var result = HumanizeTextPromptBuilder.BuildSystemPrompt(tone);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BuildSystemPrompt_UnknownTone_ReturnsDefaultPrompt()
    {
        // Act
        var result = HumanizeTextPromptBuilder.BuildSystemPrompt("unknown_tone");

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BuildSystemPrompt_WithLanguage_IncludesLanguage()
    {
        // Act
        var result = HumanizeTextPromptBuilder.BuildSystemPrompt("humanize_text_light", "en-US");

        // Assert
        result.Should().Contain("en-US");
    }

    [Fact]
    public void BuildSystemPrompt_DefaultLanguage_UsesPtBR()
    {
        // Act
        var result = HumanizeTextPromptBuilder.BuildSystemPrompt("humanize_text_light");

        // Assert
        result.Should().Contain("pt-BR");
    }

    [Fact]
    public void BuildUserPrompt_WithText_IncludesInputText()
    {
        // Arrange
        var inputText = "Este é um texto de teste para humanização.";

        // Act
        var result = HumanizeTextPromptBuilder.BuildUserPrompt(inputText);

        // Assert
        result.Should().Contain(inputText);
    }
}
