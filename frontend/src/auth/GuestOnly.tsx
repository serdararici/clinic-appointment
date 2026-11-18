import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { homeFor } from './roles'
import { useAuth } from './useAuth'

/**
 * For the login and register pages. Someone who is logged in is sent on: to the page they originally asked for
 * (the route guard passes it as `from`), or else to their own home page. This is also what moves a user away
 * from the login page right after a successful login.
 */
export function GuestOnly() {
  const { user } = useAuth()
  const location = useLocation()

  if (!user) return <Outlet />

  const from = (location.state as { from?: string } | null)?.from
  return <Navigate to={from ?? homeFor(user.role)} replace />
}
