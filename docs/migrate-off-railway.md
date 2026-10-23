# Migrating Pathway off Railway

Target: **Vercel** (frontend), **Fly.io** (API, Keycloak, evaluator broker), **Neon** (both Postgres databases).
Reason: Railway's October 2026 platform outage, which Railway tied to a security incident in which environment variables may have been read by a third party.

| Piece | From (Railway) | To |
| --- | --- | --- |
| Frontend (Vite SPA) | `Web` + nginx | Vercel project, root directory `frontend` (`frontend/vercel.json`) |
| API (.NET 10) | `Api` | Fly app `pathway-api` (`backend/Pathway.Api/fly.toml`) |
| Keycloak 26.1 | `KeyCloak` | Fly app `pathway-auth` (`infra/keycloak/fly.toml`) |
| Modal evaluator broker | `ModalBroker` (private) | Fly app `pathway-broker`, private network only (`modal-broker/fly.toml`) |
| Pathway database | `Postgres` | Neon project `pathway` |
| Keycloak database | `Keycloak Auth Database` | Separate Neon project `pathway-auth` |
| Legacy .NET evaluator | `Evaluator` | **Not migrated.** The API's `EVALUATOR_URL` points at the broker. Confirm before deleting. |

Public hostnames after the move:
- `pathway.orangecastle.net` → Vercel (unchanged URL for learners).
- `pathway-api.orangecastle.net` → Fly `pathway-api`.
- `pathway-auth.orangecastle.net` → Fly `pathway-auth`. This hostname becomes the token issuer. Keeping it on your own domain means the next hosting move will not change the issuer again.

## 1. Rotate secrets first

Treat every value stored in Railway as exposed. Generate **new** values; never copy old ones to the new hosts.

| Secret | Action |
| --- | --- |
| Modal token (`MODAL_TOKEN_ID` / `MODAL_TOKEN_SECRET`) | Revoke in the Modal dashboard, then create a new token. Check Modal usage for unexpected sandboxes. |
| Broker shared secret (`RUNNER_SHARED_SECRET` = API `EVALUATOR_SHARED_SECRET`) | `openssl rand -base64 48`; set the same new value on both apps. |
| `ANALYTICS_ADMIN_KEY` | `openssl rand -base64 48` |
| Database passwords | New Neon databases get new credentials automatically. |
| Keycloak admin password | Change it in the Keycloak admin console after the migrated realm is running. |
| Railway API / project tokens | Railway already invalidated them. Revoke the Railway GitHub App's repository access once you're off. |
| GitHub | Review the security log and repo deploy keys for unexpected access. |

## 2. Provision

**Neon** (already installed on your Vercel account; free tier):
1. The Neon organization is managed by Vercel, so create databases from **Vercel → Storage → Create Database → Neon**, not from the Neon console or API. Create two: `pathway` and `pathway-auth`, region **US East 1 (N. Virginia)** (next to Fly `iad`), **PostgreSQL 18**. Both Railway databases run PostgreSQL 18; a dump from 18 will not restore into an older server. You don't need to connect either database to a Vercel project; only the Fly apps use them.
2. Use the **direct** (non-pooled) connection strings. Keycloak's schema migrations need a session connection, and traffic is low enough that pooling isn't needed.
3. Note the Postgres version Neon gives you. It must be equal to or newer than Railway's for `pg_restore`.

> Neon's free tier suspends idle computes and includes 100 compute-hours per project per month. Keycloak keeps sessions in memory, so its database can suspend while idle. Watch usage in the first week; if the auth database stays awake, move that project to a paid plan.

**Fly.io** (`brew install flyctl` or see fly.io/docs/flyctl/install, then `fly auth login`). `scripts/fly-bootstrap.sh` does everything in this block interactively, generating the shared secrets locally; the commands below show what it runs:

```sh
fly apps create pathway-broker
fly apps create pathway-auth
fly apps create pathway-api

fly secrets set --app pathway-broker MODAL_TOKEN_ID=... MODAL_TOKEN_SECRET=... RUNNER_SHARED_SECRET=...
fly secrets set --app pathway-auth \
  KC_DB_URL='jdbc:postgresql://<neon-auth-direct-host>/<db>?sslmode=require' \
  KC_DB_USERNAME=... KC_DB_PASSWORD=...
fly secrets set --app pathway-api \
  DATABASE_URL='postgresql://<user>:<password>@<neon-pathway-direct-host>/<db>?sslmode=require' \
  EVALUATOR_SHARED_SECRET=... ANALYTICS_ADMIN_KEY=...
```

`DATABASE_URL` may be the URI Neon displays. The API converts it, defaults the port to 5432 when the URI omits one (Neon's format), and always requires TLS.

Create a deploy token for GitHub Actions (`fly tokens create deploy --org personal` or an org token) and save it as the repository secret **`FLY_API_TOKEN`** in a GitHub environment named `production`.

## 3. Migrate the data

`scripts/migrate-databases.sh` performs the steps below and verifies exact row counts per table. It refuses to run with a `pg_dump` older than 18 or to restore into a non-empty database.

Railway is the only copy of user accounts (Keycloak) and learner progress, so this has to wait until Railway's databases are reachable again. The outage is a natural write freeze: nothing new is being written.

1. In Railway, enable public networking (TCP proxy) on both Postgres services and copy their connection URLs.
2. Use a `pg_dump` client at least as new as the server (`pg_dump --version`).

```sh
# Pathway app data: progress, submissions, analytics, workspaces
pg_dump --format=custom --no-owner --no-privileges "$RAILWAY_PATHWAY_URL" --file pathway.dump
pg_restore --no-owner --no-privileges --dbname "$NEON_PATHWAY_URL" pathway.dump

# Keycloak: users, credentials, realm configuration
pg_dump --format=custom --no-owner --no-privileges "$RAILWAY_KEYCLOAK_URL" --file keycloak.dump
pg_restore --no-owner --no-privileges --dbname "$NEON_AUTH_URL" keycloak.dump
```

Sanity-check row counts on both sides (for example `select count(*) from user_entity;` for Keycloak, and the progress table for Pathway). Keep the dump files somewhere private until the move is verified, then delete them; they contain password hashes.

Keycloak must run the **same version (26.1.0)** that wrote the database. The Dockerfile pins it.

## 4. Deploy the backend

Deploy in dependency order, either by running the "Deploy to Fly.io" workflow manually (`workflow_dispatch`) or from a terminal:

```sh
fly deploy --config modal-broker/fly.toml modal-broker
fly deploy --config infra/keycloak/fly.toml infra/keycloak
fly deploy --config backend/Pathway.Api/fly.toml backend/Pathway.Api
```

Then attach the hostnames (Fly prints the DNS records it needs):

```sh
fly certs add pathway-auth.orangecastle.net --app pathway-auth
fly certs add pathway-api.orangecastle.net --app pathway-api
```

Add those records in **Cloudflare**, which hosts DNS for `orangecastle.net`. Set them to **DNS only** (grey cloud). Fly and Vercel issue their own TLS certificates and need to see requests directly; Cloudflare's proxy in front of them can break certificate issuance and adds a second TLS hop. Until DNS resolves, Keycloak's issuer (`KC_HOSTNAME`) won't match and logins will fail. That is expected.

## 5. Deploy the frontend on Vercel

> **Status (2026-10-23): done except DNS.** The Vercel project `pathway` exists, is linked to `conorbrown-dev/pathway` (root `frontend`, production branch `main`) and has the four `VITE_*` variables. `pathway.orangecastle.net` is attached, and a production build of `8fb38e8` is live at `https://pathway-kappa-ecru.vercel.app`; it shows "API unavailable" until the Fly API is up. Only the Cloudflare change in step 3 remains. The steps below are kept for reference.

1. In Vercel, **Add New → Project**, import `conorbrown-dev/pathway`, and set **Root Directory** to `frontend`. `vercel.json` supplies the build settings, SPA rewrites and headers. The Vercel account must be able to see the GitHub repository; grant access to that repo in the Vercel GitHub App if it isn't listed.
2. Set these environment variables for Production. Vite embeds them at build time, so redeploy after changing any of them:

   ```
   VITE_API_BASE_URL=https://pathway-api.orangecastle.net
   VITE_KEYCLOAK_URL=https://pathway-auth.orangecastle.net
   VITE_KEYCLOAK_REALM=pathway
   VITE_KEYCLOAK_CLIENT_ID=pathway-web
   VITE_DONATION_URL=<optional>
   ```
3. The domain `pathway.orangecastle.net` is already attached to the Vercel project. To cut over, change the Cloudflare record `pathway` to **CNAME `2d345992e92faf49.vercel-dns-017.com`**, set to **DNS only** (grey cloud). Today it's a proxied record pointing at Railway.

> Vercel's Hobby plan is limited to non-commercial use. Check whether the donation link puts Pathway outside that, and move to Pro if needed.

## 6. Verify

Run the production smoke tests from `frontend/`:

```sh
PLAYWRIGHT_BASE_URL=https://pathway.orangecastle.net \
PLAYWRIGHT_API_BASE_URL=https://pathway-api.orangecastle.net \
npm run test:e2e -- e2e/staging-api.spec.ts e2e/production-auth.spec.ts
```

`production-auth.spec.ts` skips itself unless `PLAYWRIGHT_AUTH_STORAGE_STATE` points to a saved signed-in session (see `docs/e2e-testing.md`).

Then check by hand:

- `https://pathway-api.orangecastle.net/health` and `/ready` return 200.
- An existing user can sign in, and their completed lessons are still completed.
- A guest can answer the first quiz.
- A C# code exercise passes. This proves the private API → broker → Modal path and the new shared secret.
- The Keycloak admin console works on the new hostname. Then change the admin password.

## 7. Decommission Railway

After a week without problems:
1. Delete the Railway services and databases.
2. Remove the Railway GitHub App's access to the repository.
3. Delete the `railway.toml` files and the Railway sections of the README and `docs/railway-*.md`.

## Rollback

Until step 7, Railway's services and data are untouched. Rolling back means pointing `pathway.orangecastle.net` back to Railway's CNAME target. Any progress written to Neon after the cutover would not be in Railway's copy.
