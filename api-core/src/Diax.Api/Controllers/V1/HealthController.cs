using Asp.Versioning;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

/// <summary>
/// Controller para verificação de saúde da API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly DiaxDbContext _db;

    public HealthController(ILogger<HealthController> logger, DiaxDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Verifica se a API está online e funcionando.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check executado em {Time}", DateTime.UtcNow);

        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Verifica e corrige colunas ausentes no schema (patch idempotente).
    /// </summary>
    [HttpPost("schema-patch")]
    public async Task<IActionResult> SchemaPatch()
    {
        var results = new List<string>();

        var columns = new[]
        {
            ("credit_card_invoices", "statement_amount", "decimal(18,2) NULL"),
            ("ai_usage_logs",        "error_category",   "nvarchar(256) NULL"),
            ("ai_usage_logs",        "http_status_code", "int NULL"),
            ("ai_models",            "last_failure_at",  "datetime2 NULL"),
            ("ai_models",            "last_failure_category", "nvarchar(256) NULL"),
            ("ai_models",            "last_failure_message",  "nvarchar(256) NULL"),
            ("ai_models",            "last_success_at",  "datetime2 NULL"),
        };

        foreach (var (table, col, colDef) in columns)
        {
            try
            {
                var sql = $"""
                    IF COL_LENGTH('{table}', '{col}') IS NULL
                        ALTER TABLE [{table}] ADD [{col}] {colDef};
                    """;
                await _db.Database.ExecuteSqlRawAsync(sql);
                results.Add($"OK: {table}.{col}");
            }
            catch (Exception ex)
            {
                results.Add($"ERR: {table}.{col} → {ex.Message}");
            }
        }

        // consecutive_failure_count is NOT NULL so needs a DEFAULT
        try
        {
            await _db.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('ai_models', 'consecutive_failure_count') IS NULL
                    ALTER TABLE [ai_models] ADD [consecutive_failure_count] int NOT NULL
                        CONSTRAINT [DF_ai_models_cfc] DEFAULT(0);
                """);
            results.Add("OK: ai_models.consecutive_failure_count");
        }
        catch (Exception ex)
        {
            results.Add($"ERR: ai_models.consecutive_failure_count → {ex.Message}");
        }

        // Verify statement_amount now exists
        try
        {
            var check = await _db.Database.SqlQueryRaw<int>(
                "SELECT CASE WHEN COL_LENGTH('credit_card_invoices','statement_amount') IS NOT NULL THEN 1 ELSE 0 END AS [Value]"
            ).FirstOrDefaultAsync();
            results.Add($"VERIFY statement_amount exists: {(check == 1 ? "YES" : "NO")}");
        }
        catch (Exception ex)
        {
            results.Add($"VERIFY_ERR: {ex.Message}");
        }

        return Ok(results);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}
