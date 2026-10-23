FROM quay.io/keycloak/keycloak:26.1.0

COPY realm-import /opt/keycloak/data/import

# The platform proxy (Fly.io) terminates TLS. Keycloak trusts the X-Forwarded-* headers it
# sets and listens on PORT (default 8080). Set KC_HOSTNAME to the public URL so the token
# issuer is stable regardless of which host serves it.
ENTRYPOINT ["/bin/sh", "-c", "/opt/keycloak/bin/kc.sh start --http-enabled=true --http-port=${PORT:-8080} --proxy-headers=xforwarded --hostname-strict=false --import-realm"]
