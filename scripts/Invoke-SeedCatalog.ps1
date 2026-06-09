# Run scripts/seed-catalog.sql against a SQL Server database.
# Uses sqlcmd (SQL Server tools). Install: https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility
#
# Examples:
#   .\scripts\Invoke-SeedCatalog.ps1 -Server "karaokelist.database.windows.net" -Database "KaraokeList-Dev"
#   $env:KARAOKE_SQL_CONNECTION = "Server=...;Database=...;..."
#   .\scripts\Invoke-SeedCatalog.ps1

param(
    [string]$Server,
    [string]$Database,
    [string]$ConnectionString = $env:KARAOKE_SQL_CONNECTION,
    [string]$ScriptPath = (Join-Path $PSScriptRoot "seed-catalog.sql"),
    [switch]$UseAzureActiveDirectory
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Error "sqlcmd not found. Install SQL Server command-line tools or run seed-catalog.sql manually in SSMS / Azure Data Studio."
}

if (-not $Server -and $ConnectionString) {
    foreach ($part in $ConnectionString.Split(';')) {
        $kv = $part.Split('=', 2)
        if ($kv.Length -ne 2) { continue }
        $key = $kv[0].Trim()
        $val = $kv[1].Trim()
        switch -Regex ($key) {
            '^Server$' { $Server = $val -replace '^tcp:', '' }
            '^Data Source$' { $Server = $val -replace '^tcp:', '' }
            '^Database$' { $Database = $val }
            '^Initial Catalog$' { $Database = $val }
        }
    }
    if ($ConnectionString -match 'Authentication=Active Directory') {
        $UseAzureActiveDirectory = $true
    }
}

if (-not $Server -or -not $Database) {
    Write-Error "Provide -Server and -Database, or set KARAOKE_SQL_CONNECTION."
}

if (-not (Test-Path $ScriptPath)) {
    Write-Error "Seed script not found: $ScriptPath"
}

$sqlcmdArgs = @("-S", $Server, "-d", $Database, "-i", $ScriptPath, "-b")
if ($UseAzureActiveDirectory) {
    $sqlcmdArgs += @("-G")
}

Write-Host "Running $ScriptPath against $Database on $Server ..."
& sqlcmd @sqlcmdArgs
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE"
}
Write-Host "Seed complete."
