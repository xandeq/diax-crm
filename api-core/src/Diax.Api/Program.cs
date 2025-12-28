using Asp.Versioning;
using Diax.Api.Configuration;
using Diax.Application;
using Diax.Infrastructure;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO =====
// User Secrets é carregado automaticamente em Development
// Variáveis de ambiente são carregadas em todos os ambientes
// Prioridade: Env Vars > User Secrets > appsettings.{Environment}.json > appsettings.json

// Adiciona variáveis de ambiente com prefixo DIAX_ (opcional, para produção)
builder.Configuration.AddEnvironmentVariables(prefix: "DIAX_");

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

// Controllers
builder.Services.AddControllers();

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
            ? configured
            : new[]
            {
                "https://crm.alexandrequeiroz.com.br",
                "http://localhost:3000"
            };

        policy.WithOrigins(allowedOrigins)
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

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== DB MIGRATIONS + SEED (admin) =====
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();

    // Keep schema updated (no-op if already up to date)
    db.Database.Migrate();

    // Seed initial admin (idempotent)
    AdminUserSeeder.SeedInitialAdmin(db, builder.Configuration);
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to migrate/seed database on startup.");
    if (!builder.Environment.IsDevelopment())
        throw;
}

// ===== MIDDLEWARE PIPELINE =====

// Swagger (disponível em todos os ambientes por enquanto)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DIAX CRM API v1");
    c.RoutePrefix = "swagger";
});

// Serilog request logging
app.UseSerilogRequestLogging();

// CORS
app.UseCors("Frontend");

// HTTPS Redirection
app.UseHttpsRedirection();

// Auth (preparado para JWT futuro)
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health Check endpoint
app.MapHealthChecks("/health");

// Root redirect para Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

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
