using Asp.Versioning;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Pipeline de vendas (Kanban): board com previsão de receita,
/// movimentação de estágio e valor do negócio.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pipeline")]
[Produces("application/json")]
public class PipelineController : BaseApiController
{
    private readonly PipelineService _pipelineService;
    private readonly LeadScoringService _leadScoringService;

    public PipelineController(PipelineService pipelineService, LeadScoringService leadScoringService)
    {
        _pipelineService = pipelineService;
        _leadScoringService = leadScoringService;
    }

    /// <summary>Board completo: colunas com totais + previsão ponderada + fechados (30 dias).</summary>
    [HttpGet("board")]
    public async Task<IActionResult> GetBoard(CancellationToken ct)
        => Ok(await _pipelineService.GetBoardAsync(ct));

    /// <summary>Move um lead/negócio de estágio (drag-and-drop do Kanban).</summary>
    [HttpPatch("leads/{id:guid}/stage")]
    public async Task<IActionResult> MoveStage(
        Guid id, [FromBody] MovePipelineStageRequest request, CancellationToken ct)
        => HandleResult(await _pipelineService.MoveStageAsync(id, request.Status, ct));

    /// <summary>Atualiza valor estimado e data prevista de fechamento do negócio.</summary>
    [HttpPatch("leads/{id:guid}/deal")]
    public async Task<IActionResult> UpdateDeal(
        Guid id, [FromBody] UpdatePipelineDealRequest request, CancellationToken ct)
        => HandleResult(await _pipelineService.UpdateDealAsync(
            id, request.EstimatedValue, request.ExpectedCloseDate, ct));

    /// <summary>
    /// Recalcula o lead scoring (engajamento de email + cadastro) de todos os leads
    /// e cria tasks de follow-up para os quentes parados (dedup + máx 10 por execução).
    /// </summary>
    [HttpPost("recompute-scores")]
    public async Task<IActionResult> RecomputeScores(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Usuário não autenticado." });

        var summary = await _leadScoringService.RecomputeAllAsync(userId.Value, ct);
        return Ok(summary);
    }
}
