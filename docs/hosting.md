# Hosting

Pathway runs on **Vercel** (frontend, API and evaluator broker), **Neon** (Postgres) and **Auth0** (sign-in). It moved off Railway in October 2026, after Railway's platform outage and security incident, with a fresh start: no data was migrated.

```
browser ──► pathway.orangecastle.net (Vercel project "pathway", Vite SPA)
              │  /api/*  rewritten to ─►  Vercel project "pathway-api" (.NET 10 container)
              │                              ├─► Neon "pathway" (PostgreSQL 18)
              │                              └─► Vercel project "pathway-broker" (Node container) ─► Modal sandboxes
              └─ sign-in redirect ─►  Auth0 (OIDC + PKCE); the API validates Auth0 access tokens
```

| Piece | Where | Source | Builds on |
| --- | --- | --- | --- |
| Frontend | Vercel `pathway` | `frontend/` (`vercel.json`) | push to `main` (previews for branches) |
| API | Vercel `pathway-api`, Container Runtime | `backend/Pathway.Api/Dockerfile.vercel` | push to `main` |
| Evaluator broker | Vercel `pathway-broker`, Container Runtime | `modal-broker/Dockerfile.vercel` | push to `main` |
| Database | Neon `pathway` (`twilight-frog-83291776`), us-east-1, free plan | created via Vercel → Storage | n/a |
| Sign-in | Auth0 tenant | dashboard configuration below | n/a |

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
| `MODAL_TOKEN_ID`, `MODAL_TOKEN_SECRET` | a **new** Modal token (revoke the one that lived on Railway); **Sensitive** | owner |

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

1. **Application** → Create → *Single Page Application* named `Pathway`.
   - Allowed Callback URLs, Allowed Logout URLs and Allowed Web Origins: `https://pathway.orangecastle.net, http://localhost:5173`.
   - **Refresh Token Rotation**: on. Absolute expiration: on (for example 30 days). The SPA uses rotating refresh tokens so learners stay signed in across reloads without third-party cookies.
2. **APIs** → Create → name `Pathway API`, identifier `https://api.pathway.orangecastle.net`, signing RS256.
   - For mentor badges in the community area: enable **RBAC** and **Add Permissions in the Access Token**, add a permission `mentor`, and grant it to mentors. The API reads roles from the `permissions` claim (`AUTH_ROLE_CLAIM` overrides it).
3. **Security**: keep Brute Force Protection on (the default). Under Authentication → Database → Password Policy, choose *Good* or stronger.
4. **Email**: Auth0's built-in email provider is for testing. Before relying on password resets and verification mail, configure a provider. Resend is already installed on the Vercel account and offers SMTP.
5. Optional **custom domain** (for example `auth.orangecastle.net`). It keeps sign-in on your own domain, and Auth0 requires credit-card verification for it on the free plan. If you add one, use it for `VITE_AUTH0_DOMAIN`, and use `https://<custom-domain>/` for `AUTH_AUTHORITY`, because tokens are then issued by the custom domain.

Signed-in learners are stored as `user:<Auth0 sub>` (for example `user:google-oauth2|123`); guests as `guest:<id>`.

## DNS (Cloudflare)

`orangecastle.net` is on Cloudflare. Change the `pathway` record to **CNAME `2d345992e92faf49.vercel-dns-017.com`**, set to **DNS only** (grey cloud). Vercel issues the certificate itself and must see requests directly.

## Verify

- `https://pathway.orangecastle.net` loads lessons, and the first request after idle may take a few seconds.
- A guest can answer the first quiz.
- Sign up, sign out, sign in. The page you started on comes back after sign-in.
- A C# code exercise passes while signed in. This exercises the API → broker → Modal path and the shared secret.

## Fallback: Fly.io

`backend/Pathway.Api/fly.toml` and `modal-broker/fly.toml` build the same `Dockerfile.vercel` images, for running always-on (no cold starts) on Fly.io if Vercel's Container Runtime, still in beta, falls short. Deploy with `fly deploy --config <path>/fly.toml <path>`. Set the same variables, use `HOST=::` for the broker on Fly's IPv6 private network, and point `EVALUATOR_URL` at `http://pathway-broker.internal:8080`.

## Retiring Railway

No data lives only on Railway. Once the checks above pass, delete the Railway project, remove the Railway GitHub App's repository access, and delete the `railway.toml` files and `docs/railway-*.md`. The unused Neon `pathway-auth` database (created for Keycloak) can be deleted too.
