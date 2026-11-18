import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { returnPathFor } from './roles'
import { useAuth } from './useAuth'

/**
 * For the login and register pages. Someone who is logged in is sent on right away: this is also what moves a user
 * away from the login page after a successful login. They go back to the page they came from (a deep link, or the
 * page where their session ended) only if their role may open it; otherwise, and always after an explicit logout,
 * to the home page of their role. So logging in as another role never lands on a page that role cannot open.
 */
export function GuestOnly() {
  const { user } = useAuth()
  const location = useLocation()

  if (!user) return <Outlet />

  const from = (location.state as { from?: string } | null)?.from
  return <Navigate to={returnPathFor(user.role, from)} replace />
}
