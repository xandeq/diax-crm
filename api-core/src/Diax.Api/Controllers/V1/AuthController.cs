using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var adminEmail = _configuration["Auth:AdminEmail"];
        var adminPassword = _configuration["Auth:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return Unauthorized(new { message = "Auth is not configured." });

        if (!string.Equals(request.Email?.Trim(), adminEmail, StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = "Invalid credentials." });

        if (!string.Equals(request.Password, adminPassword, StringComparison.Ordinal))
            return Unauthorized(new { message = "Invalid credentials." });

        var issuer = _configuration["Jwt:Issuer"] ?? "DiaxCRM";
        var audience = _configuration["Jwt:Audience"] ?? "DiaxCRM";
        var key = _configuration["Jwt:Key"]
            ?? _configuration["Jwt:Secret"]
            ?? _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(key))
            return Unauthorized(new { message = "JWT key not configured." });

        var expiresMinutes = int.TryParse(_configuration["Jwt:ExpirationInMinutes"], out var m) ? m : 60;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminEmail),
            new(JwtRegisteredClaimNames.Email, adminEmail),
            new(ClaimTypes.Email, adminEmail),
            new(ClaimTypes.Role, "admin")
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(accessToken, expiresAtUtc));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

        return Ok(new MeResponse(email, roles));
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
    public record MeResponse(string Email, string[] Roles);
}
