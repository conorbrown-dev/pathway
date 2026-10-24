# Vercel Sandbox image for C# and Python exercises .
FROM mcr.microsoft.com/dotnet/sdk:10.0
RUN apt-get update \
 && apt-get install -y --no-install-recommends python3 coreutils \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /workspace && chmod 0777 /workspace
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# Learner code runs as this unprivileged user via /usr/local/bin/as-learner (see ../as-learner).
COPY --chmod=0755 as-learner /usr/local/bin/as-learner
RUN groupadd --gid 10001 learner \
 && useradd --uid 10001 --gid 10001 --create-home --shell /bin/sh learner \
 && command -v setpriv
WORKDIR /workspace
