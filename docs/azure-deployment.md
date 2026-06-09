# Deploy KaraokeList to Azure

This guide deploys the **WASM + API** stack to Azure:

| Component | Azure service |
|-----------|----------------|
| **KaraokeList.Web** (Blazor WASM) | **Azure Static Web Apps** (Free tier) |
| **KaraokeList.Api** (JWT + SQL) | **App Service** (Linux, .NET 10) |
| Database | **Azure SQL** (General Purpose serverless) |

Friends sign in on the WASM app; the API validates JWTs and stores catalog + performances in SQL. See [wasm-api-local-dev.md](wasm-api-local-dev.md) and [security-private-access.md](security-private-access.md).

## Why Azure instead of nested subdomains on shared hosting

Azure gives you **managed HTTPS per hostname** (`*.azurewebsites.net` out of the box, custom domains with free App Service / SWA certificates). You avoid Winhost origin-cert limits (e.g. a wildcard `*.johnprideaux.net` not covering `karaoke-api.johnprideaux.net`).

Custom domains that work well with Cloudflare Full (strict):

| Role | Suggested hostname |
|------|-------------------|
| WASM | `https://karaoke.johnprideaux.net` → Static Web App |
| API | `https://api.johnprideaux.net` → API App Service |

Start with `*.azurewebsites.net` URLs, then add custom domains when ready.

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (`az login`)
- .NET 10 SDK
- Node.js (for `@azure/static-web-apps-cli`, WASM deploy)
- An Azure subscription

## 1. Provision infrastructure

```powershell
$rg = "rg-karaokelist"
$location = "eastus"   # SQL + API App Service
az group create --name $rg --location $location

Copy-Item infra/main.parameters.example.json infra/main.parameters.json
# Edit infra/main.parameters.json — set baseName and sqlAdminPassword

az deployment group create `
  --resource-group $rg `
  --template-file infra/main.bicep `
  --parameters infra/main.parameters.json
```

Note the outputs:

| Output | Use |
|--------|-----|
| `apiWebAppDefaultHostName` | API URL → `https://<name>/` |
| `staticWebAppDefaultHostName` | WASM URL → `https://<name>/` |
| `staticWebAppDeploymentToken` | WASM deploy (store in password manager) |
| `sqlServerFqdn` | Migration + SSMS |

`baseName` must be globally unique (e.g. `karaokelist-jp`). Resources created:

- `sql-<baseName>` — Azure SQL server
- `api-<baseName>` — API App Service
- `stapp-<baseName>` — Static Web App (in `eastus2` by default)

## 2. Configure API secrets (portal)

Bicep sets SQL connection string and basic JWT issuer/audience. Add **production secrets** in the API App Service → **Configuration** → Application settings:

| Setting | Value |
|---------|--------|
| `Jwt__Key` | **Required** — 32+ random characters (≠ dev key in repo) |
| `Security__Registration__InviteCode` | **Required** — share only with friends |
| `Security__Registration__AllowRegistration` | `true` until everyone has joined |
| `Cors__Origins__0` | `https://<staticWebAppDefaultHostName>` (no trailing slash) |

After custom domains:

| Setting | Value |
|---------|--------|
| `Cors__Origins__0` | `https://karaoke.johnprideaux.net` |

Optional later: move secrets to **Key Vault** ([deployment-roadmap.md](deployment-roadmap.md) Phase 2a).

## 3. Allow your IP for SQL (one-time admin)

```powershell
az sql server firewall-rule create `
  --resource-group $rg `
  --server sql-<baseName> `
  --name AllowMyIp `
  --start-ip-address <your-public-ip> `
  --end-ip-address <your-public-ip>
```

App Service → SQL is allowed via the `AllowAzureServices` rule in Bicep.

## 4. Migrate catalog data (optional)

If you have data in `KaraokeList/Temp/Karaoke.sqlite3`:

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=tcp:<server>.database.windows.net,1433;Database=KaraokeList;User ID=<admin>;Password=<password>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true;"
dotnet run --project scripts/MigrateSqliteToSqlServer/MigrateSqliteToSqlServer.csproj
```

On first API startup (or after `dotnet ef database update --project KaraokeList.Api`):

1. EF Core migrations apply (Identity + catalog tables)
2. Seed catalog separately if needed — see [database.md](database.md)

## 5. Publish and deploy

### Option A — helper script (recommended)

```powershell
npm install -g @azure/static-web-apps-cli

.\scripts\deploy-azure.ps1 `
  -ResourceGroup $rg `
  -ApiAppName api-<baseName> `
  -StaticWebAppName stapp-<baseName> `
  -ApiBaseUrl "https://api-<baseName>.azurewebsites.net" `
  -StaticWebAppDeploymentToken "<from bicep output>" `
  -SyncfusionKey "<optional; or set SYNCFUSION_KEY env>"
```

The script temporarily patches `wwwroot/appsettings.json` `ApiBaseUrl` at publish time, then restores the dev default.

### Option B — manual steps

**API**

```powershell
dotnet publish KaraokeList.Api -c Release -o ./publish/api
Compress-Archive -Path ./publish/api/* -DestinationPath ./publish/karaokelist-api.zip -Force

az webapp deployment source config-zip `
  --resource-group $rg `
  --name api-<baseName> `
  --src ./publish/karaokelist-api.zip
```

**WASM**

Set production API URL in `KaraokeList.Web/wwwroot/appsettings.json` (or patch only for publish):

```json
{
  "ApiBaseUrl": "https://api-<baseName>.azurewebsites.net"
}
```

```powershell
dotnet publish KaraokeList.Web -c Release -o ./publish/web /p:SyncfusionKey=<key>

swa deploy ./publish/web/wwwroot `
  --deployment-token <token> `
  --env production
```

`wwwroot/staticwebapp.config.json` enables Blazor client-side routing and correct `.wasm` MIME types on Static Web Apps.

## 6. Smoke test

| Check | Expected |
|-------|----------|
| `GET https://api-<baseName>.azurewebsites.net/api/auth/me` | **401** (API healthy, no token) |
| Open WASM URL | Login page loads |
| Register with invite code | JWT issued; My Songs / Log work |

## 7. Custom domains (optional)

### API App Service

1. App Service → **Custom domains** → add `api.johnprideaux.net`
2. Validate DNS (CNAME to `api-<baseName>.azurewebsites.net`)
3. Add **managed certificate** (App Service → TLS/SSL)
4. Update `Cors__Origins__0` if WASM uses a custom domain too
5. Re-publish WASM with `ApiBaseUrl` → `https://api.johnprideaux.net`

### Static Web App

1. SWA → **Custom domains** → add `karaoke.johnprideaux.net`
2. Add DNS CNAME per portal instructions
3. SWA provisions its own managed cert
4. Update API `Cors__Origins__0` → `https://karaoke.johnprideaux.net`

### Cloudflare (if used)

- **Full (strict)** works with Azure-managed origin certs (unlike Winhost shared wildcard limits).
- Bypass cache for API hostname (same rule pattern as [winhost-deployment.md](winhost-deployment.md)).

## Syncfusion license

- Compiled into WASM at **publish** time (`/p:SyncfusionKey=...`), not a server secret.
- Local: user secrets or `-SyncfusionKey` on the deploy script.
- CI: GitHub secret → pipeline publish ([deployment-roadmap.md](deployment-roadmap.md)).

## Cost notes

- **Static Web Apps Free** — sufficient for a friends group.
- **App Service B1** — modest always-on API (~$13/mo region-dependent).
- **Azure SQL serverless** — pauses after 60 min idle; `minCapacity` 0.5 vCore keeps cost low.

## Troubleshooting

| Symptom | Check |
|---------|--------|
| WASM loads, API calls fail (CORS) | `Cors__Origins__0` matches exact WASM origin (scheme + host, no path) |
| Login fails / 401 on all API calls | `Jwt__Key` set on API; WASM `ApiBaseUrl` points at same API host |
| Grids empty after login | SQL schema + catalog migration; API logs in App Service |
| Deep link 404 on WASM | `staticwebapp.config.json` deployed with `wwwroot` |
| Cannot connect to SQL from laptop | Firewall rule for your IP |
| API cannot reach SQL | `AllowAzureServices` rule; connection string in App Service config |
| Redirect loops or wrong scheme behind Cloudflare | API uses forwarded headers (`X-Forwarded-Proto`); Cloudflare SSL mode Full or Full (strict) |

## Related docs

| Doc | Topic |
|-----|--------|
| [deployment-roadmap.md](deployment-roadmap.md) | Key Vault, CI/CD, phased checklist |
| [wasm-api-local-dev.md](wasm-api-local-dev.md) | Local two-process dev |
| [security-private-access.md](security-private-access.md) | Invite code, registration |
| [winhost-deployment.md](winhost-deployment.md) | Alternate host (paused) |
