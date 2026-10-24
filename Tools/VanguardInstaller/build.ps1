#	Copyright 2026 KuiseRapper for Generals Vanguard.
#	Additional terms under GNU GPL section 7 apply: see LICENSE.md.
#
#	Builds dist/VanguardSetup.exe.  Run tools/VanguardInstaller/build.ps1 after
#	tools/VanguardLauncher has been published into dist/launcher.
param(
    [string]$Version = "0.0.0",
    [string]$RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path $RepositoryRoot).Path
$launcher = Join-Path $root "dist/launcher/VanguardLauncher.exe"
if (-not (Test-Path $launcher)) {
    throw "Publish the launcher first: dist/launcher/VanguardLauncher.exe is missing."
}

$iscc = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup 6 is not installed. Install it from https://jrsoftware.org/isdl.php."
}

$script = Join-Path $PSScriptRoot "VanguardSetup.iss"
& $iscc "/DMyAppVersion=$Version" $script
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}
$setup = Join-Path $root "dist/VanguardSetup.exe"
if (-not (Test-Path $setup)) {
    throw "Inno Setup produced no dist/VanguardSetup.exe."
}
Write-Host "built $setup"
