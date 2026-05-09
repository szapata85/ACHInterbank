#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DOTNET_VERSION="10.0.203"
DOTNET_CHANNEL="10.0"
DOTNET_INSTALL_DIR="${HOME}/.dotnet"
TOOLS_DIR="${HOME}/.dotnet/tools"

export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
export PATH="${DOTNET_INSTALL_DIR}:${TOOLS_DIR}:${PATH}"

log() { printf "[codex-setup] %s\n" "$*"; }
need_cmd() { command -v "$1" >/dev/null 2>&1; }

install_dotnet() {
  mkdir -p "${DOTNET_INSTALL_DIR}"
  local installer
  installer="$(mktemp)"
  log "Descargando instalador oficial de .NET..."
  if ! curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"; then
    log "No se pudo descargar dotnet-install.sh. Verifique conectividad/red corporativa."
    exit 1
  fi
  chmod +x "${installer}"
  log "Instalando .NET SDK ${DOTNET_VERSION} en ${DOTNET_INSTALL_DIR}"
  "${installer}" --version "${DOTNET_VERSION}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}"
  rm -f "${installer}"
}

CURRENT_SDK=""
if need_cmd dotnet; then
  CURRENT_SDK="$(dotnet --version || true)"
fi

if [[ -z "${CURRENT_SDK}" || "${CURRENT_SDK}" != "${DOTNET_VERSION}" ]]; then
  install_dotnet
else
  log "dotnet detectado (${CURRENT_SDK})."
fi

if ! need_cmd dotnet; then
  log "dotnet no quedó disponible en PATH tras instalación."
  log "Instale manualmente ${DOTNET_VERSION} y exporte DOTNET_ROOT/PATH antes de reintentar."
  exit 1
fi

log "dotnet --info"
dotnet --info

cd "${ROOT_DIR}"
log "dotnet restore ACHInterbank.sln"
dotnet restore ACHInterbank.sln

log "dotnet build ACHInterbank.sln -c Release --no-restore"
dotnet build ACHInterbank.sln -c Release --no-restore

log "dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build

log "dotnet tool restore"
dotnet tool restore

log "dotnet tool run dotnet-ef --version"
dotnet tool run dotnet-ef --version

log "Setup y validación finalizados correctamente."
