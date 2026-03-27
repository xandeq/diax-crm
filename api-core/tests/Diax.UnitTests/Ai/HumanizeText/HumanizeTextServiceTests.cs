using Diax.Application.AI;
using Diax.Application.Ai.HumanizeText;
using Diax.Application.Ai.Services;
using Diax.Application.PromptGenerator;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Diax.Shared.Ai;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Diax.UnitTests.Ai.HumanizeText;

public class HumanizeTextServiceTests
{
    private readonly Mock<IAiTextTransformClient> _mockClient;
    private readonly Mock<IAiModelValidator> _mockValidator;
    private readonly Mock<ILogger<HumanizeTextService>> _mockLogger;
    private readonly Mock<IAiUsageLogRepository> _mockUsageLogRepo;
    private readonly Mock<IAiProviderRepository> _mockProviderRepo;
    private readonly Mock<IAiModelRepository> _mockModelRepo;
    private readonly Mock<ITokenEstimator> _mockTokenEstimator;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly PromptGeneratorSettings _settings;
    private readonly HumanizeTextService _sut;

    public HumanizeTextServiceTests()
    {
        _mockClient = new Mock<IAiTextTransformClient>();
        _mockClient.Setup(c => c.ProviderName).Returns("openai");

        _mockValidator = new Mock<IAiModelValidator>();
        _mockLogger = new Mock<ILogger<HumanizeTextService>>();
        _mockUsageLogRepo = new Mock<IAiUsageLogRepository>();
        _mockProviderRepo = new Mock<IAiProviderRepository>();
        _mockModelRepo = new Mock<IAiModelRepository>();
        _mockTokenEstimator = new Mock<ITokenEstimator>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _settings = new PromptGeneratorSettings
        {
            OpenAI = new ProviderConfig { ApiKey = "test-key", Model = "gpt-4" }
        };

        _sut = new HumanizeTextService(
            new[] { _mockClient.Object },
            _settings,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockUsageLogRepo.Object,
            _mockProviderRepo.Object,
            _mockModelRepo.Object,
            _mockTokenEstimator.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HumanizeAsync_ValidRequest_ReturnsTransformedText()
    {
        // Arrange
        var request = new HumanizeTextRequestDto("openai", null, "humanize_text_light", "Test input");
        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClient.Setup(c => c.TransformAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AiClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Humanized output");

        // Act
        var result = await _sut.HumanizeAsync(request);

        // Assert
        result.OutputText.Should().Be("Humanized output");
        result.ProviderUsed.Should().Be("openai");
        result.ToneUsed.Should().Be("humanize_text_light");
        result.RequestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HumanizeAsync_InvalidProvider_ThrowsArgumentException()
    {
        // Arrange
        var request = new HumanizeTextRequestDto("invalid_provider", null, "humanize_text_light", "Test input");
        _mockValidator.Setup(v => v.IsValidProviderAsync("invalid_provider", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockValidator.Setup(v => v.GetActiveProviderKeysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "openai", "gemini" });

        // Act
        var act = () => _sut.HumanizeAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalid_provider*");
    }

    [Fact]
    public async Task HumanizeAsync_NoApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settingsNoKey = new PromptGeneratorSettings
        {
            OpenAI = new ProviderConfig { ApiKey = null, Model = "gpt-4" }
        };
        var sut = new HumanizeTextService(
            new[] { _mockClient.Object },
            settingsNoKey,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockUsageLogRepo.Object,
            _mockProviderRepo.Object,
            _mockModelRepo.Object,
            _mockTokenEstimator.Object,
            _mockCurrentUserService.Object);

        var request = new HumanizeTextRequestDto("openai", null, "humanize_text_light", "Test input");
        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => sut.HumanizeAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API Key*");
    }

    [Fact]
    public async Task HumanizeAsync_NoMatchingClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new HumanizeTextRequestDto("gemini", null, "humanize_text_light", "Test input");
        var settingsGemini = new PromptGeneratorSettings
        {
            Gemini = new ProviderConfig { ApiKey = "test-key", Model = "gemini-pro" }
        };
        var sut = new HumanizeTextService(
            new[] { _mockClient.Object }, // only has "openai" client
            settingsGemini,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockUsageLogRepo.Object,
            _mockProviderRepo.Object,
            _mockModelRepo.Object,
            _mockTokenEstimator.Object,
            _mockCurrentUserService.Object);

        _mockValidator.Setup(v => v.IsValidProviderAsync("gemini", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => sut.HumanizeAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Client de IA não encontrado*");
    }

    [Fact]
    public async Task HumanizeAsync_WithAuthenticatedUser_UsesCurrentUserServiceForLogging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);

        var provider = new AiProvider("openai", "OpenAI", false);
        var model = new AiModel(provider.Id, "gpt-4", "GPT-4", false);

        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClient.Setup(c => c.TransformAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AiClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Output");
        _mockProviderRepo.Setup(r => r.GetByKeyAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _mockModelRepo.Setup(r => r.GetByProviderIdAsync(provider.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { model });
        _mockTokenEstimator.Setup(e => e.EstimateTokens(It.IsAny<string>())).Returns(100);

        var request = new HumanizeTextRequestDto("openai", null, "humanize_text_light", "Test input");

        // Act
        var result = await _sut.HumanizeAsync(request);

        // Assert
        result.OutputText.Should().Be("Output");
        _mockCurrentUserService.Verify(s => s.UserId, Times.AtLeastOnce());
    }

    [Fact]
    public async Task HumanizeAsync_WithNullUserId_DoesNotBreak()
    {
        // Arrange - anonymous user
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClient.Setup(c => c.TransformAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AiClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Output");

        var request = new HumanizeTextRequestDto("openai", null, "humanize_text_light", "Test input");

        // Act
        var result = await _sut.HumanizeAsync(request);

        // Assert
        result.OutputText.Should().Be("Output");
    }

    [Fact]
    public async Task HumanizeAsync_UsageLoggingFailure_DoesNotBreakMainFlow()
    {
        // Arrange - provider repo throws exception during logging
        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClient.Setup(c => c.TransformAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AiClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Output");
        _mockProviderRepo.Setup(r => r.GetByKeyAsync("openai", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection failed"));

        var request = new HumanizeTextRequestDto("openai", null, "humanize_text_light", "Test input");

        // Act - should not throw even though logging fails
        var result = await _sut.HumanizeAsync(request);

        // Assert
        result.OutputText.Should().Be("Output");
    }

    [Fact]
    public async Task HumanizeAsync_WithCustomModel_UsesRequestModel()
    {
        // Arrange
        var request = new HumanizeTextRequestDto("openai", "gpt-4-turbo", "humanize_text_professional", "Test input");
        _mockValidator.Setup(v => v.IsValidProviderAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockClient.Setup(c => c.TransformAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.Is<AiClientOptions>(o => o.Model == "gpt-4-turbo"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Professional output");

        // Act
        var result = await _sut.HumanizeAsync(request);

        // Assert
        result.OutputText.Should().Be("Professional output");
        _mockClient.Verify(c => c.TransformAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.Is<AiClientOptions>(o => o.Model == "gpt-4-turbo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
