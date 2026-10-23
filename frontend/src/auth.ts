import { createAuth0Client, type Auth0Client } from '@auth0/auth0-spa-js'

// Auth0 (OIDC authorization code + PKCE). Configure with VITE_AUTH0_DOMAIN, VITE_AUTH0_CLIENT_ID
// and VITE_AUTH0_AUDIENCE (the API identifier the Pathway API validates as its audience).
const domain = import.meta.env.VITE_AUTH0_DOMAIN?.trim()
const clientId = import.meta.env.VITE_AUTH0_CLIENT_ID?.trim()
const audience = import.meta.env.VITE_AUTH0_AUDIENCE?.trim()

export const authConfigured = Boolean(domain && clientId && audience)

export type AuthAccount = { token: string; displayName: string; email: string; subject: string }

type AppState = { returnTo?: string }

let clientPromise: Promise<Auth0Client> | null = null
let latestToken: string | null = null

function client(): Promise<Auth0Client> {
  if (!authConfigured) return Promise.reject(new Error('Sign-in is not configured.'))
  clientPromise ??= createAuth0Client({
    domain: domain!,
    clientId: clientId!,
    authorizationParams: { redirect_uri: window.location.origin, audience },
    // Rotating refresh tokens keep learners signed in across reloads without relying on
    // third-party cookies, which Safari and other browsers block for silent iframe renewal.
    useRefreshTokens: true,
    cacheLocation: 'localstorage',
  })
  return clientPromise
}

/** The access token from the most recent sign-in or refresh, for synchronous header builders. */
export const currentToken = () => latestToken

export async function initializeAuth(): Promise<AuthAccount | null> {
  if (!authConfigured) return null
  const auth0 = await client()

  const query = new URLSearchParams(window.location.search)
  if (query.has('state') && (query.has('code') || query.has('error'))) {
    let returnTo = '/'
    try {
      const result = await auth0.handleRedirectCallback<AppState>()
      returnTo = result.appState?.returnTo ?? '/'
    } finally {
      // Drop the one-time code from the address bar, restore the page the learner started on,
      // and let the router (which listens for popstate) load it.
      window.history.replaceState({}, document.title, returnTo.startsWith('/') ? returnTo : '/')
      window.dispatchEvent(new PopStateEvent('popstate'))
    }
  }

  if (!(await auth0.isAuthenticated())) return null
  const token = await auth0.getTokenSilently()
  if (!token) return null
  latestToken = token
  const user = await auth0.getUser()
  return {
    token,
    displayName: user?.name ?? user?.nickname ?? 'Learner',
    email: user?.email ?? '',
    subject: user?.sub ?? 'unknown',
  }
}

/** Returns a valid access token, refreshing it when it is close to expiry. */
export async function refreshToken(): Promise<string | null> {
  if (!authConfigured) return null
  latestToken = (await (await client()).getTokenSilently()) ?? null
  return latestToken
}

export async function login(register = false): Promise<void> {
  const auth0 = await client()
  await auth0.loginWithRedirect<AppState>({
    appState: { returnTo: window.location.pathname },
    authorizationParams: register ? { screen_hint: 'signup' } : undefined,
  })
}

export async function logout(): Promise<void> {
  latestToken = null
  if (!authConfigured) return
  await (await client()).logout({ logoutParams: { returnTo: window.location.origin } })
}
