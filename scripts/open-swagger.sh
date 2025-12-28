#!/bin/sh
set -e

url="${1:-https://localhost:8081}"

if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$url"
elif command -v open >/dev/null 2>&1; then
  open "$url"
elif command -v powershell.exe >/dev/null 2>&1; then
  powershell.exe Start-Process "$url"
else
  echo "Abra el navegador manualmente en: $url"
fi
