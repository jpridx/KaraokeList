# Deploy KaraokeList to Azure

This guide deploys the Blazor Server app to **Azure App Service (Linux)** with a single **Azure SQL Database (serverless)** database for both ASP.NET Identity and karaoke catalog data.

## Architecture

| Component | Azure service |
|-----------|----------------|
| Web app | App Service (Linux, .NET 10) |
| Database | Azure SQL Database (General Purpose serverless, `GP_S_Gen5`) |
| Auth | ASP.NET Core Identity (email/password registration) |

Friends sign up at `/Account/Register` with a **private invite code**, sign in, then manage venues and performances on protected pages. See [security-private-access.md](security-private-access.md).

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- .NET 10 SDK
- An Azure subscription

## 1. Provision infrastructure

```powershell
$rg = "rg-karaokelist"
$location = "eastus"
az group create --name $rg --location $location

az deployment group create `
  --resource-group $rg `
  --template-file infra/main.bicep `
  --parameters infra/main.parameters.json
```

Copy `infra/main.parameters.example.json` to `infra/main.parameters.json` and set:

- `baseName` — globally unique prefix (e.g. `karaokelist-jp`)
- `sqlAdminPassword` — strong password (store in a password manager)

Note the outputs: `webAppDefaultHostName`, `sqlServerFqdn`.

## 2. Migrate existing SQLite data (optional)

If you have data in `KaraokeList/Temp/Karaoke.sqlite3`, run the migration tool after the database exists and firewall rules allow your IP:

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=tcp:<server>.database.windows.net,1433;Database=KaraokeList;User ID=<admin>;Password=<password>;Encrypt=True;TrustServerCertificate=False;"
dotnet run --project scripts/MigrateSqliteToSqlServer/MigrateSqliteToSqlServer.csproj
```

## 3. Publish and deploy the web app

```powershell
cd KaraokeList
dotnet publish -c Release -o ./publish

cd publish
Compress-Archive -Path * -DestinationPath ../karaokelist.zip -Force
cd ..

az webapp deployment source config-zip `
  --resource-group $rg `
  --name app-<your-baseName> `
  --src karaokelist.zip
```

On first startup the app will:

1. Apply EF Core Identity migrations
2. Create karaoke catalog tables from `scripts/azure-sql/001-karaoke-schema.sql`

## 4. Configure App Service settings

The Bicep template sets `ConnectionStrings__DefaultConnection` and `ASPNETCORE_ENVIRONMENT=Production`.

Optional settings in the Azure portal (Configuration → Application settings):

| Setting | Purpose |
|---------|---------|
| `Security__Registration__InviteCode` | **Required** — long random secret; share only with friends |
| `Security__Registration__AllowRegistration` | `false` after your group has accounts |
| `Security__Registration__RequireInviteCode` | `true` in production |
| `SyncfusionKey` | Remove Syncfusion trial watermark (pipeline secret at build time only — not in git) |
| `Identity__RequireConfirmedAccount` | Set `true` if you add real email (SendGrid, etc.) |

## 5. Allow your IP for SQL administration (one-time)

```powershell
az sql server firewall-rule create `
  --resource-group $rg `
  --server sql-<baseName> `
  --name AllowMyIp `
  --start-ip-address <your-public-ip> `
  --end-ip-address <your-public-ip>
```

App Service access is enabled via the `AllowAzureServices` firewall rule in Bicep.

## Authentication options for friends

### Recommended default: ASP.NET Identity + invite code (included)

- Each friend registers with email, password, and the **invite code** you set in App Service configuration.
- After everyone has an account, set `Security__Registration__AllowRegistration` to `false`.
- `RequireConfirmedAccount` is **off** so sign-up works without SMTP.
- All catalog routes require sign-in; login has lockout and per-IP rate limits.

Full checklist: [security-private-access.md](security-private-access.md).

### Optional upgrades

| Option | Best for | Effort |
|--------|----------|--------|
| **Microsoft / Google social login** | Friends who prefer OAuth | Medium — add `.AddAuthentication().AddMicrosoftAccount()` / `.AddGoogle()` and App Service auth settings |
| **Microsoft Entra External ID** | Consumer Microsoft/Google/Apple accounts at scale | Higher — separate tenant, redirect URIs |
| **Invite-only registration** | Private group | Medium — disable public register, admin creates users |
| **Azure App Service Authentication (Easy Auth)** | Quick Microsoft login in front of the app | Low for Microsoft only; pairs with Entra app registration |

For a small friend group, **Identity with open registration** is usually enough. Tighten later with an invite code on `Register.razor` or by disabling registration after the core group has accounts.

## Cost notes (serverless SQL)

- Database pauses after `autoPauseDelay` minutes of inactivity (60 in Bicep).
- `minCapacity` 0.5 vCore keeps cost low while allowing burst.
- App Service B1 is a modest always-on option; scale down to Free F1 for demos (cold starts).

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Login works, grids empty | Catalog schema — verify `Venues` table exists in Azure SQL |
| Cannot connect to SQL from App Service | Firewall `AllowAzureServices`; connection string in App Service config |
| EF migration errors on startup | SQL admin permissions; database exists |
| Redirect loop on HTTP | Use HTTPS URL from App Service (`httpsOnly: true`) |
