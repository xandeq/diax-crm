<#
.SYNOPSIS
    Aplica EF Core Migrations no banco de dados de PRODUÇÃO (SmarterASP).

.DESCRIPTION
    ⚠️  Este script SEMPRE executa contra o banco de PRODUÇÃO.
    Nunca contra LocalDB ou qualquer outro ambiente.

    Banco de Dados: sql1002.site4now.net (SmarterASP)
    Connection String: appsettings.Production.json (local, não-commitado)

.EXAMPLE
    .\scripts\update-db.ps1
#>

$ErrorActionPreference = 'Stop'

# Entrar na raiz do projeto (api-core)
Set-Location (Split-Path $PSScriptRoot -Parent)

# ══════════════════════════════════════════════════════════════
# ⚠️  ATENÇÃO: Este script SEMPRE aponta para PRODUÇÃO.
# ══════════════════════════════════════════════════════════════
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Tentar obter a connection string dos User Secrets
Write-Host "Buscando credenciais em .NET User Secrets..." -ForegroundColor Cyan
$connString = dotnet user-secrets list --project src/Diax.Api | Where-Object { $_ -match "ConnectionStrings:DefaultConnection =" } | ForEach-Object { ($_ -split "=")[1].Trim() }

if (-not $connString) {
    Write-Host "✗ ERRO: Connection string não encontrada nos User Secrets!" -ForegroundColor Red
    Write-Host "Execute scripts\set-local-db-secret.ps1 primeiro." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n══════════════════════════════════════════════════════════" -ForegroundColor Red
Write-Host "  EF CORE MIGRATIONS — PRODUÇÃO (SmarterASP)" -ForegroundColor Red
Write-Host "══════════════════════════════════════════════════════════" -ForegroundColor Red
Write-Host "  Ambiente: Production" -ForegroundColor Yellow
Write-Host "  Servidor: sql1002.site4now.net" -ForegroundColor Yellow
Write-Host ""

# Executar atualização
dotnet ef database update --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" --startup-project "src\Diax.Api\Diax.Api.csproj" --context Diax.Infrastructure.Data.DiaxDbContext --connection "$connString" --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Migrations aplicadas com sucesso em PRODUÇÃO!" -ForegroundColor Green
} else {
    Write-Host "`n✗ Erro ao aplicar migrations" -ForegroundColor Red
    exit 1
}

Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
