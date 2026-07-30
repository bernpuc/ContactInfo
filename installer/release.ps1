# Release script for ContactInfo
# Builds the installer, tags the release, and publishes a GitHub release
# with both the installer and the Getting Started PDF attached — so the
# PDF can't be forgotten as a release asset the way it was for v1.3.1.
#
# Prerequisites: Inno Setup 6, GitHub CLI (gh) authenticated
#
# Usage: .\release.ps1 -Notes "Release notes here"

param(
    [Parameter(Mandatory = $true)]
    [string]$Notes
)

$ErrorActionPreference = "Stop"

$root        = Split-Path $PSScriptRoot -Parent
$project     = Join-Path $root "ContactInfo\ContactInfo.csproj"
$outputDir   = Join-Path $PSScriptRoot "Output"
$buildScript = Join-Path $PSScriptRoot "build.ps1"

# Version is read from the csproj rather than passed as a parameter, so the
# tag can never drift from what's actually baked into the built app/installer.
[xml]$csprojXml = Get-Content $project
$version = $csprojXml.Project.PropertyGroup.Version | Select-Object -First 1
if (-not $version) {
    Write-Error "Could not read <Version> from '$project'."
}
$tag = "v$version"

if (git -C $root tag -l $tag) {
    Write-Error "Tag '$tag' already exists. Bump <Version> in ContactInfo.csproj first."
}

Write-Host "Releasing ContactInfo $tag" -ForegroundColor Cyan

& $buildScript
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$installerExe      = Join-Path $outputDir "ContactInfoSetup.exe"
$gettingStartedPdf = Join-Path $outputDir "GETTING-STARTED.pdf"

foreach ($assetPath in @($installerExe, $gettingStartedPdf)) {
    if (-not (Test-Path $assetPath)) {
        Write-Error "Expected release asset missing after build: '$assetPath'"
    }
}

Write-Host "Tagging $tag..." -ForegroundColor Cyan
git -C $root tag -a $tag -m "ContactInfo $tag"
git -C $root push origin $tag

Write-Host "Creating GitHub release..." -ForegroundColor Cyan
gh release create $tag $installerExe $gettingStartedPdf `
    --title "ContactInfo $tag" `
    --notes $Notes

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done. Release: https://github.com/bernpuc/ContactInfo/releases/tag/$tag" -ForegroundColor Green
