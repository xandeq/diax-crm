param(
    [string]$HostName = "sql1002.site4now.net",
    [int]$Port = 1433,
    [string]$Database = "db_aaf0a8_diaxcrm",
    [string]$UserId = "db_aaf0a8_diaxcrm_admin",
    [string]$UserSecretsId = "diax-crm-api-secrets"
)

$ErrorActionPreference = 'Stop'

function Escape-ConnectionStringValue {
    param([Parameter(Mandatory = $true)][string]$Value)

    # Connection string values can be quoted to allow ';' and other special chars.
    # To include a double quote inside a quoted value, escape it by doubling it.
    $needsQuoting = $Value.Contains(';') -or $Value.Contains('"') -or $Value.StartsWith(' ') -or $Value.EndsWith(' ')
    if (-not $needsQuoting) {
        return $Value
    }

    $escaped = $Value -replace '"', '""'
    return '"' + $escaped + '"'
}

Write-Host "Configuring User Secrets for Diax.Api" -ForegroundColor Cyan
Write-Host "- Host: $HostName"
Write-Host "- Port: $Port"
Write-Host "- Database: $Database"
Write-Host "- UserId: $UserId"
Write-Host "- UserSecretsId: $UserSecretsId"

$securePassword = Read-Host "Enter SQL password" -AsSecureString
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
)

try {
    $passwordValue = Escape-ConnectionStringValue -Value $plainPassword
    $connectionString = "Server=tcp:$HostName,$Port;Database=$Database;User Id=$UserId;Password=$passwordValue;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

    $userSecretsRoot = Join-Path $env:APPDATA "Microsoft\UserSecrets"
    $secretDir = Join-Path $userSecretsRoot $UserSecretsId
    $secretFile = Join-Path $secretDir "secrets.json"

    New-Item -ItemType Directory -Force -Path $secretDir | Out-Null

    $secrets = @{}
    if (Test-Path $secretFile) {
        $existing = Get-Content $secretFile -Raw | ConvertFrom-Json
        if ($null -ne $existing) {
            $existing.PSObject.Properties | ForEach-Object { $secrets[$_.Name] = $_.Value }
        }
    }

    $secrets["ConnectionStrings:DefaultConnection"] = $connectionString

    ($secrets | ConvertTo-Json -Depth 10) | Set-Content -Path $secretFile -Encoding UTF8

    Write-Host "OK: User Secret written." -ForegroundColor Green
    Write-Host "Path: $secretFile"
    Write-Host "Key: ConnectionStrings:DefaultConnection"
}
finally {
    if ($plainPassword) {
        $plainPassword = $null
    }
}
