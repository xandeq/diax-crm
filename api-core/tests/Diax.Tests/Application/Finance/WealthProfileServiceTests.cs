using Diax.Application.Finance.Patrimonio;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

public class WealthProfileServiceTests
{
    private readonly Mock<IWealthProfileRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public WealthProfileServiceTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1);
    }

    private WealthProfileService Build() => new(
        _repo.Object,
        _unitOfWork.Object,
        NullLogger<WealthProfileService>.Instance);

    // ── GetOrCreate semeia os defaults (1M / 5 anos / builder_all) ────

    [Fact]
    public async Task GetOrCreateAsync_NoProfile_SeedsDefaults()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((WealthProfile?)null);

        var result = await Build().GetOrCreateAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("builder_all", result.Value.RiskProfile);
        Assert.Equal(1_000_000m, result.Value.GoalAmount);
        Assert.Equal(5, result.Value.GoalYears);

        // Alocação-alvo default (ease-ranked) soma 100%
        Assert.Equal(100m, result.Value.TargetAllocation.Values.Sum());
        Assert.Equal(25m, result.Value.TargetAllocation[nameof(AssetClass.RendaFixa)]);
        Assert.Equal(20m, result.Value.TargetAllocation[nameof(AssetClass.Acao)]);

        _repo.Verify(r => r.AddAsync(It.Is<WealthProfile>(p => p.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingProfile_ReturnsWithoutCreating()
    {
        var userId = Guid.NewGuid();
        var existing = WealthProfile.Create(userId, "custom_profile", 500_000m, 10);
        _repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existing);

        var result = await Build().GetOrCreateAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("custom_profile", result.Value.RiskProfile);
        Assert.Equal(500_000m, result.Value.GoalAmount);
        Assert.Equal(10, result.Value.GoalYears);

        _repo.Verify(r => r.AddAsync(It.IsAny<WealthProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
