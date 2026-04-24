using Diax.Application.AI;
using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers;

/// <summary>
/// Base controller for all AI-related endpoints.
/// Centralizes RBAC validation, user resolution, and error handling.
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseAiController : BaseApiController
{
    protected readonly IAiCatalogService _catalogService;
    protected readonly DiaxDbContext _db;
    protected readonly ILogger _logger;

    protected BaseAiController(
        IAiCatalogService catalogService,
        DiaxDbContext db,
        ILogger logger)
    {
        _catalogService = catalogService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Executes an AI action with centralized RBAC validation and error handling.
    ///
    /// This method:
    /// 1. Resolves the user ID from JWT
    /// 2. Validates user has access to provider + model via group permissions
    /// 3. Executes the action with error handling
    /// 4. Maps exceptions to appropriate HTTP status codes
    /// </summary>
    /// <param name="customExceptionHandler">Optional handler for custom exceptions; called before standard handlers. Return null to fall through to standard handlers.</param>
    protected async Task<IActionResult> ExecuteAiActionAsync(
        string providerKey,
        string? modelKey,
        CancellationToken ct,
        Func<Guid, Task<IActionResult>> action,
        Func<Exception, IActionResult?>? customExceptionHandler = null)
    {
        var userId = await ResolveUserIdAsync(_db, ct);
        if (userId == null)
        {
            _logger.LogWarning("[BaseAiController] Failed to resolve user ID from JWT");
            return Unauthorized(new { Message = "Usuário não autenticado." });
        }

        if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(modelKey))
        {
            _logger.LogWarning("[BaseAiController] Missing provider/model in AI request for user {UserId}", userId.Value);
            return BadRequest(new { Message = "Provider e modelo sÃ£o obrigatÃ³rios." });
        }

        var hasAccess = await _catalogService.ValidateUserAccessAsync(userId.Value, providerKey, modelKey, ct);
        if (!hasAccess)
        {
            _logger.LogWarning("[BaseAiController] User {UserId} denied access to {Provider}/{Model}",
                userId.Value, providerKey, modelKey);
            return StatusCode(403, new { Message = "Acesso negado ao provider/modelo." });
        }

        try
        {
            return await action(userId.Value);
        }
        catch (Exception ex)
        {
            // Try custom handler first (if provided)
            var customResult = customExceptionHandler?.Invoke(ex);
            if (customResult != null) return customResult;

            // Standard handlers
            if (ex is ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "[BaseAiController] ArgumentException: {Message}", argEx.Message);
                return BadRequest(new { message = argEx.Message, errorCode = AiErrorCategory.InvalidRequest });
            }

            if (ex is AiProviderException aiEx)
            {
                _logger.LogError(aiEx, "[BaseAiController] AiProviderException [{ErrorCode}]: {Message}", aiEx.ErrorCode, aiEx.Message);
                return StatusCode(502, new { message = aiEx.Message, errorCode = aiEx.ErrorCode });
            }

            if (ex is InvalidOperationException invOpEx)
            {
                _logger.LogError(invOpEx, "[BaseAiController] InvalidOperationException: {Message}", invOpEx.Message);
                return StatusCode(502, new { message = invOpEx.Message, errorCode = AiErrorCategory.Unknown });
            }

            _logger.LogError(ex, "[BaseAiController] Unexpected error executing AI action");
            return StatusCode(500, new { message = "Erro inesperado ao executar ação de IA.", errorCode = AiErrorCategory.Unknown });
        }
    }
}
