[CmdletBinding()]
param(
    # Optional. If omitted, inferred from `git remote get-url origin`.
    [string]$Repo,

    # Optional defaults for convenience.
    [string]$FtpServer = "",
    [string]$FtpUsername = "",
    [string]$FtpRemoteDir = ""
)

$ErrorActionPreference = 'Stop'

function Get-RepoFromGitRemote {
    $originUrl = (git remote get-url origin 2>$null)
    if (-not $originUrl) {
        throw "Não foi possível obter a URL do remote 'origin'. Passe -Repo owner/repo." 
    }

    # Supports:
    # - https://github.com/owner/repo.git
    # - git@github.com:owner/repo.git
    if ($originUrl -match 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(\.git)?$') {
        return "$($Matches.owner)/$($Matches.repo)"
    }

    throw "Remote origin não parece ser GitHub: $originUrl"
}

function Assert-GhCli {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) {
        throw "GitHub CLI (gh) não encontrado. Instale: https://cli.github.com/"
    }

    & gh auth status *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Você não está autenticado no gh. Rode: gh auth login"
    }
}

function Set-GhSecretFromValue {
    param(
        [Parameter(Mandatory = $true)][string]$Repo,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    # Para evitar expor o valor em argumentos, enviamos via STDIN.
    $Value | & gh secret set $Name --repo $Repo --app actions *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao definir secret: $Name"
    }
    Write-Host "OK: $Name"
}

function Read-HostWithDefault {
    param(
        [Parameter(Mandatory = $true)][string]$Prompt,
        [Parameter(Mandatory = $true)][string]$Default
    )

    $value = Read-Host "$Prompt [$Default]"
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $Default
    }
    return $value
}

if (-not $Repo) {
    $Repo = Get-RepoFromGitRemote
}

Assert-GhCli

$defaultServer = "win1151.site4now.net"
$defaultUsername = "partiurock-003"
$defaultRemoteDir = "/api-diax-crm/"

if (-not $FtpServer) {
    $FtpServer = Read-HostWithDefault -Prompt "SMARTERASP_FTP_SERVER (hostname do FTP)" -Default $defaultServer
}
if (-not $FtpUsername) {
    $FtpUsername = Read-HostWithDefault -Prompt "SMARTERASP_FTP_USERNAME" -Default $defaultUsername
}
if (-not $FtpRemoteDir) {
    $FtpRemoteDir = Read-HostWithDefault -Prompt "SMARTERASP_FTP_REMOTE_DIR (pasta remota)" -Default $defaultRemoteDir
}

$passwordSecure = Read-Host "SMARTERASP_FTP_PASSWORD" -AsSecureString
$passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordSecure)
)

try {
    Set-GhSecretFromValue -Repo $Repo -Name "SMARTERASP_FTP_SERVER" -Value $FtpServer
    Set-GhSecretFromValue -Repo $Repo -Name "SMARTERASP_FTP_USERNAME" -Value $FtpUsername
    Set-GhSecretFromValue -Repo $Repo -Name "SMARTERASP_FTP_PASSWORD" -Value $passwordPlain
    Set-GhSecretFromValue -Repo $Repo -Name "SMARTERASP_FTP_REMOTE_DIR" -Value $FtpRemoteDir

    Write-Host "\nSecrets configurados com sucesso para $Repo."
}
finally {
    # Best-effort cleanup
    $passwordPlain = ""
}
