#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Aplica a migration UpdateGeminiModels no banco de dados de PRODUÇÃO.

.DESCRIPTION
    Este script:
    1. Configura a connection string de produção como variável de ambiente
    2. Aplica a migration UpdateGeminiModels
    3. Valida os modelos Gemini no banco
    4. Limpa as variáveis de ambiente

.PARAMETER ConnectionString
    Connection string de produção do SmarterASP (obrigatório)

.EXAMPLE
    .\apply-gemini-update-production.ps1 -ConnectionString "Server=..."
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Navegar para a raiz do projeto
$scriptDir = Split-Path -Parent $PSScriptRoot
Set-Location $scriptDir

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  ATUALIZAÇÃO MODELOS GOOGLE GEMINI - PRODUÇÃO                 ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

# 1. Verificar se a connection string contém dados válidos
if ($ConnectionString -match 'SEU_|PLACEHOLDER|EXAMPLE') {
    Write-Error "Connection string parece ser um placeholder. Forneça a string real."
    exit 1
}

# Máscara da senha para log
$maskedCs = $ConnectionString -replace 'Password=([^;]*)', 'Password=***'
Write-Host "`nConnection String (mascarada): $maskedCs" -ForegroundColor DarkGray

# 2. Configurar variável de ambiente
Write-Step "Configurando connection string de produção..."
$env:ConnectionStrings__DefaultConnection = $ConnectionString
Write-Success "Connection string configurada"

try {
    # 3. Aplicar migration
    Write-Step "Aplicando migration UpdateGeminiModels..."
    dotnet ef database update `
        --project "src\Diax.Infrastructure\Diax.Infrastructure.csproj" `
        --startup-project "src\Diax.Api\Diax.Api.csproj" `
        --context Diax.Infrastructure.Data.DiaxDbContext `
        --verbose

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao aplicar migration"
        exit 1
    }

    Write-Success "Migration aplicada com sucesso"

    # 4. Validar modelos no banco (usando Invoke-Sqlcmd se disponível)
    Write-Step "Validando modelos Gemini no banco..."

    $validateSql = @"
DECLARE @geminiProviderId UNIQUEIDENTIFIER;
SELECT @geminiProviderId = id FROM ai_providers WHERE [key] IN ('google', 'gemini');

SELECT
    p.[key] AS provider_key,
    p.name AS provider_name,
    m.model_key,
    m.display_name,
    m.is_enabled
FROM ai_models m
INNER JOIN ai_providers p ON m.provider_id = p.id
WHERE p.id = @geminiProviderId
ORDER BY
    CASE m.model_key
        WHEN 'gemini-2.5-flash' THEN 1
        WHEN 'gemini-2.0-flash' THEN 2
        WHEN 'gemini-flash-latest' THEN 3
        WHEN 'gemini-pro-latest' THEN 4
        WHEN 'gemma-3-4b-it' THEN 5
        WHEN 'gemma-3-12b-it' THEN 6
        ELSE 99
    END;
"@

    if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
        $models = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $validateSql

        Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║  MODELOS GEMINI ATIVOS                                        ║" -ForegroundColor Green
        Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green

        $models | Format-Table -AutoSize

        # Validar quantidade de modelos ativos
        $activeCount = ($models | Where-Object { $_.is_enabled -eq $true }).Count

        if ($activeCount -eq 6) {
            Write-Success "6 modelos Gemini ativos encontrados ✓"
        } else {
            Write-Host "⚠ Atenção: Esperado 6 modelos ativos, encontrado $activeCount" -ForegroundColor Yellow
        }

        # Verificar modelos esperados
        $expectedModels = @(
            'gemini-2.5-flash',
            'gemini-2.0-flash',
            'gemini-flash-latest',
            'gemini-pro-latest',
            'gemma-3-4b-it',
            'gemma-3-12b-it'
        )

        $actualModels = $models | Where-Object { $_.is_enabled -eq $true } | Select-Object -ExpandProperty model_key
        $missing = $expectedModels | Where-Object { $_ -notin $actualModels }

        if ($missing.Count -eq 0) {
            Write-Success "Todos os modelos esperados estão ativos ✓"
        } else {
            Write-Host "⚠ Modelos faltando: $($missing -join ', ')" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠ Invoke-Sqlcmd não disponível. Validação manual recomendada." -ForegroundColor Yellow
    }

    Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ATUALIZAÇÃO CONCLUÍDA COM SUCESSO                            ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green

    Write-Host "`nPróximos passos:" -ForegroundColor Cyan
    Write-Host "1. Validar o endpoint /api/v1/ai/catalog"
    Write-Host "2. Verificar o frontend em /utilities/prompt-generator"
    Write-Host "3. Fazer commit das alterações"

} catch {
    Write-Error "Erro durante execução: $_"
    exit 1
} finally {
    # 5. Limpar variável de ambiente
    Write-Step "Limpando variáveis de ambiente..."
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Write-Success "Ambiente limpo"
}
