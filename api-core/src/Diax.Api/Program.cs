// Force deployment to apply pending database migrations (AddVideoProviderLimits)
using Asp.Versioning;
using Microsoft.AspNetCore.DataProtection;
using Diax.Api.Auth;
using Diax.Api.Configuration;
using Diax.Api.Middleware;
using Diax.Application;
using Diax.Application.PromptGenerator;
using Diax.Infrastructure;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Seed;
using Diax.Infrastructure.Data.Seeders;
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
promptGeneratorSettings.Anthropic.ApiKey ??= builder.Configuration["ANTHROPIC_API_KEY"]
    ?? builder.Configuration["Anthropic:ApiKey"];

// Video generation providers (fallback when DB credential decryption fails)
promptGeneratorSettings.Runway.ApiKey ??= builder.Configuration["RUNWAY_API_KEY"]
    ?? builder.Configuration["Runway:ApiKey"];
promptGeneratorSettings.Replicate.ApiKey ??= builder.Configuration["REPLICATE_API_KEY"]
    ?? builder.Configuration["Replicate:ApiKey"];
promptGeneratorSettings.Shotstack.ApiKey ??= builder.Configuration["SHOTSTACK_API_KEY"]
    ?? builder.Configuration["Shotstack:ApiKey"];

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

// ===== AUTENTICAÇÃO: JWT + API Key estática (para n8n e outros clientes M2M) =====
// PolicyScheme roteia para o scheme correto com base nos headers presentes:
//   - Header "X-Api-Key" presente  → ApiKeyAuthenticationHandler (chave estática, nunca expira)
//   - Caso contrário              → JwtBearer (token de sessão do usuário)
const string multiAuthScheme = "JwtOrApiKey";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = multiAuthScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // 401 com WWW-Authenticate: Bearer
    })
    .AddPolicyScheme(multiAuthScheme, "JWT or API Key", policyOptions =>
    {
        policyOptions.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.ContainsKey("X-Api-Key")
                ? ApiKeyAuthenticationOptions.DefaultScheme
                : JwtBearerDefaults.AuthenticationScheme;
    })
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
        // Support ?token= query param for file download endpoints (anchor tags can't set headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, _ => { });

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
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"), tags: new[] { "live", "ready" });

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
    var migrationTask = Task.Run(async () =>
    {
        try
        {
            // Keep schema updated (no-op if already up to date)
            db.Database.Migrate();
            Log.Information("Database migrations applied successfully.");

            await EnsurePersonalFinanceSchemaAsync(db);
            Log.Information("Personal finance schema hotfix verified.");

            // Seed initial admin (idempotent) — usa app.Configuration (após Build)
            UserSeeder.SeedInitialAdmin(db, app.Configuration, seedLogger);
            Log.Information("UserSeeder completed.");

            // Seed AI Providers
            AiDataSeeder.SeedAiProviders(db, seedLogger);
            Log.Information("AiDataSeeder completed.");

            // Seed Video Providers
            await VideoProviderSeeder.SeedVideoProvidersAsync(db);
            Log.Information("VideoProviderSeeder completed.");

            // Approve video providers for admin group
            await AdminGroupVideoAccessSeeder.ApproveVideoProvidersForAdminAsync(db, seedLogger);
            Log.Information("AdminGroupVideoAccessSeeder completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration/seed failed inside task.");
            throw;
        }
    });

    // Wait max 180 seconds for migration (increased from 45s to handle large schema changes on remote DB)
    await migrationTask.WaitAsync(TimeSpan.FromSeconds(180));
    Log.Information("Database initialization completed successfully.");
}
catch (TimeoutException)
{
    Log.Warning("Database migration timed out after 180 seconds. App will start but DB may not be ready.");
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
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString());
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

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
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

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

static async Task EnsurePersonalFinanceSchemaAsync(DiaxDbContext db)
{
    const string sql = """
        IF COL_LENGTH('transactions', 'details') IS NULL
            ALTER TABLE [transactions] ADD [details] nvarchar(1000) NULL;

        IF COL_LENGTH('transactions', 'is_subscription') IS NULL
            ALTER TABLE [transactions] ADD [is_subscription] bit NOT NULL
                CONSTRAINT [DF_transactions_is_subscription] DEFAULT(0);

        IF COL_LENGTH('transactions', 'recurring_transaction_id') IS NULL
            ALTER TABLE [transactions] ADD [recurring_transaction_id] uniqueidentifier NULL;

        IF COL_LENGTH('recurring_transactions', 'details') IS NULL
            ALTER TABLE [recurring_transactions] ADD [details] nvarchar(max) NULL;

        IF COL_LENGTH('recurring_transactions', 'item_kind') IS NULL
            ALTER TABLE [recurring_transactions] ADD [item_kind] int NOT NULL
                CONSTRAINT [DF_recurring_transactions_item_kind] DEFAULT(1);

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_transactions_recurring_transaction_id'
              AND object_id = OBJECT_ID(N'[transactions]'))
        BEGIN
            CREATE INDEX [IX_transactions_recurring_transaction_id]
                ON [transactions] ([recurring_transaction_id]);
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = 'FK_transactions_recurring_transactions_recurring_transaction_id')
        BEGIN
            ALTER TABLE [transactions]
            ADD CONSTRAINT [FK_transactions_recurring_transactions_recurring_transaction_id]
                FOREIGN KEY ([recurring_transaction_id])
                REFERENCES [recurring_transactions] ([id])
                ON DELETE SET NULL;
        END;
        """;

    await db.Database.ExecuteSqlRawAsync(sql);
}
