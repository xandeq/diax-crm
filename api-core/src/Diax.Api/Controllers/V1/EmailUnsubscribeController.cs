using Asp.Versioning;
using Diax.Application.EmailMarketing;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Anonymous endpoint: GET /unsubscribe?token=&lt;hmac&gt;&amp;uid=&lt;userId&gt;&amp;email=&lt;email&gt;
/// Token = Base64Url(HMAC-SHA256(EmailLinks:SigningKey, "unsub:{userId}:{email}")) — gerado e
/// validado pelo IUnsubscribeLinkBuilder (fonte única de chave e algoritmo).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("unsubscribe")]
public class EmailUnsubscribeController : BaseApiController
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUnsubscribeLinkBuilder _linkBuilder;
    private readonly ILogger<EmailUnsubscribeController> _logger;

    public EmailUnsubscribeController(
        ICustomerRepository customerRepository,
        IEmailSuppressionRepository suppressionRepository,
        IUnitOfWork unitOfWork,
        IUnsubscribeLinkBuilder linkBuilder,
        ILogger<EmailUnsubscribeController> logger)
    {
        _customerRepository = customerRepository;
        _suppressionRepository = suppressionRepository;
        _unitOfWork = unitOfWork;
        _linkBuilder = linkBuilder;
        _logger = logger;
    }

    [HttpGet("")]
    [AllowAnonymous]
    [ApiVersionNeutral]
    public async Task<IActionResult> Unsubscribe(
        [FromQuery] string token,
        [FromQuery] string uid,
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var failure = await ApplyUnsubscribeAsync(token, uid, email, cancellationToken);
        if (failure != null) return failure;

        return Content(
            "<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
            "<h2>Descadastramento realizado</h2>" +
            "<p>Seu email foi removido da lista com sucesso.</p>" +
            "</body></html>",
            "text/html");
    }

    /// <summary>
    /// One-click (RFC 8058): Gmail/Yahoo fazem POST na URL do header List-Unsubscribe com o corpo
    /// <c>List-Unsubscribe=One-Click</c>. Mesmo token HMAC do GET; responde 200 sem HTML.
    /// </summary>
    [HttpPost("")]
    [AllowAnonymous]
    [ApiVersionNeutral]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public async Task<IActionResult> UnsubscribeOneClick(
        [FromQuery] string token,
        [FromQuery] string uid,
        [FromQuery] string email,
        [FromForm(Name = "List-Unsubscribe")] string? oneClick,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(oneClick, "One-Click", StringComparison.Ordinal))
            return BadRequest("Requisicao one-click invalida.");

        return await ApplyUnsubscribeAsync(token, uid, email, cancellationToken) ?? Ok();
    }

    /// <summary>Valida o token e aplica o opt-out. Retorna null em sucesso, ou o resultado HTTP de falha.</summary>
    private async Task<IActionResult?> ApplyUnsubscribeAsync(
        string token, string uid, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Link de descadastro invalido.");

        var userId = uid;
        if (!_linkBuilder.IsValidToken(userId, email, token))
        {
            _logger.LogWarning("Invalid unsubscribe token for email={Email}", email);
            return Unauthorized();
        }

        // Todos os cadastros com esse email (duplicados incluídos): senão o duplicado segue recebendo
        foreach (var customer in await _customerRepository.GetAllByEmailAsync(email, cancellationToken))
        {
            if (customer.EmailOptOut) continue;
            customer.OptOutEmail();
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }

        if (Guid.TryParse(userId, out var userGuid))
        {
            var existing = await _suppressionRepository.FindByEmailAsync(userGuid, email, cancellationToken);
            if (existing == null)
            {
                await _suppressionRepository.AddAsync(
                    EmailSuppression.ForEmail(userGuid, email, SuppressionReason.ManualOptOut, "unsubscribe_link"),
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unsubscribed email={Email}", email);
        return null;
    }
}
