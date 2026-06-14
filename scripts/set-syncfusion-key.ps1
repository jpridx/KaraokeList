param(
    [Parameter(Mandatory = $true)]
    [string]$Key,
    [ValidateSet("33.1.44", "33.2.4")]
    [string]$PackageVersion = "33.1.44"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$webProject = Join-Path $repoRoot "KaraokeList.Web\KaraokeList.Web.csproj"

Write-Host "Setting SyncfusionKey in user secrets (not in git)..."
& dotnet user-secrets set "SyncfusionKey" $Key.Trim() --project $webProject

Write-Host "Pinning Syncfusion packages to $PackageVersion..."
Push-Location (Join-Path $repoRoot "KaraokeList.Web")
& dotnet add package Syncfusion.Blazor.Grid --version $PackageVersion | Out-Null
& dotnet add package Syncfusion.Blazor.DropDowns --version $PackageVersion | Out-Null
Pop-Location

Write-Host "Building (embeds key in gitignored SyncfusionLicenseKey.g.cs)..."
& dotnet build $webProject

Write-Host ""
Write-Host "Done. Run: dotnet run --project KaraokeList.Web"
Write-Host "Then hard-refresh the browser (Ctrl+F5)."
Write-Host "Package version: $PackageVersion — must match your license key."
