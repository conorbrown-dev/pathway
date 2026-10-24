# Vercel Sandbox image for React/TypeScript exercises (type-checked with tsc, no network).
FROM node:22-bookworm-slim
RUN npm install -g typescript@^6 \
 && apt-get update && apt-get install -y --no-install-recommends grep \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /workspace && chmod 0777 /workspace
# Learner code runs as this unprivileged user via /usr/local/bin/as-learner (see ../as-learner).
COPY --chmod=0755 as-learner /usr/local/bin/as-learner
RUN groupadd --gid 10001 learner \
 && useradd --uid 10001 --gid 10001 --create-home --shell /bin/sh learner \
 && command -v setpriv
WORKDIR /workspace
