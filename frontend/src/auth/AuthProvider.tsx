import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { loginRequest } from '../api/auth'
import { setUnauthorizedHandler } from '../api/client'
import { AuthContext, type AuthContextValue } from './auth-context'
import { decodeToken, isExpired, type SessionUser } from './jwt'
import { clearToken, getToken, setToken } from './session'

const EXPIRED_NOTICE = 'Oturumunuz sona erdi. Lütfen yeniden giriş yapın.'

// The session that survived a page reload, if its token is still valid.
function restoreSession(): SessionUser | null {
  const token = getToken()
  if (!token) return null

  const user = decodeToken(token)
  if (!user || isExpired(user)) {
    clearToken()
    return null
  }
  return user
}

/**
 * Holds the logged-in user for the whole app. The token lives in localStorage; the user (name, role, branch,
 * organization) is decoded from it. Also ends the session when the token expires or the API answers 401.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [user, setUser] = useState<SessionUser | null>(restoreSession)

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password })

    const decoded = decodeToken(response.token)
    if (!decoded) throw new Error('The server returned a token that could not be read.')

    setToken(response.token)
    setUser(decoded)
    return decoded
  }, [])

  // Session ended on its own (expiry, or the API rejected the token): log out and explain on the login page.
  const endSession = useCallback(() => {
    logout()
    navigate('/giris', { replace: true, state: { notice: EXPIRED_NOTICE } })
  }, [logout, navigate])

  // The API client reports 401s on authenticated requests here.
  useEffect(() => {
    setUnauthorizedHandler(endSession)
    return () => setUnauthorizedHandler(null)
  }, [endSession])

  // Log out at the moment the token expires, even if the user is idle on a page.
  const expiresAt = user?.expiresAt
  useEffect(() => {
    if (expiresAt === undefined) return
    const timer = setTimeout(endSession, Math.max(expiresAt - Date.now(), 0))
    return () => clearTimeout(timer)
  }, [expiresAt, endSession])

  const value = useMemo<AuthContextValue>(() => ({ user, login, logout }), [user, login, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
