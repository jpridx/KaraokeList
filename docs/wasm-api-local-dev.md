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

- `Syncfusion.Blazor.Grid` (includes grid date editing)
- `Syncfusion.Blazor.DropDowns` (dropdowns + autocomplete)
- `Syncfusion.Blazor.Themes`

Sample/demo pages from the old Server template are not included.

### Remove the trial license banner

The yellow Syncfusion popup means no license key was registered. The app already calls `SyncfusionLicenseProvider.RegisterLicense` when `SyncfusionKey` is set.

1. Get a key (free for many personal/small-team use cases):
   - [Syncfusion Community License](https://www.syncfusion.com/sales/communitylicense) — qualify by company size/revenue rules on that page, or
   - Sign in at [Syncfusion downloads](https://www.syncfusion.com/account/downloads) and copy a trial/license key for Essential Studio.
2. In `KaraokeList.Web/wwwroot`, copy the example file:
   ```powershell
   cd KaraokeList.Web\wwwroot
   copy appsettings.local.json.example appsettings.local.json
   ```
3. Open `appsettings.local.json` and replace the placeholder with your key (one long string, no quotes inside the value).
4. Restart the WASM app (`dotnet run` in `KaraokeList.Web`). Hard-refresh the browser (Ctrl+F5).

`appsettings.local.json` is gitignored so the key is not committed. For Azure, set `SyncfusionKey` in the Static Web App / build pipeline the same way you inject `ApiBaseUrl` if you publish WASM with a licensed build.

## Auth

- Register/login call `api/auth/register` and `api/auth/login`
- JWT is stored in browser local storage and sent on catalog API calls
- Development: invite code not required (`KaraokeList.Api/appsettings.Development.json`)
