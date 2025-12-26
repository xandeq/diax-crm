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

    $tempFile = New-TemporaryFile
    try {
        Set-Content -Path $tempFile -Value $Value -NoNewline -Encoding utf8
        & gh secret set $Name --repo $Repo --body-file $tempFile.FullName *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao definir secret: $Name"
        }
        Write-Host "OK: $Name"
    }
    finally {
        Remove-Item -Force -ErrorAction SilentlyContinue $tempFile
    }
}

if (-not $Repo) {
    $Repo = Get-RepoFromGitRemote
}

Assert-GhCli

if (-not $FtpServer) {
    $FtpServer = Read-Host "SMARTERASP_FTP_SERVER (ex: win1151.site4now.net)"
}
if (-not $FtpUsername) {
    $FtpUsername = Read-Host "SMARTERASP_FTP_USERNAME"
}
if (-not $FtpRemoteDir) {
    $FtpRemoteDir = Read-Host "SMARTERASP_FTP_REMOTE_DIR (ex: /api-diax-crm/)"
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
