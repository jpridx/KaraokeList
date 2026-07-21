# 03 — CI/CD

## Overview

KaraokeList uses **GitHub Actions** for continuous integration and Azure deployment. Pull requests and pushes to `master` build and test; production deploys are selective (API vs WASM) based on which paths changed, authenticated to Azure with **OIDC** (no long-lived deploy passwords in the workflow).

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `.github/workflows/ci.yml` | PR + push `master` | Restore, build, unit + integration tests |
| `.github/workflows/deploy-azure.yml` | Push `master` + manual | Path-filter → test gate → deploy API/WASM |
| `.github/dependabot.yml` | Schedule | Dependency update PRs |

## Major aspects

1. **CI always tests** — build Release, run Api.Tests, Web.Tests, IntegrationTests.
2. **SQL Server service container** — integration tests need a real SQL instance in CI.
3. **Secrets in GitHub / Azure** — Syncfusion key, JWT, connection strings are never committed.
4. **Path filters** — only deploy API when `KaraokeList.Api/**` (or Shared) changes; same idea for WASM.
5. **OIDC to Azure** — `id-token: write` + federated credentials replace stored service-principal secrets.
6. **Smoke checks** — post-deploy hit `/api/version` and WASM HTTP 200.
7. **Concurrency groups** — cancel in-progress CI runs for the same ref to save minutes.

## Code samples

### Sample 1 — CI build and three test projects

```22:73:.github/workflows/ci.yml
jobs:
  build-and-test:
    runs-on: ubuntu-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        # ...
    steps:
      # ... checkout, setup-dotnet, restore, wait for SQL ...
      - name: Build
        env:
          SYNCFUSION_KEY: ${{ secrets.SYNCFUSION_KEY }}
        run: dotnet build KaraokeList.sln -c Release --no-restore /p:SyncfusionKey="$SYNCFUSION_KEY" /p:SourceRevisionId=${{ github.sha }}

      - name: Test API unit tests
        run: dotnet test KaraokeList.Api.Tests/KaraokeList.Api.Tests.csproj -c Release --no-build --verbosity minimal

      - name: Test Web unit tests
        run: dotnet test KaraokeList.Web.Tests/KaraokeList.Web.Tests.csproj -c Release --no-build --verbosity minimal

      - name: Test API integration tests
        run: dotnet test KaraokeList.Api.IntegrationTests/KaraokeList.Api.IntegrationTests.csproj -c Release --no-build --verbosity minimal --settings KaraokeList.Api.IntegrationTests/integration.runsettings
```

### Sample 2 — Selective deploy via path filters

```yaml
# From .github/workflows/deploy-azure.yml (changes job)
- uses: dorny/paths-filter@v4
  with:
    filters: |
      api:
        - 'KaraokeList.Api/**'
        - 'KaraokeList.Shared/**'
      wasm:
        - 'KaraokeList.Web/**'
        - 'KaraokeList.Shared/**'
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `.github/workflows/deploy-azure.yml` | ~79–262 | API deploy: OIDC login, SQL firewall, EF migrate, zip deploy, smoke |
| `.github/workflows/deploy-azure.yml` | ~277–364 | WASM deploy: patch `ApiBaseUrl`, publish, `swa deploy` |
| `docs/github-actions.md` | full | Human-facing pipeline docs and OIDC setup notes |

## Exercises

1. **Multiple choice.** Which workflow runs on every pull request to `master`?
   - A) `deploy-azure.yml` only
   - B) `ci.yml`
   - C) Dependabot only
   - D) Playwright workflow

2. **Fill in the blank.** Integration tests in CI use a Docker image of ________ Server.

3. **Multiple choice.** Why does the deploy workflow request `id-token: write`?
   - A) To push git tags
   - B) To obtain an OIDC token for Azure login without a stored password
   - C) To publish NuGet packages
   - D) To enable Dependabot

4. **Fill in the blank.** The GitHub Action that decides whether API or WASM changed is commonly ________ (path filter action).

5. **Multiple choice.** Syncfusion license in CI comes from:
   - A) Committed `appsettings.json`
   - B) `secrets.SYNCFUSION_KEY`
   - C) Azure Key Vault only
   - D) The SQL connection string

6. **Fill in the blank.** After API deploy, a smoke check typically hits ________.

7. **Multiple choice.** If only `KaraokeList.Web/**` changes, the deploy workflow should:
   - A) Always redeploy API and WASM
   - B) Prefer deploying WASM (and skip unnecessary API deploy)
   - C) Skip all jobs
   - D) Only run Dependabot

8. **Fill in the blank.** `concurrency.cancel-in-progress: true` means overlapping CI runs for the same ref are ________.

9. **Multiple choice.** EF database migrations during deploy are applied against:
   - A) LocalDB on the runner’s laptop
   - B) The Azure SQL database for the environment
   - C) SQLite in `temp_sqlite_inspect`
   - D) No migrations are ever applied

10. **Fill in the blank.** Dependency update PRs are managed by ________.

## Answer key

1. B  
2. SQL (Microsoft SQL / MSSQL)  
3. B  
4. `dorny/paths-filter`  
5. B  
6. `/api/version`  
7. B  
8. cancelled  
9. B  
10. Dependabot  
