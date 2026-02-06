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

Set-Location (Split-Path $PSScriptRoot -Parent)

# ══════════════════════════════════════════════════════════════
# ⚠️  ATENÇÃO: Este script SEMPRE aponta para PRODUÇÃO.
# ══════════════════════════════════════════════════════════════
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Verificar se appsettings.Production.json existe
$prodSettings = "src\Diax.Api\appsettings.Production.json"
if (-not (Test-Path $prodSettings)) {
  Write-Host "`n✗ ERRO: Arquivo appsettings.Production.json não encontrado!" -ForegroundColor Red
  Write-Host "  Caminho esperado: $prodSettings" -ForegroundColor Yellow
  Write-Host "  Execute scripts\set-local-db-secret.ps1 para configurar." -ForegroundColor Yellow
  exit 1
}

# Rejeitar qualquer env var apontando para LocalDB
if ($env:ConnectionStrings__DefaultConnection -and $env:ConnectionStrings__DefaultConnection -match 'localdb|localhost|SEU_') {
  Write-Host "Removendo env var ConnectionStrings__DefaultConnection (apontava para banco local)." -ForegroundColor Yellow
  Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
}

Write-Host "`n══════════════════════════════════════════════════════════" -ForegroundColor Red
Write-Host "  EF CORE MIGRATIONS — PRODUÇÃO (SmarterASP)" -ForegroundColor Red
Write-Host "══════════════════════════════════════════════════════════" -ForegroundColor Red
Write-Host "  Ambiente: Production" -ForegroundColor Yellow
Write-Host "  Servidor: sql1002.site4now.net" -ForegroundColor Yellow
Write-Host ""

try {
  dotnet ef database update `
    --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" `
    --startup-project "src\Diax.Api\Diax.Api.csproj" `
    --context Diax.Infrastructure.Data.DiaxDbContext `
    --verbose

  if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Migrations aplicadas com sucesso em PRODUÇÃO!" -ForegroundColor Green
  } else {
    Write-Host "`n✗ Erro ao aplicar migrations" -ForegroundColor Red
    exit 1
  }
}
finally {
  Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
}
