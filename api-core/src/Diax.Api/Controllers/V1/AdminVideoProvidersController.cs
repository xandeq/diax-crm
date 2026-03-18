using Diax.Application.AI.QuotaManagement;
using Diax.Infrastructure.Data;
using Diax.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Admin endpoints for managing AI video providers and their quotas.
/// Restricted to Admin role.
/// </summary>
[ApiController]
[Route("api/v1/admin/video-providers")]
[Authorize(Roles = "Admin")]
public class AdminVideoProvidersController : BaseApiController
{
    private readonly DiaxDbContext _db;
    private readonly IAiQuotaService _quotaService;
    private readonly ILogger<AdminVideoProvidersController> _logger;

    public AdminVideoProvidersController(
        DiaxDbContext db,
        IAiQuotaService quotaService,
        ILogger<AdminVideoProvidersController> logger)
    {
        _db = db;
        _quotaService = quotaService;
        _logger = logger;
    }

    /// <summary>
    /// Get all video providers with their models.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VideoProviderAdminDto>>> GetAllVideoProviders(CancellationToken ct = default)
    {
        var providers = await _db.AiProviders
            .Where(p => p.IsVideoProvider)
            .Include(p => p.Models)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = providers.Select(p => new VideoProviderAdminDto
        {
            Id = p.Id,
            Name = p.Name,
            ProviderKey = p.Key,
            IsEnabled = p.IsEnabled,
            BaseUrl = p.BaseUrl,
            Models = p.Models.Select(m => new AiModelAdminDto
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                ModelKey = m.ModelKey,
                IsActive = m.IsActive,
                MaxDurationSeconds = m.MaxDurationSeconds,
                MaxResolution = m.MaxResolution,
                SupportedAspectRatios = m.SupportedAspectRatios
            }).ToList()
        }).ToList();

        return dtos;
    }

    /// <summary>
    /// Get a specific video provider with its models and quota status.
    /// </summary>
    [HttpGet("{providerId}")]
    public async Task<ActionResult<VideoProviderDetailDto>> GetVideoProvider(Guid providerId, CancellationToken ct = default)
    {
        var provider = await _db.AiProviders
            .Include(p => p.Models)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId && p.IsVideoProvider, ct);

        if (provider == null)
            return NotFound();

        var quotaStatus = await _quotaService.GetQuotaStatusAsync(providerId);

        return new VideoProviderDetailDto
        {
            Id = provider.Id,
            Name = provider.Name,
            ProviderKey = provider.Key,
            IsEnabled = provider.IsEnabled,
            BaseUrl = provider.BaseUrl,
            Models = provider.Models.Select(m => new AiModelAdminDto
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                ModelKey = m.ModelKey,
                IsActive = m.IsActive,
                MaxDurationSeconds = m.MaxDurationSeconds,
                MaxResolution = m.MaxResolution,
                SupportedAspectRatios = m.SupportedAspectRatios
            }).ToList(),
            QuotaStatus = quotaStatus
        };
    }

    /// <summary>
    /// Update a video provider (enable/disable, description).
    /// </summary>
    [HttpPut("{providerId}")]
    public async Task<IActionResult> UpdateProvider(Guid providerId, [FromBody] UpdateVideoProviderRequest request, CancellationToken ct = default)
    {
        var provider = await _db.AiProviders.FirstOrDefaultAsync(p => p.Id == providerId && p.IsVideoProvider, ct);
        if (provider == null)
            return NotFound();

        if (request.IsEnabled)
            provider.Enable();
        else
            provider.Disable();

        _db.AiProviders.Update(provider);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Video provider '{ProviderName}' updated: IsEnabled={IsEnabled}",
            provider.Name, provider.IsEnabled);

        return NoContent();
    }

    /// <summary>
    /// Get quota status for a specific provider.
    /// </summary>
    [HttpGet("{providerId}/quota")]
    public async Task<ActionResult<QuotaStatusDto>> GetProviderQuota(Guid providerId, CancellationToken ct = default)
    {
        var quotaStatus = await _quotaService.GetQuotaStatusAsync(providerId, ct);
        if (quotaStatus == null)
            return NotFound();

        return quotaStatus;
    }

    /// <summary>
    /// Manually reset a provider's daily quota (useful for testing or emergency reset).
    /// </summary>
    [HttpPost("{providerId}/quota/reset")]
    public async Task<ActionResult<QuotaResetResponse>> ResetProviderQuota(Guid providerId, CancellationToken ct = default)
    {
        var quota = await _db.AiProviderQuotas
            .Include(q => q.AiProvider)
            .FirstOrDefaultAsync(q => q.AiProviderId == providerId, ct);

        if (quota == null)
            return NotFound();

        var previousUsage = quota.CurrentDailyUsage;
        quota.ResetDailyQuota();

        _db.AiProviderQuotas.Update(quota);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Quota reset for provider '{ProviderName}': Previous usage={PreviousUsage}, Reset at={ResetTime}",
            quota.AiProvider.Name, previousUsage, DateTime.UtcNow);

        return new QuotaResetResponse
        {
            Message = $"Quota reset successfully for {quota.AiProvider.Name}",
            PreviousUsage = previousUsage,
            ResetAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update a model's active status.
    /// </summary>
    [HttpPut("{providerId}/models/{modelId}")]
    public async Task<IActionResult> UpdateModel(
        Guid providerId,
        Guid modelId,
        [FromBody] UpdateAiModelRequest request,
        CancellationToken ct = default)
    {
        var model = await _db.AiModels
            .FirstOrDefaultAsync(m => m.Id == modelId && m.ProviderId == providerId, ct);

        if (model == null)
            return NotFound();

        if (request.IsActive)
            model.Activate();
        else
            model.Deactivate();

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            model.UpdateDisplayName(request.DisplayName);

        _db.AiModels.Update(model);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Model '{ModelName}' updated: IsActive={IsActive}",
            model.DisplayName, model.IsActive);

        return NoContent();
    }

    /// <summary>
    /// Get all models for a specific provider.
    /// </summary>
    [HttpGet("{providerId}/models")]
    public async Task<ActionResult<List<AiModelAdminDto>>> GetProviderModels(Guid providerId, CancellationToken ct = default)
    {
        var models = await _db.AiModels
            .Where(m => m.ProviderId == providerId)
            .AsNoTracking()
            .ToListAsync(ct);

        return models.Select(m => new AiModelAdminDto
        {
            Id = m.Id,
            DisplayName = m.DisplayName,
            ModelKey = m.ModelKey,
            IsActive = m.IsActive,
            MaxDurationSeconds = m.MaxDurationSeconds,
            MaxResolution = m.MaxResolution,
            SupportedAspectRatios = m.SupportedAspectRatios
        }).ToList();
    }
}

/// <summary>
/// DTO for video provider summary in list view.
/// </summary>
public class VideoProviderAdminDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ProviderKey { get; set; }
    public bool IsEnabled { get; set; }
    public string? BaseUrl { get; set; }
    public List<AiModelAdminDto> Models { get; set; } = new();
}

/// <summary>
/// DTO for video provider detailed view with quota status.
/// </summary>
public class VideoProviderDetailDto : VideoProviderAdminDto
{
    public QuotaStatusDto QuotaStatus { get; set; }
}

/// <summary>
/// DTO for AI model details in admin view.
/// </summary>
public class AiModelAdminDto
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? ModelKey { get; set; }
    public bool IsActive { get; set; }
    public int? MaxDurationSeconds { get; set; }
    public string? MaxResolution { get; set; }
    public string? SupportedAspectRatios { get; set; }
}

/// <summary>
/// Request to update a video provider.
/// </summary>
public class UpdateVideoProviderRequest
{
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request to update an AI model.
/// </summary>
public class UpdateAiModelRequest
{
    public bool IsActive { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>
/// Response from quota reset operation.
/// </summary>
public class QuotaResetResponse
{
    public string? Message { get; set; }
    public int PreviousUsage { get; set; }
    public DateTime ResetAt { get; set; }
}
