# Vercel Sandbox image for Git, SQLite, Linux/Bash/Vim, Kubernetes manifest checks and
# Claude Code hook exercises. One image keeps the number of registry repositories small.
FROM debian:bookworm-slim
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      bash coreutils findutils grep sed gawk tar gzip vim git sqlite3 jq python3 python3-yaml \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /workspace && chmod 0777 /workspace
WORKDIR /workspace
