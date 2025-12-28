param(
  [string]$Repo
)

$ErrorActionPreference = "Stop"

function Get-RepoFromGitRemote {
  $remote = git remote get-url origin 2>$null
  if (-not $remote) { throw "Não achei remote 'origin'. Passe -Repo owner/repo." }

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

function Read-SecretPlain([string]$prompt) {
  $s = Read-Host $prompt -AsSecureString
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
  try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
  finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

Assert-GhCli

if (-not $Repo) {
  $Repo = Get-RepoFromGitRemote
}

Write-Host "Repo: $Repo" -ForegroundColor Cyan

$adminEmail = Read-Host "DIAX_AUTH_ADMIN_EMAIL (ex: admin@alexandrequeiroz.com.br)"
$adminPassword = Read-SecretPlain "DIAX_AUTH_ADMIN_PASSWORD"

# JWT key: mínimo 32 chars recomendado
$jwtKey = Read-SecretPlain "DIAX_JWT_KEY (mínimo 32 caracteres)"

Set-GhSecretFromValue -Repo $Repo -Name "DIAX_AUTH_ADMIN_EMAIL" -Value $adminEmail
Set-GhSecretFromValue -Repo $Repo -Name "DIAX_AUTH_ADMIN_PASSWORD" -Value $adminPassword
Set-GhSecretFromValue -Repo $Repo -Name "DIAX_JWT_KEY" -Value $jwtKey

Write-Host "\nPronto. Secrets de autenticação definidos." -ForegroundColor Cyan
