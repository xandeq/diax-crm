param(
  [string]$UserSecretsId = "diax-crm-api-secrets",
  [string]$ConnectionString,
  [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Get-SecretConnectionString {
  param([Parameter(Mandatory = $true)][string]$SecretsId)

  $secretFile = Join-Path $env:APPDATA ("Microsoft\\UserSecrets\\{0}\\secrets.json" -f $SecretsId)
  if (-not (Test-Path $secretFile)) {
    throw "User Secrets file not found: $secretFile. Run scripts\\set-local-db-secret.ps1 first (or pass -ConnectionString)."
  }

  $json = Get-Content $secretFile -Raw | ConvertFrom-Json
  $cs = $json.'ConnectionStrings:DefaultConnection'
  if (-not $cs) {
    throw "Key 'ConnectionStrings:DefaultConnection' not found in $secretFile."
  }

  return $cs
}

function To-SafeConnectionString {
  param([Parameter(Mandatory = $true)][string]$Cs)

  # Mask Password=... (handles quoted values too)
  return ($Cs -replace 'Password=([^;]*|"[^"]*")', 'Password=***')
}

if (-not $ConnectionString) {
  $ConnectionString = Get-SecretConnectionString -SecretsId $UserSecretsId
}

Write-Host "Resetting remote DB (DANGER)" -ForegroundColor Yellow
Write-Host "- ConnectionString (safe): $(To-SafeConnectionString -Cs $ConnectionString)"

# Quick inventory
$migrations = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query "IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL SELECT CAST(NULL AS nvarchar(150)) AS MigrationId WHERE 1=0; ELSE SELECT MigrationId FROM [dbo].[__EFMigrationsHistory] ORDER BY MigrationId;"
$tables = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query "SELECT s.name AS [schema], t.name AS [table] FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY s.name, t.name;"

Write-Host "Existing migrations:" -ForegroundColor Cyan
if ($migrations.Count -eq 0) {
  Write-Host "- (none)"
} else {
  $migrations | ForEach-Object { Write-Host "- $($_.MigrationId)" }
}

Write-Host "Existing tables:" -ForegroundColor Cyan
$tables | ForEach-Object { Write-Host "- $($_.schema).$($_.table)" }

if (-not $Force) {
  $confirm = Read-Host "Type RESET to DROP ALL objects in this database"
  if ($confirm -ne 'RESET') {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 1
  }
}

Write-Host "Dropping views, procedures, functions, tables, and migration history..." -ForegroundColor Yellow

$dropSql = @"
DECLARE @sql nvarchar(max) = N'';

-- Views
SELECT @sql += N'DROP VIEW ' + QUOTENAME(SCHEMA_NAME(v.schema_id)) + N'.' + QUOTENAME(v.name) + N';' + CHAR(13)
FROM sys.views v;
EXEC sp_executesql @sql;

-- Procedures
SET @sql = N'';
SELECT @sql += N'DROP PROCEDURE ' + QUOTENAME(SCHEMA_NAME(p.schema_id)) + N'.' + QUOTENAME(p.name) + N';' + CHAR(13)
FROM sys.procedures p;
EXEC sp_executesql @sql;

-- Functions
SET @sql = N'';
SELECT @sql += N'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' + CHAR(13)
FROM sys.objects o
WHERE o.type IN ('FN','IF','TF');
EXEC sp_executesql @sql;

-- Foreign keys
SET @sql = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
            + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13)
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id = fk.parent_object_id;
EXEC sp_executesql @sql;

-- Tables (includes __EFMigrationsHistory)
SET @sql = N'';
SELECT @sql += N'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
FROM sys.tables t;
EXEC sp_executesql @sql;
"@

Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $dropSql | Out-Null

Write-Host "DB cleared. Running EF migrations..." -ForegroundColor Green

Set-Location (Split-Path $PSScriptRoot -Parent)

# Reuse the standard migration runner
& "$PSScriptRoot\update-db.ps1"
