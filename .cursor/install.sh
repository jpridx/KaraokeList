#!/usr/bin/env bash
set -euo pipefail

# Idempotent cloud-agent bootstrap: .NET 10 SDK + NuGet restore.
# Runs on every agent start (see .cursor/environment.json). Do not pin snapshots there.

if ! command -v dotnet >/dev/null 2>&1; then
  sudo apt-get update -qq
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-10.0
fi

dotnet restore
dotnet --version
echo "KaraokeList cloud environment ready."
