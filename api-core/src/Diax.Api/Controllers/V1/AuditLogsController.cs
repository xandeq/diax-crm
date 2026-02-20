using Asp.Versioning;
using Diax.Application.Audit;
using Diax.Application.Audit.Dtos;
using Diax.Application.Auth;
using Diax.Domain.Audit;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Endpoint administrativo para consulta de logs de auditoria.
/// Acesso restrito a usuários com grupo system-admin.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Produces("application/json")]
public class AuditLogsController : BaseApiController
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===== Helpers =====

    private async Task<bool> IsAdminAsync(CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (userId is null) return false;
        return await _permissionService.IsAdminAsync(userId.Value, ct);
    }

    // ===== Endpoints =====

    /// <summary>
    /// Lista logs de auditoria com filtros e paginação. Admin only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPagedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] Guid? userId,
        [FromQuery] string? resourceType,
        [FromQuery] string? resourceId,
        [FromQuery] AuditAction? action,
        [FromQuery] AuditSource? source,
        [FromQuery] AuditStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? searchText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await IsAdminAsync(ct))
            return Forbid();

        var request = new AuditLogFilterRequest(
            userId, resourceType, resourceId, action, source, status,
            fromDate, toDate, searchText, page, pageSize);

        var result = await _auditLogService.GetFilteredAsync(request, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtém um log de auditoria por ID. Admin only.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!await IsAdminAsync(ct))
            return Forbid();

        var result = await _auditLogService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Retorna o histórico completo de um recurso específico. Admin only.
    /// Ex: GET /api/v1/audit-logs/resource/Customer/3fa85f64...
    /// </summary>
    [HttpGet("resource/{resourceType}/{resourceId}")]
    [ProducesResponseType(typeof(List<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResourceHistory(
        string resourceType,
        string resourceId,
        CancellationToken ct)
    {
        if (!await IsAdminAsync(ct))
            return Forbid();

        var result = await _auditLogService.GetResourceHistoryAsync(resourceType, resourceId, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Retorna a atividade de um usuário específico.
    /// Admin pode consultar qualquer usuário; usuário comum só pode consultar a si próprio.
    /// </summary>
    [HttpGet("user/{targetUserId:guid}")]
    [ProducesResponseType(typeof(List<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserActivity(
        Guid targetUserId,
        [FromQuery] int? limit = null,
        CancellationToken ct = default)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId is null)
            return Unauthorized();

        // Verifica se é admin ou o próprio usuário
        var isAdmin = await _permissionService.IsAdminAsync(currentUserId.Value, ct);
        if (!isAdmin && currentUserId.Value != targetUserId)
            return Forbid();

        var result = await _auditLogService.GetUserActivityAsync(targetUserId, limit, ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove logs de auditoria mais antigos que o período especificado. Admin only.
    /// Mínimo de 30 dias de retenção obrigatório.
    /// </summary>
    [HttpDelete("cleanup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cleanup(
        [FromQuery] int olderThanDays = 90,
        CancellationToken ct = default)
    {
        if (!await IsAdminAsync(ct))
            return Forbid();

        _logger.LogWarning(
            "Cleanup de audit logs solicitado por {UserId}: logs anteriores a {Days} dias serão removidos",
            _currentUserService.UserId, olderThanDays);

        var result = await _auditLogService.CleanupOldLogsAsync(olderThanDays, ct);

        if (!result.IsSuccess)
            return HandleResult(result);

        return Ok(new
        {
            deletedCount = result.Value,
            message = $"{result.Value} logs removidos com sucesso (anteriores a {olderThanDays} dias)"
        });
    }
}
