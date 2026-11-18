// Where the JWT is kept between page loads. localStorage can throw (private mode, storage disabled),
// so every access is guarded: without storage the user simply has to log in again after a reload.

const TOKEN_KEY = 'clinic.token'

export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token: string): void {
  try {
    localStorage.setItem(TOKEN_KEY, token)
  } catch {
    // Storage unavailable: the session lives only in memory for this page load.
  }
}

export function clearToken(): void {
  try {
    localStorage.removeItem(TOKEN_KEY)
  } catch {
    // Nothing to clear.
  }
}

// The name in the token is the one the user had when they logged in. After they change their name in "Hesabım" the
// token cannot change, so the new name is remembered next to it (with the user id, so it can never belong to someone
// else) and shown until the next login, which issues a token with the new name.
const NAME_KEY = 'clinic.displayName'

export function getDisplayName(userId: string): string | null {
  try {
    const stored = localStorage.getItem(NAME_KEY)
    if (!stored) return null
    const parsed = JSON.parse(stored) as { userId?: string; name?: string }
    return parsed.userId === userId && parsed.name ? parsed.name : null
  } catch {
    return null
  }
}

export function setDisplayName(userId: string, name: string): void {
  try {
    localStorage.setItem(NAME_KEY, JSON.stringify({ userId, name }))
  } catch {
    // Storage unavailable: the new name is shown until the page is reloaded.
  }
}

export function clearDisplayName(): void {
  try {
    localStorage.removeItem(NAME_KEY)
  } catch {
    // Nothing to clear.
  }
}
