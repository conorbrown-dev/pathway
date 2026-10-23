# Moving Pathway off Railway

Target: **Vercel** (frontend), **Fly.io** (API, Keycloak, evaluator broker), **Neon** (both Postgres databases).
Reason: Railway's October 2026 platform outage, which Railway tied to a security incident in which environment variables may have been read by a third party.

**Decision (2026-10-23): fresh start.** The only accounts were the owner's, so no data is migrated. The new databases start empty: the API creates its tables on startup, and Keycloak builds the `pathway` realm from `infra/keycloak/realm-import/pathway-realm.json` on first boot. Nothing ever needs to connect to Railway's databases, which avoids touching credentials that may be exposed. `scripts/migrate-databases.sh` is kept, tested, for any future move that does need data.

| Piece | From (Railway) | To | Status |
| --- | --- | --- | --- |
| Frontend (Vite SPA) | `Web` + nginx | Vercel project `pathway`, root `frontend` | **Done**, awaiting DNS |
| Pathway database | `Postgres` | Neon `pathway` (`twilight-frog-83291776`) | **Done**, empty |
| Keycloak database | `Keycloak Auth Database` | Neon `pathway-auth` (`still-band-46977453`) | **Done**, empty |
| API (.NET 10) | `Api` | Fly app `pathway-api` | Owner: steps 2–3 |
| Keycloak 26.1 | `KeyCloak` | Fly app `pathway-auth` | Owner: steps 2–3 |
| Modal evaluator broker | `ModalBroker` (private) | Fly app `pathway-broker`, private network only | Owner: steps 2–3 |
| Legacy .NET evaluator | `Evaluator` | Not moved; `EVALUATOR_URL` points at the broker | Delete with Railway |

Both Neon databases are PostgreSQL 18 in AWS us-east-1 (next to Fly `iad`), on the free plan. Each has the default database `neondb`, owned by `neondb_owner`. They were created through **Vercel → Storage**, because the Neon organization is managed by Vercel and the Neon API refuses direct creation. They are not connected to the Vercel project; only the Fly apps use them.

Public hostnames:
- `pathway.orangecastle.net` → Vercel (unchanged URL for learners).
- `pathway-api.orangecastle.net` → Fly `pathway-api`.
- `pathway-auth.orangecastle.net` → Fly `pathway-auth`. This hostname is the token issuer. Keeping it on your own domain means future hosting moves won't change it.

DNS for `orangecastle.net` is on **Cloudflare**. Every record below should be **DNS only** (grey cloud). Fly and Vercel issue their own TLS certificates and need to see requests directly; Cloudflare's proxy in front of them can block certificate issuance and adds a second TLS hop.

## 1. Rotate secrets

Treat every value stored in Railway as exposed. New values are generated for the new hosts; nothing is copied from Railway.

| Secret | Action |
| --- | --- |
| Modal token | **Revoke the old token** in the Modal dashboard, check usage for unexpected sandboxes, and create a new one for step 2. |
| Broker shared secret, analytics key, Keycloak bootstrap admin | Generated on your machine by `scripts/fly-bootstrap.sh`. |
| Database credentials | New Neon databases have new credentials. |
| Railway API / project tokens | Railway already invalidated them. Remove the Railway GitHub App's repository access once you're off. |
| GitHub | Review the security log and deploy keys for unexpected access. |
| Vercel | Turn on two-factor authentication (the dashboard currently prompts for it). |

## 2. Create the Fly apps and secrets

Install flyctl (`brew install flyctl`, or see fly.io/docs/flyctl/install), run `fly auth login`, then from the repository root:

```sh
scripts/fly-bootstrap.sh
```

It creates `pathway-broker`, `pathway-auth` and `pathway-api` (reusing any that exist) and stages their secrets. It asks for:
- each Neon database's **`DATABASE_URL_UNPOOLED`**: Vercel → Storage → the database → `.env.local` tab → Show secret. It derives Keycloak's JDBC URL and credentials from that string and refuses the pooled (`-pooler`) host, because Keycloak's schema migrations need a direct connection.
- the new Modal token.

Input is hidden, and nothing is written to disk. The script generates the broker/API shared secret, the analytics key and a **temporary Keycloak admin** (`pathway-bootstrap`). It prints the admin password once; save it to your password manager.

Then create a deploy token (`fly tokens create org --org personal`) and save it as **`FLY_API_TOKEN`** in the GitHub environment **`production`** (Settings → Environments). Until it exists, the deploy workflow skips with a notice.

## 3. Deploy the backend

Run **Actions → Deploy to Fly.io → Run workflow**. Every green push to main also deploys automatically. It deploys the broker, then Keycloak, then the API. Alternatively, from a terminal:

```sh
fly deploy --config modal-broker/fly.toml modal-broker
fly deploy --config infra/keycloak/fly.toml infra/keycloak
fly deploy --config backend/Pathway.Api/fly.toml backend/Pathway.Api
```

Then attach the hostnames and add the records Fly prints to Cloudflare (DNS only):

```sh
fly certs add pathway-auth.orangecastle.net --app pathway-auth
fly certs add pathway-api.orangecastle.net --app pathway-api
```

Until DNS resolves, Keycloak's issuer (`KC_HOSTNAME`) won't match and logins fail. That's expected.

## 4. Finish Keycloak setup

On first boot Keycloak imports the realm. CI verifies the import against Keycloak 26.1: registration on, brute-force protection, a 12-character password policy, the PKCE public client, production redirect URIs and web origins, post-logout redirects, and the `pathway-api` audience.

1. Sign in at `https://pathway-auth.orangecastle.net/admin` as `pathway-bootstrap`.
2. In the **master** realm, create a permanent admin user for yourself with a strong password and 2FA, and give it the `admin` role.
3. Delete `pathway-bootstrap`, then: `fly secrets unset --app pathway-auth KC_BOOTSTRAP_ADMIN_USERNAME KC_BOOTSTRAP_ADMIN_PASSWORD`.
4. **Email (follow-up):** the realm has no SMTP server, so "Forgot password" and email verification can't send mail. The Resend integration already installed on Vercel offers SMTP; configure it under Realm settings → Email when you want password resets to work.

## 5. Point the frontend domain at Vercel

The Vercel project is ready: linked to `conorbrown-dev/pathway` (root `frontend`, branch `main`) with the `VITE_*` variables, and `pathway.orangecastle.net` is attached. In Cloudflare, change the record `pathway` to **CNAME `2d345992e92faf49.vercel-dns-017.com`**, DNS only. It's currently a proxied record pointing at Railway.

> Vercel's Hobby plan is limited to non-commercial use. Check whether the donation link puts Pathway outside that, and move to Pro if needed.

## 6. Verify

- `https://pathway-api.orangecastle.net/health` and `/ready` return 200.
- `https://pathway.orangecastle.net` loads lessons.
- A guest can answer the first quiz.
- Registering a new account works. Your old Railway account doesn't exist here; register again.
- A C# code exercise passes while signed in. This proves the private API → broker → Modal path and the new shared secret.

Production smoke tests, from `frontend/`:

```sh
PLAYWRIGHT_BASE_URL=https://pathway.orangecastle.net \
PLAYWRIGHT_API_BASE_URL=https://pathway-api.orangecastle.net \
npm run test:e2e -- e2e/staging-api.spec.ts e2e/production-auth.spec.ts
```

`production-auth.spec.ts` skips itself unless `PLAYWRIGHT_AUTH_STORAGE_STATE` points to a saved signed-in session (see `docs/e2e-testing.md`).

## 7. Decommission Railway

No data lives only on Railway, so it can go as soon as step 6 passes:
1. Delete the Railway project (all services and both databases).
2. Remove the Railway GitHub App's access to the repository.
3. Delete the `railway.toml` files and the Railway sections of the README and `docs/railway-*.md`.

Until then, rolling back means pointing the Cloudflare `pathway` record back at Railway.
