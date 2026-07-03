using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

public class PipelineServiceTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PipelineService CreateService() => new(
        _repo.Object, _unitOfWork.Object, NullLogger<PipelineService>.Instance);

    private static Customer MakeCustomer(
        string name, CustomerStatus status, decimal? value = null, bool converted = false)
    {
        var c = new Customer(name, $"{name.ToLower().Replace(" ", ".")}@teste.com");
        c.UpdateStatus(status);
        if (value.HasValue) c.UpdateDealInfo(value, null);
        if (converted) c.ConvertToCustomer();
        return c;
    }

    [Fact]
    public async Task GetBoardAsync_ComputesWeightedForecast_AndColumnTotals()
    {
        var customers = new List<Customer>
        {
            MakeCustomer("Lead A", CustomerStatus.Lead, 1000m),          // 1000 × 0.10 = 100
            MakeCustomer("Contato B", CustomerStatus.Contacted, 2000m),  // 2000 × 0.25 = 500
            MakeCustomer("Quali C", CustomerStatus.Qualified, 4000m),    // 4000 × 0.50 = 2000
            MakeCustomer("Nego D", CustomerStatus.Negotiating, 8000m),   // 8000 × 0.75 = 6000
            MakeCustomer("Sem valor", CustomerStatus.Lead),              // sem valor → 0 na previsão
            MakeCustomer("Fechado E", CustomerStatus.Negotiating, 5000m, converted: true),
        };
        _repo.Setup(r => r.GetPipelineAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        var board = await CreateService().GetBoardAsync();

        Assert.Equal(8600m, board.WeightedForecast); // 100+500+2000+6000
        Assert.Equal(15000m, board.TotalOpenValue);
        Assert.Equal(5, board.TotalOpenDeals);
        Assert.Equal(5000m, board.WonLast30DaysValue);
        Assert.Equal(1, board.WonLast30DaysCount);
        Assert.Equal(5, board.Columns.Count); // 4 abertos + fechados

        var negotiating = board.Columns.Single(c => c.Status == CustomerStatus.Negotiating);
        Assert.Equal(8000m, negotiating.TotalValue);
        Assert.Equal(0.75m, negotiating.Probability);
        Assert.Single(negotiating.Cards);
    }

    [Fact]
    public async Task GetBoardAsync_CapsCardsPerColumn_ButTotalsCoverAll()
    {
        var many = Enumerable.Range(1, PipelineService.MaxCardsPerColumn + 20)
            .Select(i => MakeCustomer($"Lead {i}", CustomerStatus.Lead, 10m))
            .ToList();
        _repo.Setup(r => r.GetPipelineAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(many);

        var board = await CreateService().GetBoardAsync();

        var leadCol = board.Columns.Single(c => c.Status == CustomerStatus.Lead);
        Assert.Equal(PipelineService.MaxCardsPerColumn, leadCol.Cards.Count); // payload limitado
        Assert.Equal(many.Count, leadCol.Count);                              // contagem completa
        Assert.Equal(many.Count * 10m, leadCol.TotalValue);                   // total completo
    }

    [Fact]
    public async Task GetBoardAsync_OrdersCardsByValueThenScore()
    {
        var big = MakeCustomer("Grande", CustomerStatus.Lead, 9000m);
        var small = MakeCustomer("Pequeno", CustomerStatus.Lead, 100m);
        _repo.Setup(r => r.GetPipelineAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { small, big });

        var board = await CreateService().GetBoardAsync();

        var leadCol = board.Columns.Single(c => c.Status == CustomerStatus.Lead);
        Assert.Equal("Grande", leadCol.Cards[0].Name);
    }

    [Fact]
    public async Task MoveStageAsync_ToCustomer_SetsConvertedAt()
    {
        var customer = MakeCustomer("Fechando", CustomerStatus.Negotiating, 3000m);
        _repo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await CreateService().MoveStageAsync(customer.Id, CustomerStatus.Customer);

        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerStatus.Customer, customer.Status);
        Assert.NotNull(customer.ConvertedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveStageAsync_RejectsInvalidTarget()
    {
        var result = await CreateService().MoveStageAsync(Guid.NewGuid(), CustomerStatus.Churned);

        Assert.True(result.IsFailure);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MoveStageAsync_ReturnsNotFound_WhenCustomerMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await CreateService().MoveStageAsync(Guid.NewGuid(), CustomerStatus.Qualified);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateDealAsync_PersistsValueAndDate()
    {
        var customer = MakeCustomer("Negócio", CustomerStatus.Qualified);
        _repo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        var closeDate = DateTime.UtcNow.AddDays(15);

        var result = await CreateService().UpdateDealAsync(customer.Id, 7500m, closeDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(7500m, customer.EstimatedValue);
        Assert.Equal(closeDate, customer.ExpectedCloseDate);
        Assert.Equal(7500m, result.Value.EstimatedValue);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100_000_001)]
    public async Task UpdateDealAsync_RejectsOutOfRangeValues(decimal value)
    {
        var result = await CreateService().UpdateDealAsync(Guid.NewGuid(), value, null);

        Assert.True(result.IsFailure);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
