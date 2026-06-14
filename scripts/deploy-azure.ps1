# Publish and deploy KaraokeList.Api + KaraokeList.Web to Azure App Service + Static Web Apps.
# Run from repo root after provisioning infra/main.bicep.
#
# Example:
#   .\scripts\deploy-azure.ps1 `
#     -ResourceGroup rg-karaokelist `
#     -ApiAppName api-karaokelist-jp `
#     -StaticWebAppName stapp-karaokelist-jp `
#     -ApiBaseUrl https://api-karaokelist-jp.azurewebsites.net `
#     -StaticWebAppDeploymentToken <from bicep output> `
#     -SyncfusionKey <optional; or user secrets / env SYNCFUSION_KEY>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$ApiAppName,

    [Parameter(Mandatory = $true)]
    [string]$StaticWebAppName,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$StaticWebAppDeploymentToken,

    [string]$SyncfusionKey = $env:SYNCFUSION_KEY,

    [string]$PublishRoot = "publish"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$apiOut = Join-Path $PublishRoot "api"
$webOut = Join-Path $PublishRoot "web"
$apiZip = Join-Path $PublishRoot "karaokelist-api.zip"

Write-Host "Publishing API..."
if (Test-Path $apiOut) { Remove-Item $apiOut -Recurse -Force }
dotnet publish KaraokeList.Api/KaraokeList.Api.csproj -c Release -o $apiOut

Write-Host "Publishing WASM (ApiBaseUrl=$ApiBaseUrl)..."
if (Test-Path $webOut) { Remove-Item $webOut -Recurse -Force }
$appsettingsPath = "KaraokeList.Web/wwwroot/appsettings.json"
$appsettingsBackup = "$appsettingsPath.bak-deploy"
Copy-Item $appsettingsPath $appsettingsBackup -Force
try {
    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $json.ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
    $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding utf8

    $publishArgs = @(
        "publish", "KaraokeList.Web/KaraokeList.Web.csproj",
        "-c", "Release",
        "-o", $webOut
    )
    if ($SyncfusionKey) {
        $publishArgs += "/p:SyncfusionKey=$SyncfusionKey"
    }
    dotnet @publishArgs
}
finally {
    Move-Item $appsettingsBackup $appsettingsPath -Force
}

Write-Host "Zipping API for App Service (tar — Linux-safe paths)..."
if (Test-Path $apiZip) { Remove-Item $apiZip -Force }
Push-Location $apiOut
try {
    tar -a -c -f (Join-Path $repoRoot $apiZip) *
}
finally {
    Pop-Location
}

Write-Host "Deploying API to $ApiAppName..."
az webapp deploy `
    --resource-group $ResourceGroup `
    --name $ApiAppName `
    --src-path $apiZip `
    --type zip `
    --timeout 600

$swaCli = Get-Command swa -ErrorAction SilentlyContinue
if (-not $swaCli) {
    Write-Host ""
    Write-Host "Static Web Apps CLI (swa) not found. Install with:"
    Write-Host "  npm install -g @azure/static-web-apps-cli"
    Write-Host ""
    Write-Host "Then deploy WASM:"
    Write-Host "  swa deploy $webOut/wwwroot --deployment-token <token> --env production"
    exit 0
}

Write-Host "Deploying WASM to $StaticWebAppName..."
swa deploy (Join-Path $webOut "wwwroot") `
    --deployment-token $StaticWebAppDeploymentToken `
    --env production

Write-Host "Done. Smoke test API: curl https://$ApiAppName.azurewebsites.net/api/auth/me (expect 401)"
