#!/usr/bin/env bash
set -euo pipefail

: "${PHASE8_SFTP_USERNAME:?PHASE8_SFTP_USERNAME is required}"
: "${PHASE8_SFTP_PASSWORD:?PHASE8_SFTP_PASSWORD is required}"
PHASE8_SFTP_HOST="${PHASE8_SFTP_HOST:-phase8-sftp}"
PHASE8_SFTP_PORT="${PHASE8_SFTP_PORT:-22}"
work="${PHASE8_ACTOR_WORKDIR:-/tmp/phase8-goanywhere}"
mkdir -p "$work/picked" "$work/known-hosts"
known_hosts="$work/known-hosts/known_hosts"

prepare_host_key() {
  : > "$known_hosts"
  for _ in $(seq 1 60); do
    if ssh-keyscan -p "$PHASE8_SFTP_PORT" "$PHASE8_SFTP_HOST" >> "$known_hosts" 2>/dev/null; then
      return 0
    fi
    sleep 1
  done
  echo "SFTP host did not become reachable" >&2
  return 1
}

sftp_batch() {
  local batch="$1"
  SSHPASS="$PHASE8_SFTP_PASSWORD" sshpass -e sftp \
    -P "$PHASE8_SFTP_PORT" \
    -o BatchMode=no \
    -o StrictHostKeyChecking=yes \
    -o UserKnownHostsFile="$known_hosts" \
    -b - "$PHASE8_SFTP_USERNAME@$PHASE8_SFTP_HOST" <<< "$batch"
}

list_input() {
  sftp_batch $'ls -1 /input\nbye' 2>/dev/null | sed -n '/\.env$/p' | awk '{print $NF}'
}

pickup_all() {
  prepare_host_key
  local name
  while IFS= read -r name; do
    [[ -z "$name" || "$name" == .* ]] && continue
    sftp_batch "get /input/$name $work/picked/$name
rm /input/$name
bye" >/dev/null
    sha256sum "$work/picked/$name" | awk -v n="$name" '{print "PICKUP file=" n " sha256=" $1}'
  done < <(list_input)
}

deliver() {
  local source="$1"
  local remote_name="$2"
  [[ -f "$source" ]] || { echo "Fixture not found: $source" >&2; return 2; }
  [[ "$remote_name" == *.OUT.env || "$remote_name" == *.RET.env ]] || {
    echo "Remote name must end in .OUT.env or .RET.env" >&2
    return 2
  }
  prepare_host_key
  local tmp="/output/.${remote_name}.$$.tmp"
  sftp_batch "put $source $tmp
rename $tmp /output/$remote_name
bye" >/dev/null
  sha256sum "$source" | awk -v n="$remote_name" '{print "DELIVERY file=" n " sha256=" $1}'
}

case "${1:-}" in
  pickup-all)
    pickup_all
    ;;
  deliver)
    [[ $# -eq 3 ]] || { echo "usage: $0 deliver <local-fixture> <remote-name>" >&2; exit 2; }
    deliver "$2" "$3"
    ;;
  daemon)
    prepare_host_key
    echo "Phase 8 GoAnywhere simulator ready; no automatic pickup is performed."
    exec sleep infinity
    ;;
  *)
    echo "usage: $0 {daemon|pickup-all|deliver <local-fixture> <remote-name>}" >&2
    exit 2
    ;;
esac
