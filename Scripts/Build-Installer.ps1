$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "EAPlaymateGroup.csproj"
$installerScript = Join-Path $root "installer\EAPlaymateGroup.iss"
$artifacts = Join-Path $root "artifacts\installer"

$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if (-not $iscc) {
    $knownPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $knownPaths) {
        if (Test-Path $path) {
            $iscc = Get-Item $path
            break
        }
    }
}

if (-not $iscc) {
    throw "ISCC.exe was not found. Install Inno Setup 6, then run this script again."
}

$runningApps = Get-Process "EAPlaymateGroup" -ErrorAction SilentlyContinue
if ($runningApps) {
    Write-Host "Stopping running EAPlaymateGroup.exe..."
    $runningApps | Stop-Process -Force
}

Write-Host "Publishing EA Playmate Group..."
dotnet publish $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

Write-Host "Building installer..."
& $iscc.Source $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

Write-Host "Done. Installer output:"
Get-ChildItem $artifacts -Filter "*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 5 FullName, Length, LastWriteTime
