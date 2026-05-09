<#
.SYNOPSIS
    One-time migration: bumps EmailSentCount / LastEmailSentAt on customers
    that appear in the Python-era sent_emails JSON ledgers.

.NOTES
    Run ONCE after Phase 1 has been deployed.
    Reads DB connection string from dotnet user-secrets.
#>

param(
    [string]$LedgerSites = "C:/tmp/sent_emails.json",
    [string]$LedgerApps  = "C:/tmp/sent_emails_apps.json",
    [string]$ProjectDir  = "$PSScriptRoot/../src/Diax.Api"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Lendo connection string dos user-secrets..."
$secretLine = dotnet user-secrets list --project $ProjectDir 2>&1 |
    Where-Object { $_ -like "ConnectionStrings:DefaultConnection*" } |
    Select-Object -First 1

if (-not $secretLine) {
    throw "ConnectionStrings:DefaultConnection nao encontrado nos user-secrets"
}
$ConnectionString = ($secretLine -split " = ", 2)[1].Trim()
Write-Host "  Conectando em: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')"

$allEmails = @()
foreach ($path in @($LedgerSites, $LedgerApps)) {
    if (Test-Path $path) {
        $emails = Get-Content $path -Raw | ConvertFrom-Json
        Write-Host "  Lido: $path ($($emails.Count) emails)"
        $allEmails += $emails
    } else {
        Write-Warning "  Arquivo nao encontrado: $path (pulando)"
    }
}

if ($allEmails.Count -eq 0) {
    Write-Host "Nenhum email encontrado nos ledgers. Encerrando."
    exit 0
}

$uniqueEmails = $allEmails | ForEach-Object { $_.ToLower().Trim() } | Sort-Object -Unique
Write-Host "Total unico de emails: $($uniqueEmails.Count)"

$proxyDate = [DateTime]::Parse("2026-04-01T00:00:00Z").ToUniversalTime()

$updateSql = @"
UPDATE customers
SET    email_sent_count   = email_sent_count + 1,
       last_email_sent_at = CASE
           WHEN last_email_sent_at IS NULL OR last_email_sent_at < @proxyDate
           THEN @proxyDate
           ELSE last_email_sent_at
       END,
       updated_at = GETUTCDATE(),
       updated_by = 'python_ledger_import'
WHERE  LOWER(email) = @email
"@

$checkSql = "SELECT COUNT(1) FROM customers WHERE LOWER(email) = @email"

Add-Type -AssemblyName "System.Data"
$conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$conn.Open()

$bumped   = 0
$notFound = 0
$errors   = 0
$i        = 0

foreach ($email in $uniqueEmails) {
    $i++
    try {
        $checkCmd = $conn.CreateCommand()
        $checkCmd.CommandText = $checkSql
        $checkCmd.Parameters.AddWithValue("@email", $email) | Out-Null
        $count = [int]$checkCmd.ExecuteScalar()

        if ($count -eq 0) {
            $notFound++
            if ($notFound -le 10 -or ($notFound % 50 -eq 0)) {
                Write-Host "  [not_found] $email"
            }
        } else {
            $updateCmd = $conn.CreateCommand()
            $updateCmd.CommandText = $updateSql
            $updateCmd.Parameters.AddWithValue("@email", $email) | Out-Null
            $updateCmd.Parameters.AddWithValue("@proxyDate", $proxyDate) | Out-Null
            $updateCmd.ExecuteNonQuery() | Out-Null
            $bumped++
        }
    } catch {
        $errors++
        Write-Warning "  [error] $email : $_"
    }

    if ($i % 100 -eq 0) {
        Write-Host "  Progresso: $i / $($uniqueEmails.Count) (bumped=$bumped not_found=$notFound)"
    }
}

$conn.Close()

Write-Host ""
Write-Host "========================================="
Write-Host "  IMPORT LEDGERS PYTHON - CONCLUIDO"
Write-Host "  Total processado : $($uniqueEmails.Count)"
Write-Host "  Customers bumped : $bumped"
Write-Host "  Not found        : $notFound"
Write-Host "  Errors           : $errors"
Write-Host "========================================="
