<#
.SYNOPSIS
    Applies EF Core migrations to the PRODUCTION database (SmarterASP).

.EXAMPLE
    .\scripts\update-db.ps1
#>

$ErrorActionPreference = 'Stop'

Set-Location (Split-Path $PSScriptRoot -Parent)
$env:ASPNETCORE_ENVIRONMENT = "Production"

Write-Host "Loading connection string from .NET User Secrets..." -ForegroundColor Cyan

$secretLine = dotnet user-secrets list --project src/Diax.Api |
    Where-Object { $_ -like "ConnectionStrings:DefaultConnection*" } |
    Select-Object -First 1

if (-not $secretLine) {
    Write-Host "ERROR: Connection string not found in User Secrets." -ForegroundColor Red
    Write-Host "Run scripts\set-local-db-secret.ps1 first." -ForegroundColor Yellow
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    exit 1
}

$connString = $secretLine.Substring($secretLine.IndexOf('=') + 1).Trim()

if ($connString.StartsWith('"') -and $connString.EndsWith('"')) {
    $connString = $connString.Trim('"')
}

if ($connString -notmatch ';') {
    Write-Host "ERROR: Invalid connection string format in User Secrets." -ForegroundColor Red
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "EF CORE MIGRATIONS - PRODUCTION (SmarterASP)" -ForegroundColor Red
Write-Host "Environment: Production" -ForegroundColor Yellow
Write-Host "Server: sql1002.site4now.net" -ForegroundColor Yellow
Write-Host ""

dotnet ef database update `
    --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" `
    --startup-project "src\Diax.Api\Diax.Api.csproj" `
    --context Diax.Infrastructure.Data.DiaxDbContext `
    --connection "$connString"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Error applying migrations." -ForegroundColor Red
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "Migrations applied successfully in PRODUCTION." -ForegroundColor Green

Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
