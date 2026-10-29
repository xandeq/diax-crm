using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Tasks;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

/// <summary>
/// Pipeline de vendas (Kanban): board com totais por estágio e previsão
/// ponderada de receita, movimentação de estágio e edição do valor do negócio.
/// </summary>
public class PipelineService : IApplicationService
{
    /// <summary>
    /// Probabilidade de fechamento por estágio — pesos da previsão ponderada.
    /// Valores padrão de mercado para funil de serviços; ajustáveis no futuro.
    /// </summary>
    public static readonly IReadOnlyDictionary<CustomerStatus, decimal> StageProbability =
        new Dictionary<CustomerStatus, decimal>
        {
            [CustomerStatus.Lead] = 0.10m,
            [CustomerStatus.Contacted] = 0.25m,
            [CustomerStatus.Qualified] = 0.50m,
            [CustomerStatus.Negotiating] = 0.75m,
        };

    private static readonly IReadOnlyDictionary<CustomerStatus, string> StageLabel =
        new Dictionary<CustomerStatus, string>
        {
            [CustomerStatus.Lead] = "Lead",
            [CustomerStatus.Contacted] = "Contactado",
            [CustomerStatus.Qualified] = "Qualificado",
            [CustomerStatus.Negotiating] = "Negociando",
            [CustomerStatus.Customer] = "Fechado (30 dias)",
        };

    /// <summary>
    /// Máximo de cards retornados por coluna. Com bases grandes (4k+ leads) o board
    /// ficaria com payload de MBs e a tela inutilizável — os TOTAIS (Count/TotalValue/
    /// previsão) continuam calculados sobre TODOS os registros do estágio.
    /// </summary>
    public const int MaxCardsPerColumn = 50;

    /// <summary>Estágios válidos como destino de drag-and-drop no Kanban.</summary>
    private static readonly HashSet<CustomerStatus> ValidMoveTargets = new()
    {
        CustomerStatus.Lead,
        CustomerStatus.Contacted,
        CustomerStatus.Qualified,
        CustomerStatus.Negotiating,
        CustomerStatus.Customer,
        CustomerStatus.Inactive,
    };

    private readonly ICustomerRepository _customerRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(
        ICustomerRepository customerRepository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ILogger<PipelineService> logger)
    {
        _customerRepository = customerRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PipelineBoardDto> GetBoardAsync(CancellationToken ct = default)
    {
        var wonSince = DateTime.UtcNow.AddDays(-30);
        var customers = (await _customerRepository.GetPipelineAsync(wonSince, ct)).ToList();
        var openStages = new[]
        {
            CustomerStatus.Lead, CustomerStatus.Contacted,
            CustomerStatus.Qualified, CustomerStatus.Negotiating,
        };

        List<Customer> OrderedStage(CustomerStatus stage) => customers
            .Where(c => c.Status == stage)
            .OrderByDescending(c => c.EstimatedValue ?? 0)
            .ThenByDescending(c => c.LeadScore ?? 0)
            .ToList();

        // Próxima ação só dos cards que aparecem (Take por coluna aberta): com milhares de leads,
        // consultar todos viraria um IN gigante a cada carga do board.
        var visibleIds = openStages
            .SelectMany(st => OrderedStage(st).Take(MaxCardsPerColumn))
            .Select(c => c.Id)
            .ToList();
        var nextActions = await _taskRepository.GetNextOpenTasksByCustomerAsync(visibleIds, ct);

        var columns = new List<PipelineColumnDto>();

        decimal weightedForecast = 0;
        decimal totalOpenValue = 0;
        var totalOpenDeals = 0;

        foreach (var stage in openStages)
        {
            var stageCustomers = OrderedStage(stage);

            var stageValue = stageCustomers.Sum(c => c.EstimatedValue ?? 0);
            var probability = StageProbability[stage];

            weightedForecast += stageValue * probability;
            totalOpenValue += stageValue;
            totalOpenDeals += stageCustomers.Count;

            columns.Add(new PipelineColumnDto(
                Status: stage,
                Label: StageLabel[stage],
                Probability: probability,
                Count: stageCustomers.Count,
                TotalValue: stageValue,
                Cards: stageCustomers.Take(MaxCardsPerColumn).Select(c => ToCard(c, nextActions)).ToList()));
        }

        // Coluna de fechados: convertidos nos últimos 30 dias (receita ganha, fora da previsão)
        var won = customers
            .Where(c => c.Status == CustomerStatus.Customer && c.ConvertedAt >= wonSince)
            .OrderByDescending(c => c.ConvertedAt)
            .ToList();
        var wonValue = won.Sum(c => c.EstimatedValue ?? 0);

        columns.Add(new PipelineColumnDto(
            Status: CustomerStatus.Customer,
            Label: StageLabel[CustomerStatus.Customer],
            Probability: 1m,
            Count: won.Count,
            TotalValue: wonValue,
            Cards: won.Take(MaxCardsPerColumn).Select(c => ToCard(c)).ToList()));

        return new PipelineBoardDto(
            Columns: columns,
            TotalOpenDeals: totalOpenDeals,
            TotalOpenValue: totalOpenValue,
            WeightedForecast: weightedForecast,
            WonLast30DaysValue: wonValue,
            WonLast30DaysCount: won.Count);
    }

    public async Task<Result<PipelineCardDto>> MoveStageAsync(
        Guid customerId, CustomerStatus targetStatus, CancellationToken ct = default)
    {
        if (!ValidMoveTargets.Contains(targetStatus))
            return Result.Failure<PipelineCardDto>(
                Error.Validation("Status", $"Estágio '{targetStatus}' não é um destino válido no pipeline."));

        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer == null)
            return Result.Failure<PipelineCardDto>(Error.NotFound("Customer", customerId.ToString()));

        if (targetStatus == CustomerStatus.Customer)
            customer.ConvertToCustomer(); // registra ConvertedAt (entra em "Fechado")
        else
            customer.UpdateStatus(targetStatus);

        await _customerRepository.UpdateAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pipeline: {CustomerId} movido para {Status}", customerId, targetStatus);

        return ToCard(customer);
    }

    public async Task<Result<PipelineCardDto>> UpdateDealAsync(
        Guid customerId, decimal? estimatedValue, DateTime? expectedCloseDate, CancellationToken ct = default)
    {
        if (estimatedValue is < 0)
            return Result.Failure<PipelineCardDto>(
                Error.Validation("EstimatedValue", "Valor estimado não pode ser negativo."));

        if (estimatedValue is > 100_000_000)
            return Result.Failure<PipelineCardDto>(
                Error.Validation("EstimatedValue", "Valor estimado acima do limite permitido."));

        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer == null)
            return Result.Failure<PipelineCardDto>(Error.NotFound("Customer", customerId.ToString()));

        customer.UpdateDealInfo(estimatedValue, expectedCloseDate);
        await _customerRepository.UpdateAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pipeline: negócio de {CustomerId} atualizado (valor: {Value})", customerId, estimatedValue);

        return ToCard(customer);
    }

    private static PipelineCardDto ToCard(Customer c, IReadOnlyDictionary<Guid, NextTaskInfo>? nextActions = null) => new(
        Id: c.Id,
        Name: string.IsNullOrWhiteSpace(c.NormalizedName) ? c.Name : c.NormalizedName,
        CompanyName: c.CompanyName,
        Email: c.Email,
        Phone: c.Phone,
        WhatsApp: c.WhatsApp,
        EstimatedValue: c.EstimatedValue,
        ExpectedCloseDate: c.ExpectedCloseDate,
        LeadScore: c.LeadScore,
        Segment: c.Segment?.ToString(),
        LastContactAt: c.LastContactAt,
        Tags: c.Tags,
        NextActionTitle: nextActions != null && nextActions.TryGetValue(c.Id, out var n) ? n.Title : null,
        NextActionDue: nextActions != null && nextActions.TryGetValue(c.Id, out var n2) ? n2.DueDate : null);
}
