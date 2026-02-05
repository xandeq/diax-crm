using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Diax.Infrastructure.Data;
using Diax.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Diax.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DiaxDbContext _db;

    public AuthController(IConfiguration configuration, DiaxDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var email = request.Email.Trim();

        // Prefer DB-backed admin users
        var hasDbAdmins = await _db.AdminUsers.AnyAsync();
        if (hasDbAdmins)
        {
            var admin = await _db.AdminUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Email == email);

            if (admin is null || !admin.IsActive)
                return Unauthorized(new { message = "Invalid credentials." });

            if (!PasswordHash.Verify(admin.PasswordHash, request.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            return Ok(CreateTokenResponse(admin.Email, admin.Role.ToString(), admin.Id));
        }

        // Fallback (temporary): allow config-based admin while DB is empty
        var adminEmail = _configuration["Auth:AdminEmail"];
        var adminPassword = _configuration["Auth:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return Unauthorized(new { message = "Auth is not configured." });

        if (!string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = "Invalid credentials." });

        if (!string.Equals(request.Password, adminPassword, StringComparison.Ordinal))
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(CreateTokenResponse(adminEmail, "Admin", null));
    }

    private LoginResponse CreateTokenResponse(string adminEmail, string role, Guid? userId)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "DiaxCRM";
        var audience = _configuration["Jwt:Audience"] ?? "DiaxCRM";
        var key = _configuration["Jwt:Key"]
            ?? _configuration["Jwt:Secret"]
            ?? _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT key not configured.");

        var expiresMinutes = int.TryParse(_configuration["Jwt:ExpirationInMinutes"], out var m) ? m : 60;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminEmail),
            new(JwtRegisteredClaimNames.Email, adminEmail),
            new(ClaimTypes.Email, adminEmail),
            new(ClaimTypes.Role, role)
        };

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            claims.Add(new Claim("id", userId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(accessToken, expiresAtUtc);
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

    public record LoginResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("expiresAtUtc")] DateTime ExpiresAtUtc)
    {
        // Backward-compat alias for clients expecting "token"
        [JsonPropertyName("token")]
        public string Token => AccessToken;
    }

    public record MeResponse(string Email, string[] Roles);
}
