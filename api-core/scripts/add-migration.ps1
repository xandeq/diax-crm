# Add EF Core migration for DIAX CRM
# Usage: .\scripts\add-migration.ps1 -Name "MigrationName"

param(
    [Parameter(Mandatory=$true)]
    [string]$Name,
    [string]$Environment = "Development"
)

$env:ASPNETCORE_ENVIRONMENT = $Environment

$apiCoreDir = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $apiCoreDir

Write-Host "Adding migration: $Name" -ForegroundColor Cyan

try {
    & dotnet ef migrations add $Name `
        --project "src/Diax.Infrastructure" `
        --startup-project "src/Diax.Api" `
        --context DiaxDbContext

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Migration '$Name' created successfully!" -ForegroundColor Green
    } else {
        Write-Host "Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error creating migration:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
