#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "[1/4] dotnet clean"
dotnet clean ACHInterbank.sln

echo "[2/4] removing bin/ and obj/"
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

echo "[3/4] dotnet restore"
dotnet restore ACHInterbank.sln

echo "[4/4] dotnet build"
dotnet build ACHInterbank.sln --no-restore

echo "Done."
