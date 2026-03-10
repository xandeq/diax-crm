using Asp.Versioning;
using Microsoft.AspNetCore.DataProtection;
using Diax.Api.Configuration;
using Diax.Api.Middleware;
using Diax.Application;
using Diax.Application.PromptGenerator;
using Diax.Infrastructure;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO =====
// User Secrets é carregado automaticamente em Development
// Variáveis de ambiente são carregadas em todos os ambientes
// Prioridade: Env Vars > AWS Secrets Manager > User Secrets > appsettings.{Environment}.json > appsettings.json

// Adiciona variáveis de ambiente com prefixo DIAX_ (opcional, para produção)
builder.Configuration.AddEnvironmentVariables(prefix: "DIAX_");

// AWS Secrets Manager — carrega secrets do AWS em produção (path: /diax-crm/)
// Optional=true: não derruba o startup se o AWS estiver inacessível
builder.Configuration.AddAwsSecretsManager(builder.Environment);

// ===== SERILOG =====
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DiaxCRM.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ===== SERVICES =====

// Adiciona camadas da aplicação
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// HttpClient factory (used for external AI providers)
builder.Services.AddHttpClient();

// Data Protection API (for encrypting API keys)
// Keys are persisted to App_Data/DataProtection-Keys so they survive app restarts and deploys.
// Without persistence, every restart generates new keys and all DB-encrypted values become undecryptable.
var keyRingPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keyRingPath))
    .SetApplicationName("DiaxCRM");

// Prompt Generator settings (API keys from env vars or config)
var promptGeneratorSettings = new PromptGeneratorSettings();
builder.Configuration.GetSection("PromptGenerator").Bind(promptGeneratorSettings);

promptGeneratorSettings.OpenAI.ApiKey ??= builder.Configuration["OPENAI_API_KEY"]
    ?? builder.Configuration["OpenAI:ApiKey"];
promptGeneratorSettings.Perplexity.ApiKey ??= builder.Configuration["PERPLEXITY_API_KEY"]
    ?? builder.Configuration["Perplexity:ApiKey"];
promptGeneratorSettings.DeepSeek.ApiKey ??= builder.Configuration["DEEPSEEK_API_KEY"]
    ?? builder.Configuration["DeepSeek:ApiKey"];
promptGeneratorSettings.Gemini.ApiKey ??= builder.Configuration["GEMINI_API_KEY"]
    ?? builder.Configuration["Gemini:ApiKey"];
promptGeneratorSettings.OpenRouter.ApiKey ??= builder.Configuration["OPENROUTER_API_KEY"]
    ?? builder.Configuration["OpenRouter:ApiKey"];
promptGeneratorSettings.FAL.ApiKey ??= builder.Configuration["FAL_API_KEY"]
    ?? builder.Configuration["FAL:ApiKey"];
promptGeneratorSettings.Grok.ApiKey ??= builder.Configuration["XAI_API_KEY"]
    ?? builder.Configuration["Grok:ApiKey"];
promptGeneratorSettings.HuggingFace.ApiKey ??= builder.Configuration["HF_API_KEY"]
    ?? builder.Configuration["HUGGINGFACE_API_KEY"]
    ?? builder.Configuration["HuggingFace:ApiKey"];
promptGeneratorSettings.Groq.ApiKey ??= builder.Configuration["GROQ_API_KEY"]
    ?? builder.Configuration["Groq:ApiKey"];
promptGeneratorSettings.Cerebras.ApiKey ??= builder.Configuration["CEREBRAS_API_KEY"]
    ?? builder.Configuration["Cerebras:ApiKey"];

builder.Services.AddSingleton(promptGeneratorSettings);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Permite deserialização case-insensitive
    });

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var configured = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var allowedOrigins = (configured is { Length: > 0 })
            ? configured.Select(o => o.Trim().TrimEnd('/')).ToList()
            : new List<string>
            {
                "https://crm.alexandrequeiroz.com.br",
                "http://localhost:3000"
            };

        // Garantir que as origens de produção estejam sempre permitidas
        var productionOrigins = new[]
        {
            "https://crm.alexandrequeiroz.com.br",
            "https://alexandrequeiroz.com.br"
        };

        foreach (var origin in productionOrigins)
        {
            if (!allowedOrigins.Contains(origin))
            {
                allowedOrigins.Add(origin);
            }
        }

        Log.Information("CORS configured with allowed origins: {Origins}", string.Join(", ", allowedOrigins));

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT Auth
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DiaxCRM";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DiaxCRM";
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? builder.Configuration["Jwt:Secret"]
    ?? builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "dev-only-insecure-key-change-me-please-32chars";
        Log.Warning("JWT key not configured. Using development fallback key.");
    }
    else
    {
        throw new InvalidOperationException("JWT key not configured. Set Jwt:Key (recommended) or Jwt:Secret via DIAX_Jwt__Key / DIAX_Jwt__Secret.");
    }
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting — proteção contra brute force no login
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== PATH BASE (IIS virtual directory support) =====
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    if (!pathBase.StartsWith("/"))
    {
        pathBase = "/" + pathBase;
    }

    app.UsePathBase(pathBase);
}

// ===== DB MIGRATIONS + SEED (admin) =====
try
{
    Log.Information("Starting database migrations and seeding...");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserSeeder");

    // Wrap migration in timeout to prevent ANCM startup timeout (120s default)
    var migrationTask = Task.Run(() =>
    {
        try
        {
            // Keep schema updated (no-op if already up to date)
            db.Database.Migrate();
            Log.Information("Database migrations applied successfully.");

            // Seed initial admin (idempotent) — usa app.Configuration (após Build)
            UserSeeder.SeedInitialAdmin(db, app.Configuration, seedLogger);
            Log.Information("UserSeeder completed.");

            // Seed AI Providers
            AiDataSeeder.SeedAiProviders(db, seedLogger);
            Log.Information("AiDataSeeder completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration/seed failed inside task.");
            throw;
        }
    });

    // Wait max 45 seconds for migration (leaves 75s buffer for ANCM timeout)
    await migrationTask.WaitAsync(TimeSpan.FromSeconds(45));
    Log.Information("Database initialization completed successfully.");
}
catch (TimeoutException)
{
    Log.Warning("Database migration timed out after 45 seconds. App will start but DB may not be ready.");
    // Allow app to start - health checks will report DB status
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to migrate/seed database on startup. App will start but DB may not be ready.");
    // Em produção, preferimos manter a API no ar e deixar o pipeline cuidar das migrations.

    // Write detailed error to file for FTP diagnostics
    try
    {
        var errorPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "startup-error.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
        await File.WriteAllTextAsync(errorPath,
            $"Startup Error at {DateTime.UtcNow:O}\n\n" +
            $"Message: {ex.Message}\n\n" +
            $"Type: {ex.GetType().FullName}\n\n" +
            $"Stack:\n{ex.StackTrace}\n\n" +
            $"Inner: {ex.InnerException?.Message}");
        Log.Information("Wrote startup error details to {ErrorPath}", errorPath);
    }
    catch { /* Best effort - don't fail startup for logging */ }
}

Log.Information("Configuring middleware pipeline...");

// ===== MIDDLEWARE PIPELINE =====

// CORS - DEVE ser o primeiro middleware para garantir headers em todas as respostas (incluindo erros)
app.UseCors("Frontend");

// Middleware para garantir CORS correto e evitar headers duplicados
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // Tenta remover headers duplicados ou wildcard antes de enviar a resposta
        if (context.Response.Headers.TryGetValue("Access-Control-Allow-Origin", out var origins))
        {
             if (origins.Count > 1 || (origins.Count == 1 && origins[0] == "*"))
            {
                context.Response.Headers.Remove("Access-Control-Allow-Origin");
            }
        }
        return Task.CompletedTask;
    });

    await next();
});

// Correlation ID - adiciona ID de correlação em todas as requisições
app.UseCorrelationId();

// Exception Logging - captura exceções e grava no banco de dados
app.UseExceptionLogging();

// Request/Response Logging - registra requisições com erro (4xx/5xx) na tabela app_logs
app.UseRequestResponseLogging();

// Rate Limiting — deve vir antes de auth/controllers
app.UseRateLimiter();

// Swagger — restrito a ambientes não-produção
if (!app.Environment.IsProduction())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            var serverUrl = $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}";
            swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
            {
                new() { Url = serverUrl }
            };
        });
    });
    app.UseSwaggerUI(c =>
    {
        var swaggerJson = string.IsNullOrWhiteSpace(pathBase)
            ? "v1/swagger.json"
            : $"{pathBase}/swagger/v1/swagger.json";

        c.SwaggerEndpoint(swaggerJson, "DIAX CRM API v1");
        c.RoutePrefix = "swagger";
    });
}

// Serilog request logging
app.UseSerilogRequestLogging();

// Static Files - serve imagens de email da pasta wwwroot/email-images
app.UseStaticFiles();

// HTTPS Redirection
app.UseHttpsRedirection();

// Auth (preparado para JWT futuro)
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health Check endpoint
app.MapHealthChecks("/health");

// Redirects para Swagger — apenas em não-produção
if (!app.Environment.IsProduction())
{
    app.MapGet("/v1/swagger", () => Results.Redirect("/swagger/index.html"));
    app.MapGet("/v1/swagger/index.html", () => Results.Redirect("/swagger/index.html"));
    app.MapGet("/v1/swagger/v1/swagger.json", () => Results.Redirect("/swagger/v1/swagger.json"));
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}
else
{
    app.MapGet("/", () => Results.Ok(new { status = "ok", version = "1.0" }));
}

// ===== RUN =====
try
{
    Log.Information("Iniciando DIAX CRM API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
