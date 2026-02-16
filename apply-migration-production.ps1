# Script PowerShell para aplicar migration UpdateCustomerPhoneColumnsSize em produção
#
# ATENÇÃO: Este script requer a senha do banco de dados de produção

param(
    [Parameter(Mandatory=$false)]
    [string]$Password = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Aplicar Migration em Produção" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Migration: UpdateCustomerPhoneColumnsSize" -ForegroundColor Yellow
Write-Host "Objetivo: Aumentar colunas phone e whats_app de 20 -> 50 caracteres" -ForegroundColor Yellow
Write-Host ""

# Solicita senha se não foi fornecida
if ([string]::IsNullOrWhiteSpace($Password)) {
    $SecurePassword = Read-Host "Digite a senha do banco de produção (db_aaf0a8_diaxcrm_admin)" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Connection String
$ConnectionString = "Server=tcp:sql1002.site4now.net;Database=db_aaf0a8_diaxcrm;User Id=db_aaf0a8_diaxcrm_admin;Password=$Password;TrustServerCertificate=True;Encrypt=True;"

Write-Host "Conectando ao banco de dados..." -ForegroundColor Green
Write-Host "Server: sql1002.site4now.net" -ForegroundColor Gray
Write-Host "Database: db_aaf0a8_diaxcrm" -ForegroundColor Gray
Write-Host ""

# Muda para o diretório do projeto
Set-Location "$PSScriptRoot\api-core"

Write-Host "Aplicando migration..." -ForegroundColor Green
Write-Host ""

# Executa dotnet ef database update
try {
    dotnet ef database update `
        --project src/Diax.Infrastructure `
        --startup-project src/Diax.Api `
        --connection $ConnectionString `
        --verbose

    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  ✓ Migration aplicada com sucesso!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Próximos passos:" -ForegroundColor Cyan
        Write-Host "1. Testar importação de clientes em: https://crm.alexandrequeiroz.com.br/leads/import" -ForegroundColor White
        Write-Host "2. Verificar logs da API para confirmar que não há mais erros de truncamento" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "  ✗ Erro ao aplicar migration" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Alternativa: Execute manualmente o script SQL" -ForegroundColor Yellow
        Write-Host "Arquivo: api-core/update_phone_columns_prod.sql" -ForegroundColor Yellow
        Write-Host "Instruções: APLICAR_MIGRATION_PRODUCAO.md" -ForegroundColor Yellow
        Write-Host ""
        exit $exitCode
    }
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ✗ Erro ao aplicar migration" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Erro: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternativa: Execute manualmente o script SQL" -ForegroundColor Yellow
    Write-Host "Arquivo: api-core/update_phone_columns_prod.sql" -ForegroundColor Yellow
    Write-Host "Instruções: APLICAR_MIGRATION_PRODUCAO.md" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
