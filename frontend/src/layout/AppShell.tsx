import { useAuth } from '../auth/useAuth'
import { Layout } from './Layout'
import { STAFF_ROLES } from './navItems'
import { StaffLayout } from './StaffLayout'

/**
 * Picks the frame by the logged-in role: the staff roles (doctor, clinic manager, system administrator) get the
 * sidebar shell, everyone else (patients, technicians, visitors on their way to the login page) the top-navbar
 * layout. One layout route for all role pages means a shared page such as /hesabim is shown in the right frame.
 */
export function AppShell() {
  const { user } = useAuth()
  return user && STAFF_ROLES.includes(user.role) ? <StaffLayout /> : <Layout />
}
