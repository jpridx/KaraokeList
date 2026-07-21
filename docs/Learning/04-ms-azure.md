# 04 — MS Azure (Resources & Environment)

## Overview

Production KaraokeList runs on Azure as a small multi-resource environment:

| Resource | Purpose |
|----------|---------|
| **Azure Static Web Apps** | Host Blazor WASM static files |
| **App Service (Linux, .NET 10)** | Host the API |
| **Azure SQL (serverless GP)** | Catalog + Identity data |
| **Application Insights + Log Analytics** | Telemetry and logs |

Infrastructure is described in **Bicep** (`infra/main.bicep`). Day-to-day app deploy is via GitHub Actions OIDC, not by re-running Bicep every commit. **Key Vault** is planned in the deployment roadmap but not yet in the Bicep template.

## Major aspects

1. **Split hosting** — static front end and stateful API are different Azure products.
2. **IaC with Bicep** — SQL server/DB, App Service plan/site, SWA, Insights are declared as code.
3. **App settings vs secrets** — connection strings and JWT keys live in App Service configuration (Key Vault is the next hardening step).
4. **System-assigned managed identity** — API web app is identity-enabled for future Key Vault / RBAC use.
5. **Serverless SQL cold start** — idle databases pause; first requests can be slow (ties into client resilience UX).
6. **Environments** — resource group naming (`rg-karaokelist`), base name, and GitHub environment secrets separate prod config from local.
7. **CORS & custom domains** — WASM origin must be allowed by the API; SWA/App Service hostnames need correct redirects for OAuth.

## Code samples

### Sample 1 — Bicep provisions SQL + related resources

```28:64:infra/main.bicep
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}
// ... firewall rule ...
resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  // ...
}
```

### Sample 2 — Deploy workflow targets a named resource group

```yaml
# From .github/workflows/deploy-azure.yml
permissions:
  id-token: write   # OIDC for azure/login
  contents: read
env:
  AZURE_RESOURCE_GROUP: rg-karaokelist
  AZURE_BASE_NAME: karaokelist
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `infra/main.bicep` | ~77–145 | App Insights, App Service plan, API site settings (`ConnectionStrings__DefaultConnection`) |
| `docs/azure-deployment.md` | full | End-to-end provision, secrets, CORS, migrations, domains |
| `docs/deployment-roadmap.md` | ~87–135 | Planned Key Vault secret mapping (`Jwt__Key`, etc.) |

## Exercises

1. **Multiple choice.** Which Azure service hosts the Blazor WASM static files?
   - A) Azure Functions
   - B) Azure Static Web Apps
   - C) Azure Kubernetes Service
   - D) Azure Cosmos DB

2. **Fill in the blank.** The API runs on Azure ________ Service with Linux and .NET 10.

3. **Multiple choice.** Infrastructure-as-code for this project is written in:
   - A) Terraform only
   - B) ARM JSON only
   - C) Bicep
   - D) CloudFormation

4. **Fill in the blank.** Catalog and Identity data persist in ________ SQL.

5. **Multiple choice.** Why can the first API call after idle time feel slow?
   - A) Blazor cannot cache DLLs
   - B) Serverless Azure SQL may cold-start after pausing
   - C) JWT validation always sleeps 30 seconds
   - D) GitHub Actions blocks production traffic

6. **Fill in the blank.** The Bicep file that provisions resources is ________.

7. **Multiple choice.** Key Vault in this project is currently:
   - A) Fully wired in `main.bicep` for all secrets
   - B) Documented as a planned hardening step
   - C) Required for local development
   - D) Replaced by SQLite

8. **Fill in the blank.** Telemetry is collected with Application ________.

9. **Multiple choice.** API app settings use double-underscore names like `ConnectionStrings__DefaultConnection` because:
   - A) Bicep forbids colons
   - B) They map to nested configuration keys (`ConnectionStrings:DefaultConnection`)
   - C) SQL Server requires underscores
   - D) GitHub Actions cannot pass colons

10. **Fill in the blank.** GitHub authenticates to Azure for deploy using ________ (federated identity), not a long-lived password in the YAML.

## Answer key

1. B  
2. App  
3. C  
4. Azure  
5. B  
6. `infra/main.bicep`  
7. B  
8. Insights  
9. B  
10. OIDC  
