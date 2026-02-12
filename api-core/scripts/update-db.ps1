<#
.SYNOPSIS
    Aplica EF Core migrations no banco de producao (SmarterASP).

.DESCRIPTION
    Este script SEMPRE executa contra o banco de PRODUCAO.
    Nunca contra LocalDB ou qualquer outro ambiente.

    Banco: sql1002.site4now.net
    Connection string: .NET User Secrets (Diax.Api)

.EXAMPLE
    .\scripts\update-db.ps1
#>

$ErrorActionPreference = 'Stop'

Set-Location (Split-Path $PSScriptRoot -Parent)
$env:ASPNETCORE_ENVIRONMENT = 'Production'

Write-Host 'Buscando credenciais em .NET User Secrets...' -ForegroundColor Cyan
$connString = dotnet user-secrets list --project src/Diax.Api |
    Where-Object { $_ -match '^ConnectionStrings:DefaultConnection\s*=' } |
    Select-Object -First 1 |
    ForEach-Object { $_ -replace '^ConnectionStrings:DefaultConnection\s*=\s*', '' }

if (-not $connString) {
    Write-Host 'ERRO: Connection string nao encontrada nos User Secrets.' -ForegroundColor Red
    Write-Host 'Execute scripts\set-local-db-secret.ps1 primeiro.' -ForegroundColor Yellow
    exit 1
}

Write-Host ''
Write-Host 'EF CORE MIGRATIONS - PRODUCAO (SmarterASP)' -ForegroundColor Yellow
Write-Host 'Ambiente: Production' -ForegroundColor Yellow
Write-Host 'Servidor: sql1002.site4now.net' -ForegroundColor Yellow

dotnet ef database update `
    --project 'src\Diax.Infrastructure\Diax.Infrastructure.csproj' `
    --startup-project 'src\Diax.Api\Diax.Api.csproj' `
    --context Diax.Infrastructure.Data.DiaxDbContext `
    --connection "$connString" `
    --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host ''
    Write-Host 'Migrations aplicadas com sucesso em PRODUCAO.' -ForegroundColor Green
}
else {
    Write-Host ''
    Write-Host 'Erro ao aplicar migrations.' -ForegroundColor Red
    exit 1
}

Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
