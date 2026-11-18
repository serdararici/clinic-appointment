import { useEffect, useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ROLE_LABELS } from '../auth/roles'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { LinkButton } from '../components/LinkButton'
import { cx } from '../utils/cx'
import { navItemsFor } from './navItems'

// One link of the navigation, with the current page marked by weight and an underline (not by color alone).
function NavItemLink({ to, label, touch, onNavigate }: { to: string; label: string; touch: boolean; onNavigate: () => void }) {
  return (
    <NavLink
      to={to}
      end
      onClick={onNavigate}
      className={({ isActive }) =>
        cx(
          'inline-flex items-center border-b-2 px-3 text-sm',
          touch ? 'min-h-11' : 'min-h-10',
          isActive ? 'border-teal-700 font-semibold text-slate-900' : 'border-transparent font-medium text-slate-600 hover:text-slate-900',
        )
      }
    >
      {label}
    </NavLink>
  )
}

/** Top bar. Anonymous visitors see login and register; a logged-in user sees the links of their role. */
export function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false) // the phone-width menu

  const closeMenu = () => setMenuOpen(false)

  function handleLogout() {
    closeMenu()
    logout()
    navigate('/giris')
  }

  // Escape closes the phone menu.
  useEffect(() => {
    if (!menuOpen) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [menuOpen])

  // The same content serves the desktop bar and the phone menu; the phone menu gets 44px touch targets.
  const links = (touch: boolean) =>
    user ? navItemsFor(user.role).map((item) => <NavItemLink key={item.to} {...item} touch={touch} onNavigate={closeMenu} />) : null

  const account = (size: 'md' | 'lg') => user ? (
    <>
      <div className="flex items-center gap-2 text-sm">
        <span className="font-medium text-slate-900">{user.name}</span>
        <Badge tone="teal">{ROLE_LABELS[user.role]}</Badge>
      </div>
      <Button variant="secondary" size={size} onClick={handleLogout}>
        Çıkış yap
      </Button>
    </>
  ) : (
    <>
      <LinkButton to="/giris" variant="secondary" size={size} onClick={closeMenu}>
        Giriş yap
      </LinkButton>
      <LinkButton to="/kayit" size={size} onClick={closeMenu}>
        Kayıt ol
      </LinkButton>
    </>
  )

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-2 sm:px-6">
        <Link to="/" className="text-base font-semibold tracking-tight text-slate-900">
          Klinik Randevu
        </Link>

        {/* Tablet and desktop */}
        <nav aria-label="Ana gezinme" className="hidden flex-1 items-center gap-1 lg:flex">
          {links(false)}
        </nav>
        <div className="hidden items-center gap-3 lg:flex">{account('md')}</div>

        {/* Phone: a menu button (44px touch target) that opens the same links below the bar. */}
        <Button
          variant="secondary"
          className="lg:hidden"
          aria-expanded={menuOpen}
          aria-controls="mobile-menu"
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? 'Kapat' : 'Menü'}
        </Button>
      </div>

      {menuOpen ? (
        <div id="mobile-menu" className="space-y-3 border-t border-slate-200 px-4 py-3 lg:hidden">
          <nav aria-label="Ana gezinme" className="flex flex-col">
            {links(true)}
          </nav>
          <div className="flex flex-col gap-3">{account('lg')}</div>
        </div>
      ) : null}
    </header>
  )
}
