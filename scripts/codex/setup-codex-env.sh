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

need_cmd() {
  command -v "$1" >/dev/null 2>&1
}

install_dotnet() {
  mkdir -p "${DOTNET_INSTALL_DIR}"
  local installer
  installer="$(mktemp)"
  log "Descargando instalador oficial de .NET..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${installer}"
  chmod +x "${installer}"
  log "Instalando .NET SDK ${DOTNET_VERSION} en ${DOTNET_INSTALL_DIR}"
  "${installer}" --version "${DOTNET_VERSION}" --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}"
  rm -f "${installer}"
}

if need_cmd dotnet; then
  CURRENT_SDK="$(dotnet --version || true)"
else
  CURRENT_SDK=""
fi

if [[ -z "${CURRENT_SDK}" ]]; then
  install_dotnet
elif [[ "${CURRENT_SDK}" != "${DOTNET_VERSION}" ]]; then
  log "dotnet detectado (${CURRENT_SDK}) pero se requiere ${DOTNET_VERSION}. Actualizando SDK..."
  install_dotnet
else
  log "dotnet detectado (${CURRENT_SDK})."
fi

if ! need_cmd dotnet; then
  log "dotnet no quedó disponible en PATH tras instalación."
  exit 1
fi

log "dotnet --info"
dotnet --info

if dotnet tool list -g | awk '{print $1}' | grep -qx 'dotnet-ef'; then
  log "dotnet-ef ya instalado globalmente."
else
  log "Instalando dotnet-ef global..."
  dotnet tool install --global dotnet-ef
fi

log "dotnet ef --version"
dotnet ef --version

if need_cmd node; then
  log "node detectado: $(node --version)"
else
  log "node no detectado. Para frontend Angular instale Node LTS (>=20 recomendado)."
fi

if need_cmd npm; then
  log "npm detectado: $(npm --version)"
else
  log "npm no detectado."
fi

if need_cmd docker; then
  log "docker detectado: $(docker --version)"
else
  log "docker no detectado. PostgreSQL de test requiere Docker/Compose."
fi

log "Setup finalizado."
log "Siguiente paso sugerido: docker compose -f ${ROOT_DIR}/docker-compose.test.yml --env-file ${ROOT_DIR}/.env.test.example up -d"
