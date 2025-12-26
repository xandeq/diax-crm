param(
    [Parameter(Mandatory=$true)]
    [string]$Name,

    [string]$OutputDir = "Data\\Migrations"
)

$ErrorActionPreference = 'Stop'

Set-Location (Split-Path $PSScriptRoot -Parent)

dotnet ef migrations add $Name `
  --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" `
  --startup-project "src\Diax.Api\Diax.Api.csproj" `
  --context Diax.Infrastructure.Data.DiaxDbContext `
  --output-dir $OutputDir
