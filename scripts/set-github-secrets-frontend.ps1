param(
  [string]$Repo
)

$ErrorActionPreference = "Stop"

function Get-RepoFromGitRemote {
  $remote = git remote get-url origin 2>$null
  if (-not $remote) { throw "Não achei remote 'origin'. Passe -Repo owner/repo." }

  # https://github.com/owner/repo.git
  if ($remote -match "github.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)") {
    return "$($Matches.owner)/$($Matches.repo)"
  }

  throw "Não consegui inferir owner/repo do remote: $remote"
}

function Assert-GhCli {
  if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) não encontrado. Instale e rode: gh auth login"
  }
}

function Set-GhSecretFromValue {
  param(
    [Parameter(Mandatory=$true)][string]$Repo,
    [Parameter(Mandatory=$true)][string]$Name,
    [Parameter(Mandatory=$true)][string]$Value
  )

  $Value | gh secret set $Name --repo $Repo --body-file - | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "Falha ao definir secret: $Name" }
  Write-Host "OK: $Name" -ForegroundColor Green
}

Assert-GhCli

if (-not $Repo) {
  $Repo = Get-RepoFromGitRemote
}

Write-Host "Repo: $Repo" -ForegroundColor Cyan

$ftpServer = Read-Host "HOSTGATOR_FTP_SERVER" 
if ([string]::IsNullOrWhiteSpace($ftpServer)) { $ftpServer = "ftp.alexandrequeiroz.com.br" }

$ftpUser = Read-Host "HOSTGATOR_FTP_USERNAME" 
if ([string]::IsNullOrWhiteSpace($ftpUser)) { $ftpUser = "crm@alexandrequeiroz.com.br" }

$ftpDir = Read-Host "HOSTGATOR_FTP_REMOTE_DIR"
if ([string]::IsNullOrWhiteSpace($ftpDir)) { $ftpDir = "/" }

$apiBase = Read-Host "CRM_WEB_API_BASE_URL"
if ([string]::IsNullOrWhiteSpace($apiBase)) { $apiBase = "https://api.alexandrequeiroz.com.br" }

# password (secure prompt)
$passSecure = Read-Host "HOSTGATOR_FTP_PASSWORD" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($passSecure)
try {
  $ftpPass = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
}
finally {
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

Set-GhSecretFromValue -Repo $Repo -Name "HOSTGATOR_FTP_SERVER" -Value $ftpServer
Set-GhSecretFromValue -Repo $Repo -Name "HOSTGATOR_FTP_USERNAME" -Value $ftpUser
Set-GhSecretFromValue -Repo $Repo -Name "HOSTGATOR_FTP_PASSWORD" -Value $ftpPass
Set-GhSecretFromValue -Repo $Repo -Name "HOSTGATOR_FTP_REMOTE_DIR" -Value $ftpDir
Set-GhSecretFromValue -Repo $Repo -Name "CRM_WEB_API_BASE_URL" -Value $apiBase

Write-Host "\nPronto. Secrets do frontend definidos." -ForegroundColor Cyan
