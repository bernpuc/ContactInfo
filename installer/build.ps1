# Build script for ContactInfo Windows installer
# Prerequisites: Inno Setup 6 installed (https://jrsoftware.org/isinfo.php)
#
# Usage: .\build.ps1
# Output: installer\Output\ContactInfoSetup.exe

$ErrorActionPreference = "Stop"

$root       = Split-Path $PSScriptRoot -Parent
$project    = Join-Path $root "ContactInfo\ContactInfo.csproj"
$publishDir = Join-Path $PSScriptRoot "publish"
$issFile    = Join-Path $PSScriptRoot "ContactInfo.iss"

# Locate Inno Setup compiler
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    Write-Error "Inno Setup 6 not found at '$iscc'. Download from https://jrsoftware.org/isinfo.php"
}

Write-Host "Publishing ContactInfo (self-contained, win-x64)..." -ForegroundColor Cyan
dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running Inno Setup..." -ForegroundColor Cyan
& $iscc $issFile

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done. Installer is at: installer\Output\ContactInfoSetup.exe" -ForegroundColor Green
