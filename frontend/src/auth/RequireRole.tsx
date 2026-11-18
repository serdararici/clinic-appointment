import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { Role } from '../api/types'
import { homeFor } from './roles'
import { useAuth } from './useAuth'

interface RequireRoleProps {
  /** The roles allowed to see the wrapped pages. */
  roles: readonly Role[]
  /** Optional: wrap one element. Without it the guard works as a layout route and renders its child routes. */
  children?: ReactNode
}

/**
 * Route guard. Not logged in: go to the login page (remembering where the user wanted to go, unless they just logged
 * out on purpose).
 * Logged in with another role: go to that user's own home page instead.
 * This is a convenience for the user; the API still refuses forbidden calls on its own (401/403).
 */
export function RequireRole({ roles, children }: RequireRoleProps) {
  const { user, justLoggedOut } = useAuth()
  const location = useLocation()

  if (!user) {
    // After an explicit logout nothing is remembered: this guard re-renders at that moment and would otherwise
    // store the page the user just left, and the next login would return there.
    return <Navigate to="/giris" replace state={justLoggedOut ? undefined : { from: location.pathname }} />
  }

  if (!roles.includes(user.role)) {
    return <Navigate to={homeFor(user.role)} replace />
  }

  return children ?? <Outlet />
}
