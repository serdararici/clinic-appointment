import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from './auth-context'

/** The current user and the login/logout actions. Must be used inside <AuthProvider>. */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>.')
  return context
}
