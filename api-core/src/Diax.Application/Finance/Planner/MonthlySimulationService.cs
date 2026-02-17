using Diax.Application.Common;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

// Alias to avoid ambiguity with Diax.Domain.Finance.TransactionType (unified)
using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Application.Finance.Planner;

/// <summary>
/// Serviço orquestrador para gerar e gerenciar simulações mensais
/// </summary>
public class MonthlySimulationService : IApplicationService
{
    private readonly IMonthlySimulationRepository _simulationRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IFinancialGoalRepository _goalRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly ICreditCardInvoiceRepository _invoiceRepository;
    private readonly CashFlowProjectionService _cashFlowService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonthlySimulationService> _logger;

    public MonthlySimulationService(
        IMonthlySimulationRepository simulationRepository,
        IRecurringTransactionRepository recurringRepository,
        IFinancialGoalRepository goalRepository,
        IFinancialAccountRepository accountRepository,
        IExpenseRepository expenseRepository,
        IIncomeRepository incomeRepository,
        ICreditCardInvoiceRepository invoiceRepository,
        CashFlowProjectionService cashFlowService,
        IUnitOfWork unitOfWork,
        ILogger<MonthlySimulationService> logger)
    {
        _simulationRepository = simulationRepository;
        _recurringRepository = recurringRepository;
        _goalRepository = goalRepository;
        _accountRepository = accountRepository;
        _expenseRepository = expenseRepository;
        _incomeRepository = incomeRepository;
        _invoiceRepository = invoiceRepository;
        _cashFlowService = cashFlowService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gera ou obtém a simulação ativa para um mês específico
    /// </summary>
    public async Task<Result<MonthlySimulationResponse>> GetOrGenerateSimulationAsync(
        int month,
        int year,
        Guid userId)
    {
        try
        {
            // Verifica se já existe uma simulação ativa
            var existing = await _simulationRepository.GetActiveSimulationForMonthAsync(userId, month, year);
            if (existing != null)
            {
                _logger.LogInformation("Found existing simulation for {Month}/{Year}", month, year);
                return Result<MonthlySimulationResponse>.Success(MapToResponse(existing));
            }

            // Gera nova simulação
            _logger.LogInformation("Generating new simulation for {Month}/{Year}", month, year);
            return await GenerateSimulationAsync(month, year, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or generate simulation for {Month}/{Year}", month, year);
            return Result.Failure<MonthlySimulationResponse>(
                new Error("Simulation.GenerationFailed", "Falha ao gerar simulação"));
        }
    }

    /// <summary>
    /// Gera uma nova simulação mensal
    /// </summary>
    public async Task<Result<MonthlySimulationResponse>> GenerateSimulationAsync(
        int month,
        int year,
        Guid userId)
    {
        try
        {
            _logger.LogInformation("Starting simulation generation for user {UserId}, {Month}/{Year}",
                userId, month, year);

            // 1. Calcular saldo inicial
            var startingBalance = await CalculateStartingBalanceAsync(userId, month, year);
            _logger.LogInformation("Starting balance: {Balance:C}", startingBalance);

            // 2. Criar objeto de simulação
            var simulation = new MonthlySimulation
            {
                UserId = userId,
                ReferenceMonth = month,
                ReferenceYear = year,
                SimulationDate = DateTime.UtcNow,
                StartingBalance = startingBalance,
                Status = SimulationStatus.Active
            };

            // 3. Coletar transações projetadas
            var projectedTransactions = await CollectProjectedTransactionsAsync(userId, month, year);
            simulation.ProjectedTransactions = projectedTransactions;

            // 4. Calcular totais
            simulation.TotalProjectedIncome = projectedTransactions
                .Where(t => t.Type == PlannerTransactionType.Income)
                .Sum(t => t.Amount);

            simulation.TotalProjectedExpenses = projectedTransactions
                .Where(t => t.Type == PlannerTransactionType.Expense)
                .Sum(t => t.Amount);

            // 5. Projetar saldos diários
            var dailyBalances = _cashFlowService.ProjectDailyBalances(
                startingBalance,
                projectedTransactions,
                simulation.Id);

            simulation.DailyBalances = dailyBalances;

            // 6. Atualizar campos de risco
            simulation.HasNegativeBalanceRisk = _cashFlowService.HasNegativeBalanceRisk(dailyBalances);
            simulation.FirstNegativeBalanceDate = _cashFlowService.FindFirstNegativeBalanceDate(dailyBalances);
            simulation.LowestProjectedBalance = _cashFlowService.FindLowestBalance(dailyBalances);
            simulation.ProjectedEndingBalance = dailyBalances.Any()
                ? dailyBalances.Last().ClosingBalance
                : startingBalance;

            // 7. Gerar recomendações
            simulation.Recommendations = GenerateRecommendations(simulation);

            // 8. Persistir
            await _simulationRepository.AddAsync(simulation);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Simulation generated successfully. ID: {SimulationId}", simulation.Id);
            return Result<MonthlySimulationResponse>.Success(MapToResponse(simulation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate simulation for {Month}/{Year}", month, year);
            return Result.Failure<MonthlySimulationResponse>(
                new Error("Simulation.GenerationFailed", "Falha ao gerar simulação"));
        }
    }

    private async Task<decimal> CalculateStartingBalanceAsync(Guid userId, int month, int year)
    {
        // Obter todas as contas ativas
        var accounts = await _accountRepository.GetAllByUserIdAsync(userId);
        var activeAccounts = accounts.Where(a => a.IsActive);

        // Saldo inicial = soma dos saldos de todas as contas
        return activeAccounts.Sum(a => a.Balance);
    }

    private async Task<List<ProjectedTransaction>> CollectProjectedTransactionsAsync(
        Guid userId,
        int month,
        int year)
    {
        var transactions = new List<ProjectedTransaction>();

        // 1. Expandir transações recorrentes
        var recurrings = await _recurringRepository.GetRecurringForMonthAsync(userId, month, year);
        foreach (var recurring in recurrings)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var occurrences = recurring.GetNextOccurrences(startDate, endDate);

            foreach (var date in occurrences)
            {
                transactions.Add(new ProjectedTransaction
                {
                    SimulationId = Guid.Empty, // Será setado ao persistir
                    Type = recurring.Type,
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    Date = date,
                    CategoryId = recurring.CategoryId,
                    PaymentMethod = recurring.PaymentMethod,
                    Priority = recurring.Priority,
                    Source = ProjectionSource.Recurring,
                    SourceId = recurring.Id,
                    CreditCardId = recurring.CreditCardId,
                    FinancialAccountId = recurring.FinancialAccountId,
                    Status = ProjectedStatus.Planned
                });
            }
        }

        _logger.LogInformation("Collected {Count} projected transactions from recurrings", transactions.Count);
        return transactions.OrderBy(t => t.Date).ThenBy(t => t.Priority).ToList();
    }

    private List<SimulationRecommendation> GenerateRecommendations(MonthlySimulation simulation)
    {
        var recommendations = new List<SimulationRecommendation>();

        // Recomendação 1: Saldo negativo
        if (simulation.HasNegativeBalanceRisk && simulation.FirstNegativeBalanceDate.HasValue)
        {
            recommendations.Add(new SimulationRecommendation
            {
                SimulationId = simulation.Id,
                Type = RecommendationType.Alert,
                Priority = 1,
                Title = "⚠️ Risco de Saldo Negativo",
                Message = $"Saldo ficará negativo em {simulation.FirstNegativeBalanceDate.Value:dd/MM/yyyy}. " +
                         $"Considere adiar despesas ou aumentar receitas."
            });
        }

        // Recomendação 2: Abaixo do colchão mínimo
        if (simulation.ProjectedEndingBalance < 1000)
        {
            recommendations.Add(new SimulationRecommendation
            {
                SimulationId = simulation.Id,
                Type = RecommendationType.Alert,
                Priority = 2,
                Title = "💰 Saldo Final Baixo",
                Message = $"Saldo final projetado (R$ {simulation.ProjectedEndingBalance:N2}) está abaixo do colchão de segurança (R$ 1.000)."
            });
        }

        // Recomendação 3: Despesas > Receitas
        if (simulation.TotalProjectedExpenses > simulation.TotalProjectedIncome)
        {
            var deficit = simulation.TotalProjectedExpenses - simulation.TotalProjectedIncome;
            recommendations.Add(new SimulationRecommendation
            {
                SimulationId = simulation.Id,
                Type = RecommendationType.IncreaseIncome,
                Priority = 3,
                Title = "📉 Déficit Projetado",
                Message = $"Despesas (R$ {simulation.TotalProjectedExpenses:N2}) excedem receitas (R$ {simulation.TotalProjectedIncome:N2}) " +
                         $"em R$ {deficit:N2}."
            });
        }

        return recommendations;
    }

    private static MonthlySimulationResponse MapToResponse(MonthlySimulation simulation)
    {
        return new MonthlySimulationResponse
        {
            Id = simulation.Id,
            UserId = simulation.UserId,
            ReferenceMonth = simulation.ReferenceMonth,
            ReferenceYear = simulation.ReferenceYear,
            SimulationDate = simulation.SimulationDate,
            StartingBalance = simulation.StartingBalance,
            ProjectedEndingBalance = simulation.ProjectedEndingBalance,
            TotalProjectedIncome = simulation.TotalProjectedIncome,
            TotalProjectedExpenses = simulation.TotalProjectedExpenses,
            HasNegativeBalanceRisk = simulation.HasNegativeBalanceRisk,
            FirstNegativeBalanceDate = simulation.FirstNegativeBalanceDate,
            LowestProjectedBalance = simulation.LowestProjectedBalance,
            Status = simulation.Status,
            DailyBalances = simulation.DailyBalances.Select(d => new DailyBalanceResponse
            {
                Date = d.Date,
                OpeningBalance = d.OpeningBalance,
                TotalIncome = d.TotalIncome,
                TotalExpenses = d.TotalExpenses,
                ClosingBalance = d.ClosingBalance,
                IsNegative = d.IsNegative,
                RiskLevel = d.GetRiskLevel()
            }).ToList(),
            Recommendations = simulation.Recommendations.Select(r => new RecommendationResponse
            {
                Type = r.Type,
                Priority = r.Priority,
                Title = r.Title,
                Message = r.Message
            }).ToList()
        };
    }
}
