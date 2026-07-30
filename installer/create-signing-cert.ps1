# One-time setup: creates a self-signed code-signing certificate for ContactInfo releases.
#
# The .pfx (private key) stays local - it's gitignored (*.pfx) and build.ps1 uses it to sign
# the installer. The .cer (public key only) gets committed and distributed to users so they
# can trust the publisher and stop seeing "Unknown publisher" in the SmartScreen warning.
#
# This is a free alternative to a paid Authenticode certificate: it changes the publisher
# name shown in the warning for anyone who imports the .cer, but does NOT build SmartScreen
# reputation the way a CA-issued cert would, so the warning screen itself may still appear.
#
# Usage: .\create-signing-cert.ps1  (run once; re-run only if the cert is lost or expired)

$ErrorActionPreference = "Stop"

$signingDir = Join-Path $PSScriptRoot "signing"
$pfxPath    = Join-Path $signingDir "ContactInfo-signing.pfx"
$cerPath    = Join-Path $signingDir "ContactInfo-signing.cer"

if (Test-Path $pfxPath) {
    Write-Error "A signing certificate already exists at '$pfxPath'. Delete it first if you really want to regenerate - this invalidates trust for anyone who already imported the old .cer."
}

New-Item -ItemType Directory -Path $signingDir -Force | Out-Null

Write-Host "Creating self-signed code-signing certificate..." -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=ContactInfo (Ethan Puc), O=Ethan Puc, C=US" `
    -KeyUsage DigitalSignature `
    -KeyExportPolicy Exportable `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation Cert:\CurrentUser\My

Write-Host "Set a password to protect the private key. You'll be asked for it whenever build.ps1 signs a release." -ForegroundColor Yellow
$pfxPassword = Read-Host -AsSecureString -Prompt "PFX password"

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pfxPassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

# The .pfx file is now the source of truth for signing; drop the copy from the
# user cert store so there's only one place the private key lives.
Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)"

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Private key (gitignored, keep local): $pfxPath"
Write-Host "Public cert (commit + distribute):     $cerPath"
