# Blazor WASM + API local development

## Projects

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WebAssembly UI (Syncfusion Grid + DropDowns only) |
| `KaraokeList.Api` | ASP.NET Core Web API, EF Identity, SQL catalog |
| `KaraokeList.Shared` | DTOs shared between Web and Api |
| `KaraokeList` | Legacy Blazor Server app (reference; not used for WASM path) |

## Run locally

Terminal 1 — API:

```powershell
cd KaraokeList.Api
dotnet run --launch-profile http
```

Listens on **http://localhost:5299**.

Terminal 2 — WASM client:

```powershell
cd KaraokeList.Web
dotnet run
```

Listens on **http://localhost:5262** (or the port in `Properties/launchSettings.json`).  
`wwwroot/appsettings.json` points `ApiBaseUrl` at the API.

## Syncfusion packages (Web only)

Pinned to **33.1.44** (must match your license key version):

- `Syncfusion.Blazor.Grid` 33.1.44
- `Syncfusion.Blazor.DropDowns` 33.1.44
- `Syncfusion.Blazor.Themes` 33.1.44

Sample/demo pages from the old Server template are not included.

### Remove the trial license banner

The yellow Syncfusion popup means no license key was registered. **Do not put the key in `wwwroot` or any tracked file** — those JSON files are served to the browser and can be committed by mistake.

Use **.NET User Secrets** (stored under your user profile, outside the repo). Blazor WASM cannot read user secrets in the browser, so each **build** generates a gitignored `SyncfusionLicenseKey.g.cs` from your secrets (see `SyncfusionLicense.targets`). The key is compiled into the app — it does not appear as `appsettings.secrets.json` in the Network tab.

1. Get a key from [Syncfusion Community License](https://www.syncfusion.com/sales/communitylicense) or your [Syncfusion account downloads](https://www.syncfusion.com/account/downloads).
2. From the repo root:
   ```powershell
   dotnet user-secrets set "SyncfusionKey" "<your-license-key>" --project KaraokeList.Web
   ```
3. **Rebuild** (required after changing secrets):
   ```powershell
   dotnet build KaraokeList.Web/KaraokeList.Web.csproj
   dotnet run --project KaraokeList.Web
   ```
4. Hard-refresh the browser (Ctrl+F5).

To verify: `dotnet user-secrets list --project KaraokeList.Web`. The generated `appsettings.secrets.json` is gitignored and must never be committed.

For CI/production, pass `/p:SyncfusionKey=...` at build time instead of user secrets. Note: the key is still present in the published WASM client (normal for Syncfusion in the browser).

## Auth

- Register/login call `api/auth/register` and `api/auth/login`
- JWT is stored in browser local storage and sent on catalog API calls
- Development: invite code not required (`KaraokeList.Api/appsettings.Development.json`)
