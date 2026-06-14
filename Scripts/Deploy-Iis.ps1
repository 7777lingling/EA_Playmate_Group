[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SiteName,

    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [string]$HealthUrl,

    [ValidateRange(1, 20)]
    [int]$BackupsToKeep = 5
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Import-Module WebAdministration -ErrorAction Stop

function Get-SafeFullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $fullPath = [System.IO.Path]::GetFullPath(
        [Environment]::ExpandEnvironmentVariables($Path))
    $root = [System.IO.Path]::GetPathRoot($fullPath)

    if ([string]::IsNullOrWhiteSpace($root) -or
        $fullPath.TrimEnd('\') -eq $root.TrimEnd('\')) {
        throw "$Description cannot be a filesystem root: $fullPath"
    }

    return $fullPath.TrimEnd('\')
}

function Invoke-MirrorCopy {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    & robocopy $Source $Destination /MIR /R:2 /W:2 /NFL /NDL /NP `
        /XD DataProtectionKeys logs `
        /XF appsettings.Development.json appsettings.Production.json app_offline.htm

    if ($LASTEXITCODE -ge 8) {
        throw "Robocopy failed with exit code $LASTEXITCODE."
    }
}

$sitePath = "IIS:\Sites\$SiteName"
if (-not (Test-Path -LiteralPath $sitePath)) {
    throw "IIS site '$SiteName' was not found."
}

$site = Get-Item -LiteralPath $sitePath
$appPoolName = [string]$site.applicationPool
if ([string]::IsNullOrWhiteSpace($appPoolName)) {
    throw "IIS site '$SiteName' has no application pool."
}

$package = Get-SafeFullPath -Path (Resolve-Path -LiteralPath $PackagePath).Path `
    -Description "Package path"
$deployPath = Get-SafeFullPath -Path ([string]$site.physicalPath) `
    -Description "IIS physical path"

if (-not (Test-Path -LiteralPath (Join-Path $package "EAPlaymateGroup.dll"))) {
    throw "Package does not contain EAPlaymateGroup.dll: $package"
}

if ($package.StartsWith("$deployPath\", [StringComparison]::OrdinalIgnoreCase) -or
    $deployPath.StartsWith("$package\", [StringComparison]::OrdinalIgnoreCase)) {
    throw "Package and deployment paths must not contain each other."
}

$deployParent = Split-Path -Parent $deployPath
$backupRoot = Get-SafeFullPath `
    -Path (Join-Path $deployParent "_deploy-backups\$SiteName") `
    -Description "Backup root"
$backupPath = Join-Path $backupRoot (Get-Date -Format "yyyyMMdd-HHmmss")
$offlinePath = Join-Path $deployPath "app_offline.htm"

New-Item -ItemType Directory -Path $deployPath -Force | Out-Null
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

Write-Host "Backing up '$deployPath' to '$backupPath'..."
Invoke-MirrorCopy -Source $deployPath -Destination $backupPath

$deploymentSucceeded = $false
try {
    Set-Content -LiteralPath $offlinePath `
        -Value "<html><body><h1>系統更新中，請稍後再試。</h1></body></html>" `
        -Encoding UTF8
    Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue

    Write-Host "Deploying '$package' to '$deployPath'..."
    Invoke-MirrorCopy -Source $package -Destination $deployPath

    Remove-Item -LiteralPath $offlinePath -Force -ErrorAction SilentlyContinue
    Start-WebAppPool -Name $appPoolName

    if (-not [string]::IsNullOrWhiteSpace($HealthUrl)) {
        $healthy = $false
        for ($attempt = 1; $attempt -le 12; $attempt++) {
            Start-Sleep -Seconds 5
            try {
                $response = Invoke-WebRequest -UseBasicParsing -Uri $HealthUrl -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    $healthy = $true
                    break
                }
            }
            catch {
                Write-Host "Health check attempt $attempt failed."
            }
        }

        if (-not $healthy) {
            throw "Health check failed: $HealthUrl"
        }
    }

    $deploymentSucceeded = $true
    Write-Host "IIS deployment completed."
}
finally {
    if (-not $deploymentSucceeded) {
        Write-Warning "Deployment failed. Restoring '$backupPath'..."
        Set-Content -LiteralPath $offlinePath -Value "Rollback in progress." -Encoding UTF8
        Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
        Invoke-MirrorCopy -Source $backupPath -Destination $deployPath
    }

    Remove-Item -LiteralPath $offlinePath -Force -ErrorAction SilentlyContinue
    Start-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
}

$backupRootPrefix = "$backupRoot\"
Get-ChildItem -LiteralPath $backupRoot -Directory |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip $BackupsToKeep |
    ForEach-Object {
        $candidate = [System.IO.Path]::GetFullPath($_.FullName)
        if (-not $candidate.StartsWith($backupRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to delete backup outside '$backupRoot': $candidate"
        }

        Remove-Item -LiteralPath $candidate -Recurse -Force
    }
