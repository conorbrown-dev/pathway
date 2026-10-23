# Vercel Sandbox image for React/TypeScript exercises (type-checked with tsc, no network).
FROM node:22-bookworm-slim
RUN npm install -g typescript@^6 \
 && apt-get update && apt-get install -y --no-install-recommends grep \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /workspace && chmod 0777 /workspace
WORKDIR /workspace
