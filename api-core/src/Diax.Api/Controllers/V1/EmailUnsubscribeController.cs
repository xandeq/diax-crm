using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Anonymous endpoint: GET /unsubscribe?token=&lt;hmac_token&gt;
/// Token = Base64Url(HMAC-SHA256(jwtKey, "unsub:{userId}:{email}"))
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("unsubscribe")]
public class EmailUnsubscribeController : BaseApiController
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailUnsubscribeController> _logger;

    public EmailUnsubscribeController(
        ICustomerRepository customerRepository,
        IEmailSuppressionRepository suppressionRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<EmailUnsubscribeController> logger)
    {
        _customerRepository = customerRepository;
        _suppressionRepository = suppressionRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    [AllowAnonymous]
    [ApiVersionNeutral]
    public async Task<IActionResult> Unsubscribe(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token invalido.");

        string? userId = null;
        string? email = null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(
                token.Replace('-', '+').Replace('_', '/')));

            var parts = decoded.Split(':');
            if (parts.Length < 3 || parts[0] != "unsub")
                return BadRequest("Token invalido.");

            userId = parts[1];
            email = string.Join(":", parts[2..]);

            var jwtKey = _configuration["Jwt:Key"] ?? string.Empty;
            var expected = ComputeToken(jwtKey, userId, email);

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(token)))
            {
                _logger.LogWarning("Invalid unsubscribe token for email={Email}", email);
                return Unauthorized();
            }
        }
        catch
        {
            return BadRequest("Token invalido.");
        }

        var customer = await _customerRepository.GetByEmailAsync(email, cancellationToken);
        if (customer != null && !customer.EmailOptOut)
        {
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

        return Content(
            "<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
            "<h2>Descadastramento realizado</h2>" +
            "<p>Seu email foi removido da lista com sucesso.</p>" +
            "</body></html>",
            "text/html");
    }

    public static string ComputeToken(string key, string userId, string email)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var payload = Encoding.UTF8.GetBytes($"unsub:{userId}:{email}");
        var hash = hmac.ComputeHash(payload);
        return Convert.ToBase64String(hash)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
