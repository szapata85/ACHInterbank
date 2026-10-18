#!/usr/bin/env bash
set -euo pipefail

: "${PHASE8_SFTP_USERNAME:?PHASE8_SFTP_USERNAME is required}"
: "${PHASE8_SFTP_PASSWORD:?PHASE8_SFTP_PASSWORD is required}"

if ! id "$PHASE8_SFTP_USERNAME" >/dev/null 2>&1; then
  useradd --home-dir / --no-create-home --shell /usr/sbin/nologin "$PHASE8_SFTP_USERNAME"
fi
printf '%s:%s\n' "$PHASE8_SFTP_USERNAME" "$PHASE8_SFTP_PASSWORD" | chpasswd

root="/srv/sftp/$PHASE8_SFTP_USERNAME"
mkdir -p "$root/input" "$root/output" "$root/archive"
chown root:root "$root"
chmod 0755 "$root"
chown -R "$PHASE8_SFTP_USERNAME:$PHASE8_SFTP_USERNAME" "$root/input" "$root/output" "$root/archive"
chmod 0750 "$root/input" "$root/output" "$root/archive"

mkdir -p /run/sshd
ssh-keygen -A >/dev/null 2>&1

cat >/etc/ssh/sshd_config.d/phase8.conf <<CFG
PasswordAuthentication yes
KbdInteractiveAuthentication no
PermitRootLogin no
UsePAM no
AllowUsers ${PHASE8_SFTP_USERNAME}
Match User ${PHASE8_SFTP_USERNAME}
    ChrootDirectory ${root}
    ForceCommand internal-sftp
    AllowTcpForwarding no
    X11Forwarding no
CFG

exec /usr/sbin/sshd -D -e
