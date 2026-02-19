# ============================================================================
# Script de Correção - Migração CustomerImports
# ============================================================================
# Este script corrige a migração vazia e recria corretamente a tabela
# customer_imports no banco de dados.
#
# Autor: Claude (DIAX CRM)
# Data: 15/02/2026
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX CUSTOMER IMPORTS MIGRATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Parar processos da API rodando
Write-Host "[1/6] Parando processos da API..." -ForegroundColor Yellow
$processes = Get-Process -Name "Diax.Api" -ErrorAction SilentlyContinue
if ($processes) {
    $processes | Stop-Process -Force
    Write-Host "  ✓ API parada com sucesso" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "  ℹ Nenhum processo da API rodando" -ForegroundColor Gray
}

# Navegar para o diretório correto
Set-Location "api-core/src/Diax.Api"

# 2. Remover migração vazia
Write-Host ""
Write-Host "[2/6] Removendo migração vazia..." -ForegroundColor Yellow
try {
    dotnet ef migrations remove --project ../Diax.Infrastructure --startup-project . --force
    Write-Host "  ✓ Migração vazia removida" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ Erro ao remover migração (pode não existir)" -ForegroundColor DarkYellow
}

# 3. Limpar e reconstruir projeto
Write-Host ""
Write-Host "[3/6] Limpando e reconstruindo projeto..." -ForegroundColor Yellow
dotnet clean --configuration Debug
dotnet build --configuration Debug --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ ERRO: Build falhou!" -ForegroundColor Red
    Write-Host "  Por favor, corrija os erros de compilação primeiro." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Build concluído com sucesso" -ForegroundColor Green

# 4. Criar nova migração
Write-Host ""
Write-Host "[4/6] Criando nova migração..." -ForegroundColor Yellow
dotnet ef migrations add AddCustomerImportsTable --project ../Diax.Infrastructure --startup-project .

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ ERRO: Falha ao criar migração!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Migração criada com sucesso" -ForegroundColor Green

# 5. Aplicar migração no banco local
Write-Host ""
Write-Host "[5/6] Aplicando migração no banco de dados local..." -ForegroundColor Yellow
dotnet ef database update --project ../Diax.Infrastructure --startup-project .

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ ERRO: Falha ao aplicar migração!" -ForegroundColor Red
    Write-Host "  Verifique a connection string e se o SQL Server está rodando." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Migração aplicada no banco local" -ForegroundColor Green

# 6. Gerar script SQL para produção
Write-Host ""
Write-Host "[6/6] Gerando script SQL para produção..." -ForegroundColor Yellow
Set-Location ../../..
dotnet ef migrations script --idempotent --project src/Diax.Infrastructure --startup-project src/Diax.Api --output apply_customer_imports_production.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Script SQL gerado: apply_customer_imports_production.sql" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Não foi possível gerar script SQL" -ForegroundColor DarkYellow
}

# Voltar para a raiz do projeto
Set-Location ..

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ✓ PROCESSO CONCLUÍDO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Próximos passos:" -ForegroundColor White
Write-Host "1. Verificar se a tabela foi criada no banco local:" -ForegroundColor White
Write-Host "   SELECT * FROM customer_imports" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Para aplicar em PRODUÇÃO, execute o script:" -ForegroundColor White
Write-Host "   apply_customer_imports_production.sql" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Iniciar a API e testar a importação:" -ForegroundColor White
Write-Host "   cd api-core/src/Diax.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
