#!/usr/bin/env bash
# Copies the Pathway and Keycloak databases from Railway to Neon and verifies row counts.
#
#   scripts/migrate-databases.sh
#
# Prompts for four connection URLs (input hidden). Both Railway databases run PostgreSQL 18, so
# pg_dump/pg_restore/psql must be version 18 or newer (e.g. `brew install postgresql@18`). Dumps go to a private temporary directory that
# is deleted on exit, because they contain Keycloak password hashes.
#
# Run it while nothing writes to Railway (during the outage, or after stopping the Railway API
# and Keycloak), and before pointing traffic at the new hosts.
set -euo pipefail

for tool in pg_dump pg_restore psql; do
  command -v "$tool" >/dev/null || { echo "$tool is required (PostgreSQL client tools)." >&2; exit 1; }
done
client_major="$(pg_dump --version | sed -E 's/[^0-9]*([0-9]+).*/\1/')"
if [ "$client_major" -lt 18 ]; then
  echo "pg_dump $client_major is too old: the Railway servers run PostgreSQL 18. Install PostgreSQL 18 client tools." >&2
  exit 1
fi

prompt_secret() {
  local name="$1" value=""
  while [ -z "$value" ]; do
    read -r -s -p "$name: " value
    echo >&2
  done
  printf '%s' "$value"
}

WORK="$(mktemp -d)"
chmod 700 "$WORK"
trap 'rm -rf "$WORK"' EXIT

table_counts() {
  # Exact row counts for every user table, sorted, as "schema.table count". A single read-only
  # query (query_to_xml runs each count) so the check creates nothing that it would then count.
  psql "$1" --no-psqlrc --tuples-only --no-align --field-separator ' ' --quiet -c "
    SELECT schemaname || '.' || relname,
           (xpath('/row/c/text()', query_to_xml(format('SELECT count(*) AS c FROM %I.%I', schemaname, relname), false, true, '')))[1]::text::bigint
    FROM pg_stat_user_tables
    ORDER BY 1"
}

migrate() {
  local label="$1" source="$2" target="$3"
  echo "== $label: dumping"
  pg_dump --format=custom --no-owner --no-privileges --dbname "$source" --file "$WORK/$label.dump"
  echo "== $label: restoring"
  pg_restore --no-owner --no-privileges --exit-on-error --dbname "$target" "$WORK/$label.dump"
  echo "== $label: verifying row counts"
  table_counts "$source" > "$WORK/$label.source"
  table_counts "$target" > "$WORK/$label.target"
  if diff -u "$WORK/$label.source" "$WORK/$label.target"; then
    echo "   $(wc -l < "$WORK/$label.source" | tr -d ' ') tables match"
  else
    echo "!! $label row counts differ (diff above). Do not cut over until resolved." >&2
    return 1
  fi
}

echo "Enter connection URLs (postgresql://...). Use Railway's public TCP proxy URLs and Neon's DIRECT URLs."
RAILWAY_PATHWAY="$(prompt_secret 'Railway Pathway DB')"
NEON_PATHWAY="$(prompt_secret 'Neon pathway DB')"
RAILWAY_AUTH="$(prompt_secret 'Railway Keycloak DB')"
NEON_AUTH="$(prompt_secret 'Neon pathway-auth DB')"

for target in "$NEON_PATHWAY" "$NEON_AUTH"; do
  existing="$(psql "$target" --no-psqlrc --tuples-only --no-align -c "select count(*) from pg_stat_user_tables")"
  if [ "$existing" != "0" ]; then
    echo "A target Neon database already has $existing tables. Restore only into empty databases." >&2
    exit 1
  fi
done

migrate pathway "$RAILWAY_PATHWAY" "$NEON_PATHWAY"
migrate keycloak "$RAILWAY_AUTH" "$NEON_AUTH"

echo
echo "Both databases copied and verified. Next: deploy (Deploy to Fly.io workflow) and DNS."
