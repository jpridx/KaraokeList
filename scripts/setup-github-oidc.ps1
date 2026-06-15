# One-time Azure AD app registration for GitHub Actions OIDC deploys.
# Grants Contributor on a resource group so workflows can deploy API + SWA.
#
# Example (from repo root):
#   .\scripts\setup-github-oidc.ps1
#   .\scripts\setup-github-oidc.ps1 -GitHubRepo "jpridx/KaraokeList" -SetGitHubSecrets
#
# Prerequisites: az login, Contributor rights on the subscription/resource group.

param(
    [string]$ResourceGroup = "rg-karaokelist",

    [string]$GitHubRepo = "jpridx/KaraokeList",

    [string]$AppDisplayName = "github-karaokelist-deploy",

    [switch]$SetGitHubSecrets
)

$ErrorActionPreference = "Stop"

function Invoke-AzOptional {
    param(
        [Parameter(Mandatory)][string[]]$Args,
        [switch]$AllowEmpty
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & az @Args 2>&1
        $exitCode = $LASTEXITCODE
        $detail = ($output | Out-String).Trim()

        if ($exitCode -ne 0) {
            return [PSCustomObject]@{
                Ok     = $false
                Value  = $null
                Detail = $detail
            }
        }

        if (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($detail)) {
            return [PSCustomObject]@{
                Ok     = $false
                Value  = $null
                Detail = "Command exited 0 but returned no output."
            }
        }

        return [PSCustomObject]@{
            Ok     = $true
            Value  = $detail
            Detail = $null
        }
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Invoke-AzRequired {
    param(
        [Parameter(Mandatory)][string[]]$Args,
        [Parameter(Mandatory)][string]$FailureMessage,
        [switch]$AllowEmpty
    )

    $result = Invoke-AzOptional -Args $Args -AllowEmpty:$AllowEmpty
    if (-not $result.Ok) {
        if ($result.Detail) {
            throw "$FailureMessage`n$result.Detail"
        }
        throw $FailureMessage
    }
    return $result.Value
}

function Get-FederatedCredentialNames {
    param([Parameter(Mandatory)][string]$AppId)

    $result = Invoke-AzOptional -Args @(
        "ad", "app", "federated-credential", "list",
        "--id", $AppId,
        "-o", "json"
    ) -AllowEmpty

    if (-not $result.Ok -or [string]::IsNullOrWhiteSpace($result.Value)) {
        return @()
    }

    $items = $result.Value | ConvertFrom-Json
    if ($null -eq $items) {
        return @()
    }
    if ($items -isnot [System.Collections.IEnumerable] -or $items -is [string]) {
        $items = @($items)
    }

    return @($items | ForEach-Object { $_.name })
}

function New-FederatedCredential {
    param(
        [Parameter(Mandatory)][string]$AppId,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Issuer,
        [Parameter(Mandatory)][string]$Subject
    )

    $payload = @{
        name      = $Name
        issuer    = $Issuer
        subject   = $Subject
        audiences = @("api://AzureADTokenExchange")
    }
    $jsonPath = Join-Path $env:TEMP "github-oidc-$Name-$AppId.json"
    try {
        $json = $payload | ConvertTo-Json -Depth 5
        [System.IO.File]::WriteAllText($jsonPath, $json, [System.Text.UTF8Encoding]::new($false))
        Invoke-AzRequired -Args @(
            "ad", "app", "federated-credential", "create",
            "--id", $AppId,
            "--parameters", "@$jsonPath"
        ) -FailureMessage "Failed to create federated credential '$Name'." -AllowEmpty
    }
    finally {
        Remove-Item -Path $jsonPath -Force -ErrorAction SilentlyContinue
    }
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) is required. Install: https://learn.microsoft.com/cli/azure/install-azure-cli"
}

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    throw "Run 'az login' first."
}

$subscriptionId = $account.id
$tenantId = $account.tenantId
$scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup"

Write-Host "Subscription: $($account.name) ($subscriptionId)"
Write-Host "Resource group: $ResourceGroup"
Write-Host "GitHub repo:    $GitHubRepo"
Write-Host ""

$rgExists = az group exists --name $ResourceGroup
if ($rgExists -ne "true") {
    throw "Resource group '$ResourceGroup' not found. Provision infra first (infra/main.bicep)."
}

# Reuse existing app registration if display name already exists.
$existingApp = Invoke-AzOptional -Args @("ad", "app", "list", "--display-name", $AppDisplayName, "--query", "[0].appId", "-o", "tsv")
if ($existingApp.Ok -and $existingApp.Value) {
    $appId = $existingApp.Value.Trim()
    Write-Host "Using existing app registration '$AppDisplayName' ($appId)"
}
else {
    Write-Host "Creating app registration '$AppDisplayName'..."
    $appId = Invoke-AzRequired -Args @("ad", "app", "create", "--display-name", $AppDisplayName, "--query", "appId", "-o", "tsv") `
        -FailureMessage "Failed to create app registration '$AppDisplayName'."
}

# App registration can exist without a service principal (e.g. partial prior run).
$existingSp = Invoke-AzOptional -Args @("ad", "sp", "show", "--id", $appId, "--query", "id", "-o", "tsv")
if ($existingSp.Ok -and $existingSp.Value) {
    $spObjectId = $existingSp.Value.Trim()
}
else {
    Write-Host "Creating service principal..."
    $spObjectId = Invoke-AzRequired -Args @("ad", "sp", "create", "--id", $appId, "--query", "id", "-o", "tsv") `
        -FailureMessage "Failed to create service principal for app $appId."
}

$issuer = "https://token.actions.githubusercontent.com"
$federatedCredentials = @(
    @{
        name    = "github-production"
        subject = "repo:${GitHubRepo}:environment:production"
        note    = "Deploy Azure job (uses GitHub environment: production)"
    },
    @{
        name    = "github-master"
        subject = "repo:${GitHubRepo}:ref:refs/heads/master"
        note    = "Optional: workflows on master without an environment"
    }
)

$existingCredentialNames = Get-FederatedCredentialNames -AppId $appId

foreach ($cred in $federatedCredentials) {
    if ($existingCredentialNames -contains $cred.name) {
        Write-Host "Federated credential '$($cred.name)' already exists - skipping."
        continue
    }

    Write-Host "Adding federated credential '$($cred.name)' ($($cred.note))..."
    New-FederatedCredential -AppId $appId -Name $cred.name -Issuer $issuer -Subject $cred.subject
}

$existingRole = Invoke-AzOptional -Args @(
    "role", "assignment", "list",
    "--assignee-object-id", $spObjectId,
    "--scope", $scope,
    "--role", "Contributor",
    "--query", "[0].id",
    "-o", "tsv"
)

if ($existingRole.Ok -and $existingRole.Value) {
    Write-Host "Contributor role on $ResourceGroup already assigned."
}
else {
    Write-Host "Assigning Contributor on $ResourceGroup..."
    Invoke-AzRequired -Args @(
        "role", "assignment", "create",
        "--assignee-object-id", $spObjectId,
        "--assignee-principal-type", "ServicePrincipal",
        "--role", "Contributor",
        "--scope", $scope
    ) -FailureMessage "Failed to assign Contributor on $ResourceGroup." -AllowEmpty
}

Write-Host ""
Write-Host "=== GitHub repository secrets (Settings -> Secrets and variables -> Actions) ==="
Write-Host ""
Write-Host "  AZURE_CLIENT_ID       = $appId"
Write-Host "  AZURE_TENANT_ID       = $tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID = $subscriptionId"
Write-Host '  SYNCFUSION_KEY        = <your Syncfusion license key>'
Write-Host ''

if ($SetGitHubSecrets) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) {
        Write-Warning "GitHub CLI (gh) not found. Install https://cli.github.com/ and run: gh auth login"
    }
    else {
        Write-Host "Setting Azure OIDC secrets via gh..."
        gh secret set AZURE_CLIENT_ID --body $appId
        gh secret set AZURE_TENANT_ID --body $tenantId
        gh secret set AZURE_SUBSCRIPTION_ID --body $subscriptionId
        Write-Host "Set AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID."
        Write-Host "Set SYNCFUSION_KEY manually: gh secret set SYNCFUSION_KEY"
    }
}

Write-Host 'Next steps:'
Write-Host '  1. Add secrets above to GitHub (or re-run with -SetGitHubSecrets after gh auth login).'
Write-Host '  2. Commit and push .github/workflows/ to master.'
Write-Host '  3. Actions - Deploy Azure - Run workflow (or push to master).'
Write-Host '  4. Docs: docs/github-actions.md'
