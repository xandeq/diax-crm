using Asp.Versioning;
using Diax.Application.Ads;
using Diax.Application.Ads.Dtos;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller para gerenciamento de Anúncios (Facebook Ads / Graph API).
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AdsController : BaseApiController
{
    private readonly IFacebookAdsService _adsService;
    private readonly DiaxDbContext _db;
    private readonly ILogger<AdsController> _logger;

    public AdsController(
        IFacebookAdsService adsService,
        DiaxDbContext db,
        ILogger<AdsController> logger)
    {
        _adsService = adsService;
        _db = db;
        _logger = logger;
    }

    // ===== ACCOUNT CONNECTION =====

    /// <summary>
    /// Conecta uma conta de anúncios do Facebook.
    /// </summary>
    [HttpPost("connect")]
    [ProducesResponseType(typeof(AdAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Connect(
        [FromBody] ConnectAdAccountRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} connecting Facebook Ad Account {AdAccountId}", userId, request.AdAccountId);

        var result = await _adsService.ConnectAccountAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém a conta de anúncios conectada do usuário.
    /// </summary>
    [HttpGet("account")]
    [ProducesResponseType(typeof(AdAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetConnectedAccountAsync(userId.Value, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Sincroniza as informações da conta com o Facebook.
    /// </summary>
    [HttpPost("account/sync")]
    [ProducesResponseType(typeof(AdAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncAccount(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.SyncAccountInfoAsync(userId.Value, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Desconecta a conta de anúncios do Facebook.
    /// </summary>
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} disconnecting Facebook Ad Account", userId);

        var result = await _adsService.DisconnectAccountAsync(userId.Value, ct);
        return HandleResult(result);
    }

    // ===== SUMMARY =====

    /// <summary>
    /// Obtém o resumo geral da conta de anúncios.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdAccountSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetAccountSummaryAsync(userId.Value, ct);
        return HandleResult(result);
    }

    // ===== CAMPAIGNS =====

    /// <summary>
    /// Lista todas as campanhas da conta de anúncios.
    /// </summary>
    [HttpGet("campaigns")]
    [ProducesResponseType(typeof(List<FacebookCampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaigns(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetCampaignsAsync(userId.Value, ct);
        return HandleResult(result);
    }

    // ===== AD SETS =====

    /// <summary>
    /// Lista conjuntos de anúncios (ad sets), opcionalmente filtrado por campanha.
    /// </summary>
    [HttpGet("adsets")]
    [ProducesResponseType(typeof(List<FacebookAdSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdSets(
        [FromQuery] string? campaignId,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetAdSetsAsync(userId.Value, campaignId, ct);
        return HandleResult(result);
    }

    // ===== ADS =====

    /// <summary>
    /// Lista anúncios, opcionalmente filtrado por ad set.
    /// </summary>
    [HttpGet("ads")]
    [ProducesResponseType(typeof(List<FacebookAdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAds(
        [FromQuery] string? adSetId,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetAdsAsync(userId.Value, adSetId, ct);
        return HandleResult(result);
    }

    // ===== INSIGHTS =====

    /// <summary>
    /// Obtém métricas e insights dos anúncios.
    /// </summary>
    [HttpGet("insights")]
    [ProducesResponseType(typeof(List<FacebookInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInsights(
        [FromQuery] string? datePreset,
        [FromQuery] string? since,
        [FromQuery] string? until,
        [FromQuery] string level = "campaign",
        CancellationToken ct = default)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var request = new InsightsRequest(datePreset ?? "last_30d", since, until, level);
        var result = await _adsService.GetInsightsAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    // ===== CAMPAIGNS - WRITE =====

    /// <summary>
    /// Cria uma nova campanha no Facebook.
    /// </summary>
    [HttpPost("campaigns")]
    [ProducesResponseType(typeof(CampaignWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} creating campaign {Name}", userId, request.Name);

        var result = await _adsService.CreateCampaignAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o status de uma campanha (ACTIVE/PAUSED).
    /// </summary>
    [HttpPost("campaigns/{campaignId}/status")]
    [ProducesResponseType(typeof(CampaignWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCampaignStatus(
        [FromRoute] string campaignId,
        [FromBody] UpdateCampaignStatusRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} updating campaign {CampaignId} status to {Status}", userId, campaignId, request.Status);

        var result = await _adsService.UpdateCampaignStatusAsync(userId.Value, campaignId, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o orçamento de uma campanha.
    /// </summary>
    [HttpPost("campaigns/{campaignId}/budget")]
    [ProducesResponseType(typeof(CampaignWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCampaignBudget(
        [FromRoute] string campaignId,
        [FromBody] UpdateCampaignBudgetRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} updating campaign {CampaignId} budget", userId, campaignId);

        var result = await _adsService.UpdateCampaignBudgetAsync(userId.Value, campaignId, request, ct);
        return HandleResult(result);
    }

    // ===== AD SETS - WRITE =====

    /// <summary>
    /// Cria um novo ad set em uma campanha.
    /// </summary>
    [HttpPost("adsets")]
    [ProducesResponseType(typeof(AdSetWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAdSet(
        [FromBody] CreateAdSetRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} creating ad set {Name}", userId, request.Name);

        var result = await _adsService.CreateAdSetAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o status de um ad set (ACTIVE/PAUSED).
    /// </summary>
    [HttpPost("adsets/{adSetId}/status")]
    [ProducesResponseType(typeof(AdSetWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdSetStatus(
        [FromRoute] string adSetId,
        [FromBody] UpdateAdSetStatusRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} updating ad set {AdSetId} status to {Status}", userId, adSetId, request.Status);

        var result = await _adsService.UpdateAdSetStatusAsync(userId.Value, adSetId, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o orçamento de um ad set.
    /// </summary>
    [HttpPost("adsets/{adSetId}/budget")]
    [ProducesResponseType(typeof(AdSetWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdSetBudget(
        [FromRoute] string adSetId,
        [FromBody] UpdateAdSetBudgetRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} updating ad set {AdSetId} budget", userId, adSetId);

        var result = await _adsService.UpdateAdSetBudgetAsync(userId.Value, adSetId, request, ct);
        return HandleResult(result);
    }

    // ===== ADS - WRITE =====

    /// <summary>
    /// Cria um novo anúncio em um ad set.
    /// </summary>
    [HttpPost("ads")]
    [ProducesResponseType(typeof(AdWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAd(
        [FromBody] CreateAdRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} creating ad {Name}", userId, request.Name);

        var result = await _adsService.CreateAdAsync(userId.Value, request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Atualiza o status de um anúncio (ACTIVE/PAUSED).
    /// </summary>
    [HttpPost("ads/{adId}/status")]
    [ProducesResponseType(typeof(AdWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdStatus(
        [FromRoute] string adId,
        [FromBody] UpdateAdStatusRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} updating ad {AdId} status to {Status}", userId, adId, request.Status);

        var result = await _adsService.UpdateAdStatusAsync(userId.Value, adId, request, ct);
        return HandleResult(result);
    }

    // ===== AD CREATIVES =====

    /// <summary>
    /// Lista os criativos (ads creatives) da conta.
    /// </summary>
    [HttpGet("creatives")]
    [ProducesResponseType(typeof(List<AdCreativeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCreatives(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        var result = await _adsService.GetCreativesAsync(userId.Value, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Cria um novo criativo (ads creative) na conta.
    /// </summary>
    [HttpPost("creatives")]
    [ProducesResponseType(typeof(AdCreativeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCreative(
        [FromBody] CreateAdCreativeRequest request,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null) return Unauthorized();

        _logger.LogInformation("User {UserId} creating creative {Name}", userId, request.Name);

        var result = await _adsService.CreateCreativeAsync(userId.Value, request, ct);
        return HandleResult(result);
    }
}
