#!/usr/bin/env bash
# Creates the three Pathway Fly.io apps and sets their secrets in one pass.
#
# Run from the repository root after `fly auth login`:
#   scripts/fly-bootstrap.sh
#
# Secrets you are prompted for are read without echo and passed straight to `fly secrets set`;
# nothing is written to disk or shell history. The broker/API shared secret is generated here,
# so it never needs to be copied anywhere. Safe to re-run: existing apps are reused and secrets
# are overwritten with the values you enter (`--stage` defers the restart until the next deploy).
set -euo pipefail

ORG="${FLY_ORG:-personal}"
APPS=(pathway-broker pathway-auth pathway-api)

command -v fly >/dev/null || { echo "Install flyctl first: https://fly.io/docs/flyctl/install/" >&2; exit 1; }
command -v openssl >/dev/null || { echo "openssl is required to generate secrets." >&2; exit 1; }
fly auth whoami >/dev/null 2>&1 || { echo "Run 'fly auth login' first." >&2; exit 1; }

prompt_secret() {
  local name="$1" value=""
  while [ -z "$value" ]; do
    read -r -s -p "$name: " value
    echo >&2
  done
  printf '%s' "$value"
}

echo "== Creating apps in org '$ORG' (existing apps are reused)"
for app in "${APPS[@]}"; do
  if fly apps list --org "$ORG" --json | grep -q "\"Name\": *\"$app\""; then
    echo "   $app exists"
  else
    fly apps create "$app" --org "$ORG"
  fi
done

echo
echo "== Secrets"
echo "Paste values from Neon and Modal. Input is hidden."
echo
echo "Pathway database (Neon project 'pathway', DIRECT connection string, postgresql://...):"
PATHWAY_DATABASE_URL="$(prompt_secret DATABASE_URL)"
echo "Keycloak database (Neon project 'pathway-auth'). Enter the JDBC form:"
echo "  jdbc:postgresql://<direct-host>/<database>?sslmode=require"
KC_DB_URL="$(prompt_secret KC_DB_URL)"
KC_DB_USERNAME="$(prompt_secret KC_DB_USERNAME)"
KC_DB_PASSWORD="$(prompt_secret KC_DB_PASSWORD)"
echo "Modal token (create a NEW one after revoking the old token):"
MODAL_TOKEN_ID="$(prompt_secret MODAL_TOKEN_ID)"
MODAL_TOKEN_SECRET="$(prompt_secret MODAL_TOKEN_SECRET)"

RUNNER_SHARED_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
ANALYTICS_ADMIN_KEY="$(openssl rand -base64 48 | tr -d '\n')"

fly secrets set --stage --app pathway-broker \
  MODAL_TOKEN_ID="$MODAL_TOKEN_ID" \
  MODAL_TOKEN_SECRET="$MODAL_TOKEN_SECRET" \
  RUNNER_SHARED_SECRET="$RUNNER_SHARED_SECRET"

fly secrets set --stage --app pathway-auth \
  KC_DB_URL="$KC_DB_URL" \
  KC_DB_USERNAME="$KC_DB_USERNAME" \
  KC_DB_PASSWORD="$KC_DB_PASSWORD"

fly secrets set --stage --app pathway-api \
  DATABASE_URL="$PATHWAY_DATABASE_URL" \
  EVALUATOR_SHARED_SECRET="$RUNNER_SHARED_SECRET" \
  ANALYTICS_ADMIN_KEY="$ANALYTICS_ADMIN_KEY"

echo
echo "== Done. Secrets are staged and apply on the first deploy."
echo "The analytics dashboard key was generated for you. Print it once with:"
echo "  fly ssh console --app pathway-api -C 'printenv ANALYTICS_ADMIN_KEY'   (after the first deploy)"
echo
echo "Next:"
echo "  1. Create a deploy token and add it to GitHub (Settings > Environments > production > FLY_API_TOKEN):"
echo "       fly tokens create org --org $ORG"
echo "  2. Migrate data (scripts/migrate-databases.sh), then run the 'Deploy to Fly.io' workflow."
echo "  3. fly certs add pathway-auth.orangecastle.net --app pathway-auth"
echo "     fly certs add pathway-api.orangecastle.net --app pathway-api"
