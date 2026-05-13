#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
DOTNET_BIN="$DOTNET_ROOT/dotnet"
TOOLS_DIR="$DOTNET_ROOT/tools"
GLOBAL_JSON="$ROOT_DIR/global.json"
FALLBACK_SDK_VERSION="10.0.203"

log() { printf '[codex-setup] %s\n' "$*"; }
fail() { printf '[codex-setup] ERROR: %s\n' "$*" >&2; exit 1; }

mkdir -p "$DOTNET_ROOT"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$TOOLS_DIR:$PATH"

SDK_VERSION="$FALLBACK_SDK_VERSION"
if [[ -f "$GLOBAL_JSON" ]]; then
  parsed="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([0-9.]*\)".*/\1/p' "$GLOBAL_JSON" | head -n1 || true)"
  if [[ -n "$parsed" ]]; then
    SDK_VERSION="$parsed"
  fi
fi
log "SDK objetivo: $SDK_VERSION"

if [[ -x "$DOTNET_BIN" ]]; then
  if timeout 20s "$DOTNET_BIN" --version >/dev/null 2>&1; then
    log "dotnet ya está instalado y funcional: $(timeout 20s "$DOTNET_BIN" --version)"
  else
    log "dotnet existe pero no responde, se reinstalará."
    rm -f "$DOTNET_BIN"
  fi
fi

if [[ ! -x "$DOTNET_BIN" ]]; then
  log "Descargando instalador oficial dotnet-install.sh"
  timeout 180s curl -fsSL --connect-timeout 20 --max-time 120 https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    || fail "No se pudo descargar dotnet-install.sh"
  chmod +x /tmp/dotnet-install.sh

  log "Instalando .NET SDK $SDK_VERSION en $DOTNET_ROOT"
  timeout 600s bash /tmp/dotnet-install.sh --version "$SDK_VERSION" --install-dir "$DOTNET_ROOT" --no-path \
    || fail "Falló la instalación de .NET SDK $SDK_VERSION"
fi

log "Validando dotnet --version"
timeout 20s "$DOTNET_BIN" --version || fail "dotnet --version falló"
log "Validando dotnet --info"
timeout 30s "$DOTNET_BIN" --info || fail "dotnet --info falló"

cd "$ROOT_DIR"
log "dotnet tool restore"
timeout 300s "$DOTNET_BIN" tool restore || fail "dotnet tool restore falló"

log "Export sugerido para shell actual:"
echo "export DOTNET_ROOT='$DOTNET_ROOT'"
echo "export PATH='$DOTNET_ROOT:$TOOLS_DIR:\$PATH'"

log "Setup finalizado correctamente."
