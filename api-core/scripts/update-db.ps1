$ErrorActionPreference = 'Stop'

Set-Location (Split-Path $PSScriptRoot -Parent)

# Evita que uma env var com placeholder sobrescreva o User Secrets.
# (CreateDefaultBuilder dá prioridade para env vars > user-secrets.)
if ($env:ConnectionStrings__DefaultConnection -and $env:ConnectionStrings__DefaultConnection -match 'SEU_') {
  Write-Host "Ignoring session env var ConnectionStrings__DefaultConnection (placeholder detected)." -ForegroundColor Yellow
  Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
}

dotnet ef database update `
  --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" `
  --startup-project "src\Diax.Api\Diax.Api.csproj" `
  --context Diax.Infrastructure.Data.DiaxDbContext
