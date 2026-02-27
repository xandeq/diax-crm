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
}
