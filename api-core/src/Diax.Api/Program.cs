using Asp.Versioning;
using Diax.Api.Configuration;
using Diax.Application;
using Diax.Infrastructure;
using Serilog;

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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

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
app.UseCors("AllowAll");

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
