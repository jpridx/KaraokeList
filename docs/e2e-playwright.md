# Playwright E2E tests

Browser tests for the **Blazor WASM** app (`KaraokeList.E2E`). They exercise real page load, routing, and auth — things unit/bUnit tests cannot cover.

## What Playwright does here

1. Launches **Chromium** (headless by default).
2. Opens your WASM site (default `http://localhost:5262`).
3. Clicks and types like a user (login, mobile nav, etc.).

Tests use **Microsoft.Playwright.Xunit** — same xUnit runner as the rest of the repo.

## One-time setup

From the repo root:

```powershell
dotnet build KaraokeList.E2E/KaraokeList.E2E.csproj
powershell -ExecutionPolicy Bypass -File KaraokeList.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

Apply EF migrations if you have not already:

```powershell
dotnet ef database update --project KaraokeList.Api
```

## Run E2E tests (easiest — auto-start servers)

The test run **starts Api + Web for you**, waits until they respond, runs tests, then stops them:

```powershell
dotnet test KaraokeList.E2E/KaraokeList.E2E.csproj
```

Uses:

| App | Profile | URL |
|-----|---------|-----|
| API | `e2e` | `http://localhost:5299` with invite-only registration (`appsettings.E2E.json`) |
| Web | `e2e` | `http://localhost:5262` with `appsettings.E2E.json` → API over HTTP |

First run can take **1–2 minutes** (build + WASM boot + Blazor download).

## Run with servers you already started (faster iteration)

Terminal 1:

```powershell
dotnet run --project KaraokeList.Api --launch-profile e2e
```

Terminal 2 (build WASM for the **E2E** environment so `appsettings.E2E.json` is used — not Development’s HTTPS API URL):

```powershell
dotnet build KaraokeList.Web/KaraokeList.Web.csproj /p:WasmApplicationEnvironmentName=E2E /p:SyncfusionKey=""
dotnet run --project KaraokeList.Web --launch-profile e2e --no-build
```

Terminal 3:

```powershell
$env:KARAOKE_E2E_MANUAL = "true"
dotnet test KaraokeList.E2E/KaraokeList.E2E.csproj --no-build
```

## See the browser (debugging)

```powershell
$env:HEADED = "1"
dotnet test KaraokeList.E2E/KaraokeList.E2E.csproj --filter "Authenticated_user_can_open_my_songs"
```

Playwright slows actions slightly when headed so you can follow along.

## Current tests

| Test | What it checks |
|------|----------------|
| `Login_page_loads_for_anonymous_user` | WASM boots, `/login` shows Sign in |
| `User_can_sign_in_through_login_form` | Register via API → clear storage → type email/password on `/login` → home |
| `Authenticated_user_can_open_my_songs` | Register via API → store JWT → home → **My Songs** (mobile viewport) |
| `Authenticated_user_can_log_a_performance` | Seed song via API → `/log?songId=` → add venue → **Save performance** |
| `Log_song_picker_matches_dont_query_for_apostrophe_title` | Log combobox finds apostrophe titles when searching without the apostrophe |
| `Invite_link_allows_a_friend_to_register` | Signed-in user opens **Invite friends** → friend registers via invite URL in a fresh browser context |
| `Authenticated_user_can_open_song_detail_and_log_again` | My Songs row → song detail → **Log again** → second performance saved |
| `My_songs_switches_between_repertoire_want_to_sing_and_working_up` | List chips filter **My repertoire**, **Want to sing**, and **Working up** |
| `Tonight_dashboard_shows_recent_log_and_links_to_log_page` | Save via Log → home **Tonight** shows recent log → tap opens `/log?songId=` |
| `Authenticated_user_can_add_new_song_and_log_it` | Log **+ New song** → add artist → add song → save performance |
| `Authenticated_user_can_edit_a_performance_on_my_performances` | **My performances** → **Edit** opens form (date/venue/key) → **Cancel** closes it |
| `Authenticated_user_can_delete_a_performance_on_my_performances` | **My performances** → **Delete** → confirm → row removed |

Other tests seed auth through the API (fast setup) then exercise WASM UI flows. Login form coverage uses the real Sign in button and JWT storage path the app uses after a normal login.

## Environment variables

| Variable | Purpose |
|----------|---------|
| `KARAOKE_E2E_MANUAL=true` | Do not start Api/Web; use already-running servers |
| `KARAOKE_E2E_WEB_URL` | Override WASM base URL (default `http://localhost:5262`) |
| `KARAOKE_E2E_API_URL` | Override API base URL (default `http://localhost:5299`) |
| `HEADED=1` | Show browser window |

## CI

E2E is **not** in GitHub Actions yet (needs SQL + two long-running processes + Playwright browsers). Unit and integration tests still run on every PR.

## Adding a test

1. Add a class in `KaraokeList.E2E/` with `[Collection(E2eCollection.Name)]` and inherit `PageTest`.
2. Use `Page.GotoAsync("/route")` and `Page.GetByRole(...)` / `Page.Locator(...)`.
3. Wait for Blazor: `await Page.WaitForSelectorAsync("...")` with a generous timeout (WASM cold start).

See [mobile-ux.md](mobile-ux.md) for routes and flows worth automating next (Log performance, invite link register).
