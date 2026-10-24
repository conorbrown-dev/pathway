# Vercel Sandbox image for Rust exercises.
FROM rust:1.97-slim
RUN mkdir -p /workspace && chmod 0777 /workspace
# Learner code runs as this unprivileged user via /usr/local/bin/as-learner (see ../as-learner).
COPY --chmod=0755 as-learner /usr/local/bin/as-learner
RUN groupadd --gid 10001 learner \
 && useradd --uid 10001 --gid 10001 --create-home --shell /bin/sh learner \
 && command -v setpriv
WORKDIR /workspace
