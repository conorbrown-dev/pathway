# Hosting

Pathway runs on **Vercel** (frontend, API, evaluator broker and the code sandboxes), **Neon** (Postgres) and **Auth0** (sign-in). It moved off Railway in October 2026, after Railway's platform outage and security incident, with a fresh start: no data was migrated.

```
browser ──► pathway.orangecastle.net (Vercel project "pathway", Vite SPA)
              │  /api/*  rewritten to ─►  Vercel project "pathway-api" (.NET 10 container)
              │                              ├─► Neon "pathway" (PostgreSQL 18)
              │                              └─► Vercel project "pathway-broker" (Node container) ─► Vercel Sandbox microVMs
              └─ sign-in redirect ─►  Auth0 (OIDC + PKCE); the API validates Auth0 access tokens
```

| Piece | Where | Source | Builds on |
| --- | --- | --- | --- |
| Frontend | Vercel `pathway` | `frontend/` (`vercel.json`) | push to `main` (previews for branches) |
| API | Vercel `pathway-api`, Container Runtime | `backend/Pathway.Api/Dockerfile.vercel` | push to `main` |
| Evaluator broker | Vercel `pathway-broker`, Container Runtime | `sandbox-broker/Dockerfile.vercel` | push to `main` |
| Code sandboxes | Vercel Sandbox, images in `pathway-broker`'s Container Registry | `sandbox-broker/images/*/Dockerfile` | Sandbox images workflow on `main` |
| Database | Neon `pathway` (`twilight-frog-83291776`), us-east-1, free plan | created via Vercel → Storage | n/a |
| Sign-in | Auth0 tenant `dev-rkpp04u8ylrlove1` (US), shared with other apps; Pathway users live in their own `Pathway-Users` connection | dashboard configuration below | n/a |

Each Vercel project skips deployments when nothing in its own directory changed. The API and broker run in `iad1`, next to the Neon database.

## How requests flow

- The frontend calls the API on its **own origin**: `vercel.json` rewrites `/api/*` to `pathway-api-carl-brown87-9852.vercel.app`. There is no CORS in production and no extra DNS record.
- Vercel's Container Runtime runs each container as a Vercel Function. It **scales to zero after 5 minutes without traffic**, so the first request after an idle period waits while .NET starts and Neon resumes. Later requests are fast.
- The API listens on `PORT=8080`; the image runs as the non-root `app` user. `TRUST_PROXY_HEADERS=true` makes per-IP rate limits use the client address Vercel forwards.
- The broker's production URL is public but refuses every request without the shared secret header (`X-Pathway-Runner-Key`, compared in constant time). If the secret isn't configured, it refuses everything.
- Vercel Authentication protects **preview** deployments of the API and broker, and all deployments of the frontend except its custom domain.

## Configuration

### Vercel `pathway-api`

| Variable | Value | Who sets it |
| --- | --- | --- |
| `PORT` | `8080` | set |
| `TRUST_PROXY_HEADERS` | `true` | set |
| `CORS_ORIGINS` | `https://pathway.orangecastle.net` | set |
| `EVALUATOR_URL` | `https://pathway-broker-carl-brown87-9852.vercel.app` | set |
| `DATABASE_URL`, `DATABASE_URL_UNPOOLED` | injected by connecting the Neon `pathway` store to this project; the API prefers the unpooled URL | Storage → Connect Project |
| `AUTH_AUTHORITY` | `https://<auth0-tenant-domain>/` (with the trailing slash) | after Auth0 setup |
| `AUTH_AUDIENCE` | `https://api.pathway.orangecastle.net` (the Auth0 API identifier) | after Auth0 setup |
| `EVALUATOR_SHARED_SECRET` | `openssl rand -base64 48`; **Sensitive** | owner |
| `ANALYTICS_ADMIN_KEY` | `openssl rand -base64 48`; **Sensitive** | owner |

### Vercel `pathway-broker`

| Variable | Value | Who sets it |
| --- | --- | --- |
| `PORT` | `8080` | set |
| `RUNNER_SHARED_SECRET` | the **same** value as the API's `EVALUATOR_SHARED_SECRET`; **Sensitive** | owner |
| `SANDBOX_TEAM_ID` | `team_tZoUI4vE4sNosuZNPZ31M3oO` | set |
| `SANDBOX_PROJECT_ID` | `prj_kV3eCUOhADugL70Dqiuty8xWXPFD` (this project, which owns the sandbox images) | set |

The broker authenticates to Vercel Sandbox with the OIDC token Vercel attaches to each request it receives (`x-vercel-oidc-token`), so it stores no Vercel credential. Off Vercel, for example on the Fly fallback, set `VERCEL_TOKEN` instead.

**Modal is no longer used.** Revoke the Modal token that lived on Railway, and delete the Modal account if nothing else uses it.

### Vercel `pathway` (frontend)

Vite embeds these at build time; redeploy after changing them.

| Variable | Value |
| --- | --- |
| `VITE_API_BASE_URL` | `https://pathway.orangecastle.net` |
| `VITE_AUTH0_DOMAIN` | the Auth0 tenant domain (or its custom domain) |
| `VITE_AUTH0_CLIENT_ID` | the Auth0 Single Page Application's client ID (public, not a secret) |
| `VITE_AUTH0_AUDIENCE` | `https://api.pathway.orangecastle.net` |
| `VITE_DONATION_URL` | optional |

### Auth0

Pathway shares the Auth0 tenant `dev-rkpp04u8ylrlove1.us.auth0.com` with other products, because the account is at its tenant limit. It is isolated by connection: Pathway accounts exist only in the `Pathway-Users` database, which no other application uses, and the Pathway application offers no other connection. If a tenant frees up, moving Pathway to its own tenant needs only these steps repeated there and the five variables updated.

Current configuration:

| Item | Value |
| --- | --- |
| Application | `Pathway`, Single Page Application, client ID `aTNi1OdbIWAahlq5t6z0rnwp2Dh9YCKX` |
| Callback, logout and web-origin URLs | `https://pathway.orangecastle.net, http://localhost:5173` |
| Refresh tokens | rotation on; 15-day idle and 30-day maximum lifetime |
| Connections enabled for the app | `Pathway-Users` only (email + password). The tenant's shared `Username-Password-Authentication` and `google-oauth2` are switched **off** for Pathway |
| API | `Pathway API`, identifier `https://api.pathway.orangecastle.net`, RS256 |
| API settings | RBAC on, permissions added to the access token, offline access allowed; user-delegated access is per-app, granted to `Pathway` |
| Permissions | `mentor`: grant it to mentors (User Management → Users → Permissions). The API reads roles from the `permissions` claim (`AUTH_ROLE_CLAIM` overrides it) |

Keep these in mind when changing the tenant:

- **Don't enable `Pathway-Users` for other applications**, and don't enable shared connections for Pathway. Either would merge Pathway's accounts with another product's.
- **Brute-force protection** is on by default. Under Authentication → Database → `Pathway-Users` → Password Policy, choose *Good* or stronger.
- **Email:** Auth0's built-in email provider is for testing. Before relying on password resets and verification mail, configure a provider (Resend is installed on the Vercel account and offers SMTP). Email settings are tenant-wide.
- **Login page branding** (Branding → Universal Login) is tenant-wide, so it is shared with the other products.

Signed-in learners are stored as `user:<Auth0 sub>` (for example `user:auth0|65f…`); guests as `guest:<id>`.

## Sandbox images

Learner code runs in Vercel Sandbox microVMs: one per submission, network `deny-all`, 1 vCPU, a 12-second command limit. Images:

| Image | Base | Exercises |
| --- | --- | --- |
| `dotnet` | .NET 10 SDK + Python 3 | C#, Python |
| `rust` | `rust:1.97-slim` | Rust |
| `node` | Node 22 + TypeScript 6 | React / TypeScript |
| `tools` | Debian + git, sqlite3, coreutils, vim, jq, python3-yaml | Git, SQLite, Linux/Bash/Vim, Kubernetes, Claude hooks |

The **Sandbox images** workflow builds all four on every pull request that touches them, and smoke-tests them (tool versions, writable `/workspace`, no network, non-root user). On `main` it publishes them to the `pathway-broker` project's Vercel Container Registry. Publishing uses GitHub OIDC, so no token is stored. It needs, once:

1. **Vercel → Team Settings → OIDC Federation** (or Container Registry access policies): trust GitHub repository `conorbrown-dev/pathway`, workflow `sandbox-images.yml`, branch `main`, with read-write access to Vercel Container Registry for project `pathway-broker`.
2. **GitHub → Settings → Secrets and variables → Actions → Variables**: `VERCEL_TEAM_ID=team_tZoUI4vE4sNosuZNPZ31M3oO`, `VERCEL_TEAM_SLUG=carl-brown87-9852`, `VERCEL_SANDBOX_PROJECT_SLUG=pathway-broker`.
3. Run the workflow once (Actions → Sandbox images → Run workflow), then check that each repository shows **Ready** in the project's Container Registry tab.

**Hobby allowance:** 5,000 sandbox creations (one per code submission), 5 hours of active CPU, 10 concurrent sandboxes, per month. When exceeded, creation pauses until the cycle resets. You are never charged on Hobby, but code exercises can't be graded until the reset. Pro bills about $0.002 per run against its $20 monthly credit.

## DNS (Cloudflare)

`orangecastle.net` is on Cloudflare. Change the `pathway` record to **CNAME `2d345992e92faf49.vercel-dns-017.com`**, set to **DNS only** (grey cloud). Vercel issues the certificate itself and must see requests directly.

## Verify

- `https://pathway.orangecastle.net` loads lessons, and the first request after idle may take a few seconds.
- A guest can answer the first quiz.
- Sign up, sign out, sign in. The page you started on comes back after sign-in.
- A C# code exercise passes while signed in. This exercises the API → broker → Vercel Sandbox path, the shared secret and the published images.

## Fallback: Fly.io

`backend/Pathway.Api/fly.toml` and `sandbox-broker/fly.toml` build the same `Dockerfile.vercel` images, for running always-on (no cold starts) on Fly.io if Vercel's Container Runtime, still in beta, falls short. Deploy with `fly deploy --config <path>/fly.toml <path>`. Set the same variables, use `HOST=::` for the broker on Fly's IPv6 private network, and point `EVALUATOR_URL` at `http://pathway-broker.internal:8080`.

## Retiring Railway

No data lives only on Railway. Once the checks above pass, delete the Railway project, remove the Railway GitHub App's repository access, and delete the `railway.toml` files and `docs/railway-*.md`. The unused Neon `pathway-auth` database (created for Keycloak) can be deleted too.
