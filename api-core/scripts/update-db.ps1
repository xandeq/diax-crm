# Migration script for DIAX CRM production database
# Usage: .\scripts\update-db.ps1

param(
    [string]$Environment = "Production"
)

# Load credentials from .env.production file
$envFile = Join-Path (Get-Item $PSScriptRoot).Parent.Parent.FullName ".env.production"

if (Test-Path $envFile) {
    Write-Host "✓ Loading database credentials from .env.production" -ForegroundColor Green

    $envVars = @{}
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $key, $value = $line -split "=", 2
            if ($key -and $value) {
                $envVars[$key.Trim()] = $value.Trim()
            }
        }
    }

    $dbHost = $envVars["DB_HOST"]
    $dbPort = $envVars["DB_PORT"]
    $dbName = $envVars["DB_NAME"]
    $dbUser = $envVars["DB_USER"]
    $dbPass = $envVars["DB_PASS"]
} else {
    Write-Host "⚠ .env.production not found, using environment variables" -ForegroundColor Yellow

    $dbHost = $env:DB_HOST ?? "sql1002.site4now.net"
    $dbPort = $env:DB_PORT ?? "1433"
    $dbName = $env:DB_NAME ?? "db_aaf0a8_diaxcrm"
    $dbUser = $env:DB_USER ?? "db_aaf0a8_diaxcrm"
    $dbPass = $env:DB_PASS ?? "10Alexandre10#"
}

Write-Host "Database Configuration:" -ForegroundColor Cyan
Write-Host "  Host: $dbHost"
Write-Host "  Port: $dbPort"
Write-Host "  Database: $dbName"
Write-Host "  User: $dbUser"
Write-Host ""

# Set environment for EF Core migration
$env:ASPNETCORE_ENVIRONMENT = $Environment

# Build connection string
$connectionString = "Server=tcp:$dbHost,$dbPort;Database=$dbName;User Id=$dbUser;Password=$dbPass;TrustServerCertificate=True;Encrypt=True;Connection Timeout=60;"

Write-Host "Starting EF Core migrations..." -ForegroundColor Cyan
Write-Host ""

# Navigate to api-core directory
$apiCoreDir = Join-Path (Get-Item $PSScriptRoot).Parent.Parent.FullName "."
Set-Location $apiCoreDir

# Apply migrations using dotnet ef
Write-Host "Running: dotnet ef database update --project src/Diax.Infrastructure --startup-project src/Diax.Api" -ForegroundColor Gray
Write-Host ""

try {
    # Set connection string for EF Core
    $env:ConnectionStrings__DefaultConnection = $connectionString

    & dotnet ef database update `
        --project "src/Diax.Infrastructure" `
        --startup-project "src/Diax.Api" `
        --context DiaxDbContext `
        --configuration Release `
        --verbose

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Database migrations applied successfully!" -ForegroundColor Green
        Write-Host "The following changes were made:" -ForegroundColor Green
        Write-Host "  - Added is_video_provider, is_text_provider columns to ai_providers"
        Write-Host "  - Added is_active, max_duration_seconds, max_resolution, supported_aspect_ratios to ai_models"
        Write-Host "  - Added quota tracking columns to ai_usage_logs"
        Write-Host "  - Created ai_provider_quotas table"
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "✗ Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "✗ Error running migrations:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
