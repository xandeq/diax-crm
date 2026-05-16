using Diax.Application.Finance.Planner;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Finance;

public class FinancialGoalServiceTests
{
    private readonly Mock<IFinancialGoalRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FinancialGoalService Build() => new(
        _repo.Object,
        _uow.Object,
        NullLogger<FinancialGoalService>.Instance);

    private static FinancialGoal NewGoal(Guid userId, decimal target = 1000m, decimal current = 0m) => new()
    {
        UserId = userId,
        Name = "Viagem Europa",
        TargetAmount = target,
        CurrentAmount = current,
        Category = GoalCategory.Travel,
        Priority = 5,
        IsActive = true,
        AutoAllocateSurplus = false,
    };

    // ── CreateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsGoalResponse()
    {
        var userId = Guid.NewGuid();
        FinancialGoal? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<FinancialGoal>()))
             .Callback<FinancialGoal>(g => added = g)
             .ReturnsAsync((FinancialGoal g) => g);

        var request = new CreateFinancialGoalRequest
        {
            Name = "Reserva Bebê",
            TargetAmount = 5000m,
            CurrentAmount = 500m,
            Category = GoalCategory.Baby,
            Priority = 3,
            AutoAllocateSurplus = true,
        };

        var result = await Build().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Reserva Bebê", result.Value.Name);
        Assert.Equal(5000m, result.Value.TargetAmount);
        Assert.Equal(500m, result.Value.CurrentAmount);
        Assert.Equal(userId, added!.UserId);
        Assert.True(added.IsActive);
        Assert.True(added.AutoAllocateSurplus);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroCurrentAmount_DefaultsToZero()
    {
        var userId = Guid.NewGuid();
        FinancialGoal? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<FinancialGoal>()))
             .Callback<FinancialGoal>(g => added = g)
             .ReturnsAsync((FinancialGoal g) => g);

        var request = new CreateFinancialGoalRequest
        {
            Name = "Emergência",
            TargetAmount = 10000m,
            Category = GoalCategory.Emergency,
        };

        var result = await Build().CreateAsync(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, added!.CurrentAmount);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), userId))
             .ReturnsAsync((FinancialGoal?)null);

        var result = await Build().GetByIdAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("FinancialGoal.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var goal = NewGoal(userId, 2000m, 1000m);
        _repo.Setup(r => r.GetByIdAsync(goal.Id, userId)).ReturnsAsync(goal);

        var result = await Build().GetByIdAsync(goal.Id, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(goal.Id, result.Value.Id);
        Assert.Equal(50m, result.Value.Progress);
        Assert.Equal(1000m, result.Value.RemainingAmount);
        Assert.False(result.Value.IsCompleted);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), userId))
             .ReturnsAsync((FinancialGoal?)null);

        var result = await Build().UpdateAsync(Guid.NewGuid(), new UpdateFinancialGoalRequest
        {
            Name = "X", TargetAmount = 100m, Category = GoalCategory.Other, Priority = 5,
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("FinancialGoal.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_Found_UpdatesAllFields()
    {
        var userId = Guid.NewGuid();
        var goal = NewGoal(userId, 1000m);
        _repo.Setup(r => r.GetByIdAsync(goal.Id, userId)).ReturnsAsync(goal);

        var result = await Build().UpdateAsync(goal.Id, new UpdateFinancialGoalRequest
        {
            Name = "Novo Nome",
            TargetAmount = 2000m,
            Category = GoalCategory.House,
            Priority = 2,
            IsActive = false,
            AutoAllocateSurplus = true,
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Novo Nome", goal.Name);
        Assert.Equal(2000m, goal.TargetAmount);
        Assert.Equal(GoalCategory.House, goal.Category);
        Assert.Equal(2, goal.Priority);
        Assert.False(goal.IsActive);
        Assert.True(goal.AutoAllocateSurplus);
        _repo.Verify(r => r.UpdateAsync(goal), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── AddContributionAsync ──────────────────────────────────────────

    [Fact]
    public async Task AddContributionAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), userId))
             .ReturnsAsync((FinancialGoal?)null);

        var result = await Build().AddContributionAsync(Guid.NewGuid(), 100m, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("FinancialGoal.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddContributionAsync_ValidAmount_UpdatesAndSaves()
    {
        var userId = Guid.NewGuid();
        var goal = NewGoal(userId, 1000m, 0m);
        _repo.Setup(r => r.GetByIdAsync(goal.Id, userId)).ReturnsAsync(goal);

        var result = await Build().AddContributionAsync(goal.Id, 400m, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, goal.CurrentAmount);
        _repo.Verify(r => r.UpdateAsync(goal), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddContributionAsync_NegativeAmount_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var goal = NewGoal(userId, 1000m);
        _repo.Setup(r => r.GetByIdAsync(goal.Id, userId)).ReturnsAsync(goal);

        var result = await Build().AddContributionAsync(goal.Id, -50m, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("FinancialGoal.InvalidContribution", result.Error.Code);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), userId)).ReturnsAsync(false);

        var result = await Build().DeleteAsync(Guid.NewGuid(), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("FinancialGoal.NotFound", result.Error.Code);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Found_CallsDeleteAndSaves()
    {
        var userId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(goalId, userId)).ReturnsAsync(true);

        var result = await Build().DeleteAsync(goalId, userId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.DeleteAsync(goalId, userId), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllGoalsForUser()
    {
        var userId = Guid.NewGuid();
        var goals = new List<FinancialGoal>
        {
            NewGoal(userId, 1000m),
            NewGoal(userId, 2000m, 500m),
        };
        _repo.Setup(r => r.GetAllByUserIdAsync(userId)).ReturnsAsync(goals);

        var result = await Build().GetAllAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }
}
