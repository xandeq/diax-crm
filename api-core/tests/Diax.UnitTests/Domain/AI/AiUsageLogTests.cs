using Diax.Domain.AI;
using FluentAssertions;

namespace Diax.UnitTests.Domain.AI;

public class AiUsageLogTests
{
    [Fact]
    public void Constructor_ValidParameters_SetsAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        // Act
        var log = new AiUsageLog(
            userId: userId,
            providerId: providerId,
            modelId: modelId,
            tokensInput: 500,
            tokensOutput: 200,
            costEstimated: 0.0035m,
            requestType: "humanize_text");

        // Assert
        log.UserId.Should().Be(userId);
        log.ProviderId.Should().Be(providerId);
        log.ModelId.Should().Be(modelId);
        log.TokensInput.Should().Be(500);
        log.TokensOutput.Should().Be(200);
        log.CostEstimated.Should().Be(0.0035m);
        log.RequestType.Should().Be("humanize_text");
    }

    [Fact]
    public void Constructor_CalculatesTotalTokens()
    {
        // Arrange & Act
        var log = new AiUsageLog(
            userId: null,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            tokensInput: 300,
            tokensOutput: 150,
            costEstimated: 0.001m,
            requestType: "humanize_text");

        // Assert
        log.TotalTokens.Should().Be(450); // 300 + 150
    }

    [Fact]
    public void Constructor_NullUserId_IsAllowed()
    {
        // Arrange & Act
        var log = new AiUsageLog(
            userId: null,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            tokensInput: 100,
            tokensOutput: 50,
            costEstimated: 0.0001m,
            requestType: "humanize_text");

        // Assert
        log.UserId.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var log = new AiUsageLog(
            userId: Guid.NewGuid(),
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            tokensInput: 100,
            tokensOutput: 50,
            costEstimated: 0.001m,
            requestType: "humanize_text");

        var after = DateTime.UtcNow;

        // Assert
        log.CreatedAt.Should().BeOnOrAfter(before);
        log.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Arrange & Act
        var log1 = new AiUsageLog(null, Guid.NewGuid(), Guid.NewGuid(), 100, 50, 0.001m, "test");
        var log2 = new AiUsageLog(null, Guid.NewGuid(), Guid.NewGuid(), 100, 50, 0.001m, "test");

        // Assert
        log1.Id.Should().NotBe(Guid.Empty);
        log2.Id.Should().NotBe(Guid.Empty);
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Constructor_ZeroTokens_HandlesCorrectly()
    {
        // Act
        var log = new AiUsageLog(
            userId: null,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            tokensInput: 0,
            tokensOutput: 0,
            costEstimated: 0m,
            requestType: "humanize_text");

        // Assert
        log.TotalTokens.Should().Be(0);
        log.CostEstimated.Should().Be(0m);
    }

    [Fact]
    public void Constructor_HighPrecisionCost_PreservesValue()
    {
        // Micro-costs need decimal(18,6) precision
        var microCost = 0.000123m;

        // Act
        var log = new AiUsageLog(
            userId: null,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            tokensInput: 10,
            tokensOutput: 5,
            costEstimated: microCost,
            requestType: "humanize_text");

        // Assert
        log.CostEstimated.Should().Be(0.000123m);
    }
}
