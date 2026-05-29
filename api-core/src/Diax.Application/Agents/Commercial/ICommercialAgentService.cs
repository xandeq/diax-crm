using Diax.Application.Agents.Commercial.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Agents.Commercial;

/// <summary>
/// Agente Comercial — chat de IA com contexto do pipeline de leads do CRM.
/// </summary>
public interface ICommercialAgentService
{
    Task<Result<CommercialAgentResponse>> ChatAsync(
        Guid userId,
        CommercialAgentRequest request,
        CancellationToken cancellationToken = default);
}
