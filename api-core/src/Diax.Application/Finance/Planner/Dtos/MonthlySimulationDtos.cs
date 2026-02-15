using Diax.Domain.Finance.Planner;

namespace Diax.Application.Finance.Planner.Dtos;

public class MonthlySimulationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public DateTime SimulationDate { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal ProjectedEndingBalance { get; set; }
    public decimal TotalProjectedIncome { get; set; }
    public decimal TotalProjectedExpenses { get; set; }
    public bool HasNegativeBalanceRisk { get; set; }
    public DateTime? FirstNegativeBalanceDate { get; set; }
    public decimal LowestProjectedBalance { get; set; }
    public SimulationStatus Status { get; set; }
    public List<DailyBalanceResponse> DailyBalances { get; set; } = new();
    public List<RecommendationResponse> Recommendations { get; set; } = new();
}

public class DailyBalanceResponse
{
    public DateTime Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal ClosingBalance { get; set; }
    public bool IsNegative { get; set; }
    public string RiskLevel { get; set; } = "Safe";
}

public class RecommendationResponse
{
    public RecommendationType Type { get; set; }
    public int Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
