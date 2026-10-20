FROM debian:bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssh-server openssh-client sshpass ca-certificates bash coreutils \
    && rm -rf /var/lib/apt/lists/*

COPY docker/achcol-phase8-sftp/entrypoint.sh /usr/local/bin/phase8-sftp-entrypoint.sh
RUN chmod +x /usr/local/bin/phase8-sftp-entrypoint.sh

EXPOSE 22
ENTRYPOINT ["/usr/local/bin/phase8-sftp-entrypoint.sh"]
