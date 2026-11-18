import { NavLink } from 'react-router-dom'
import type { Role } from '../api/types'
import { Icon } from '../components/Icon'
import { cx } from '../utils/cx'
import { navItemsFor } from './navItems'

interface StaffNavProps {
  role: Role
  /** The phone drawer: 44px rows. */
  touch?: boolean
  /** Called when a link is followed (the drawer closes). */
  onNavigate?: () => void
}

// Groups get a small muted label only when a role has more than this many links.
const GROUP_LABEL_FROM = 5

/**
 * The page links of a staff role with a small icon each. The active link gets a light teal tint, teal text and icon
 * and aria-current="page" (NavLink sets it); the rest stay quiet. Used by the sidebar and by the phone drawer.
 */
export function StaffNav({ role, touch = false, onNavigate }: StaffNavProps) {
  const items = navItemsFor(role)

  return (
    <nav aria-label="Ana gezinme" className="space-y-1">
      {items.length >= GROUP_LABEL_FROM ? <p className="px-3 pb-1 text-xs font-medium text-slate-500">Sayfalar</p> : null}
      <ul className="space-y-0.5">
        {items.map((item) => (
          <li key={item.to}>
            <NavLink
              to={item.to}
              end
              onClick={onNavigate}
              className={({ isActive }) =>
                cx(
                  'group flex items-center gap-3 rounded-lg px-3 text-sm transition-colors duration-150',
                  touch ? 'min-h-11' : 'min-h-9',
                  isActive ? 'bg-teal-50 font-semibold text-teal-800' : 'font-medium text-slate-700 hover:bg-slate-100 hover:text-slate-900',
                )
              }
            >
              {({ isActive }) => (
                <>
                  <Icon icon={item.icon} className={isActive ? 'text-teal-700' : 'text-slate-400 group-hover:text-slate-600'} />
                  {item.label}
                </>
              )}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
