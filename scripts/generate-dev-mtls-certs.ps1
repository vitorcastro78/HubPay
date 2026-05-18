# Gera certificados auto-assinados de desenvolvimento para testar mTLS localmente.
# NUNCA usar estes certificados em produção.

param(
    [string]$OutputRoot = "$PSScriptRoot\..\certificates",
    [string]$Password = "hubpay-dev"
)

$psps = @("sibs", "bizum", "wero", "ideal", "bancontact", "cartesbancaires", "euro6000", "bancomatpay", "swish", "vipps")

foreach ($psp in $psps) {
    $dir = Join-Path $OutputRoot $psp
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    $pfxPath = Join-Path $dir "client.pfx"
    $caPath = Join-Path $dir "ca.crt"

    $cert = New-SelfSignedCertificate `
        -Subject "CN=hubpay-dev-$psp" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyLength 2048 `
        -NotAfter (Get-Date).AddYears(2)

    $secure = ConvertTo-SecureString -String $Password -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure | Out-Null
    Export-Certificate -Cert $cert -FilePath $caPath | Out-Null

    Remove-Item -Path "Cert:\CurrentUser\My\$($cert.Thumbprint)" -ErrorAction SilentlyContinue
    Write-Host "Gerado: $pfxPath"
}

Write-Host ""
Write-Host "Defina as passwords no ambiente, por exemplo:"
Write-Host '  $env:HUBPAY_SIBS_CERT_PASSWORD = "hubpay-dev"'
Write-Host "Ative mTLS em appsettings: HubPay:Sibs:MutualTls:Enabled = true"
