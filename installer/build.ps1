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
$outputDir  = Join-Path $PSScriptRoot "Output"
$gettingStartedPdf = Join-Path $root "GETTING-STARTED.pdf"
$signingPfx = Join-Path $PSScriptRoot "signing\ContactInfo-signing.pfx"
$installerExe = Join-Path $outputDir "ContactInfoSetup.exe"

if (-not (Test-Path $gettingStartedPdf)) {
    Write-Error "GETTING-STARTED.pdf not found at '$gettingStartedPdf'. Generate it from GETTING-STARTED.md before building."
}

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

Write-Host "Copying GETTING-STARTED.pdf next to the installer (for the download page)..." -ForegroundColor Cyan
Copy-Item $gettingStartedPdf -Destination $outputDir -Force

if (Test-Path $signingPfx) {
    $signtool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName

    if (-not $signtool) {
        Write-Warning "signtool.exe not found (Windows SDK not installed?). Skipping signing - installer will be unsigned."
    } else {
        Write-Host "Signing installer with self-signed certificate..." -ForegroundColor Cyan
        $pfxPassword = Read-Host -AsSecureString -Prompt "Signing certificate password"
        $pfxCred = New-Object System.Management.Automation.PSCredential("unused", $pfxPassword)
        & $signtool sign /f $signingPfx /p $($pfxCred.GetNetworkCredential().Password) /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $installerExe

        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        & $signtool verify /pa $installerExe
    }
} else {
    Write-Warning "No signing certificate at '$signingPfx' - installer will be unsigned. Run installer\create-signing-cert.ps1 to create one."
}

Write-Host ""
Write-Host "Done. Installer is at:          installer\Output\ContactInfoSetup.exe" -ForegroundColor Green
Write-Host "Getting-started PDF is at:      installer\Output\GETTING-STARTED.pdf" -ForegroundColor Green
Write-Host "Upload both to the download page." -ForegroundColor Green
