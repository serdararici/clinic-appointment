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
