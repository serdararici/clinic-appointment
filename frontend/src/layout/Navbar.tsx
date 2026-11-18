import { useEffect, useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Button } from '../components/Button'
import { LinkButton } from '../components/LinkButton'
import { LogoMark } from '../components/LogoMark'
import { cx } from '../utils/cx'
import { Container } from './Container'
import { navItemsFor } from './navItems'
import { UserAvatar, UserIdentity, UserMenu } from './UserMenu'

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
          touch ? 'min-h-11' : 'min-h-14',
          isActive ? 'border-teal-700 font-semibold text-slate-900' : 'border-transparent font-medium text-slate-600 hover:text-slate-900',
        )
      }
    >
      {label}
    </NavLink>
  )
}

/**
 * Top bar. Anonymous visitors see login and register; a logged-in user sees the links of their role and, on the
 * right, the account menu (Hesabım, Çıkış yap). Below lg everything moves into the phone menu.
 */
export function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false) // the phone-width menu

  const closeMenu = () => setMenuOpen(false)

  // An explicit logout: the auth context remembers it, so the next login goes to the role's home page.
  function handleLogout() {
    closeMenu()
    logout()
    navigate('/giris', { replace: true })
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

  const links = (touch: boolean) =>
    user ? navItemsFor(user.role).map((item) => <NavItemLink key={item.to} {...item} touch={touch} onNavigate={closeMenu} />) : null

  return (
    <header className="border-b border-slate-200 bg-white">
      <Container className="flex min-h-14 items-center gap-6">
        <Link to="/" className="flex items-center gap-2.5 text-base font-semibold tracking-tight text-slate-900">
          <LogoMark />
          Klinik Randevu
        </Link>

        {/* Desktop: the role's links, then the account menu (or login / register) on the right. */}
        <nav aria-label="Ana gezinme" className="hidden flex-1 items-center gap-1 self-stretch lg:flex">
          {links(false)}
        </nav>
        <div className="ml-auto hidden items-center gap-3 lg:flex">
          {user ? (
            <UserMenu user={user} onLogout={handleLogout} />
          ) : (
            <>
              <LinkButton to="/giris" variant="secondary">
                Giriş yap
              </LinkButton>
              <LinkButton to="/kayit">Kayıt ol</LinkButton>
            </>
          )}
        </div>

        {/* Phone: a menu button (44px touch target) that opens the links and the account block below the bar. */}
        <Button
          variant="secondary"
          className="ml-auto lg:hidden"
          aria-expanded={menuOpen}
          aria-controls="mobile-menu"
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? 'Kapat' : 'Menü'}
        </Button>
      </Container>

      {menuOpen ? (
        <div id="mobile-menu" className="border-t border-slate-200 lg:hidden">
          <Container className="space-y-3 py-3">
            {user ? (
              <>
                <nav aria-label="Ana gezinme" className="flex flex-col">
                  {links(true)}
                </nav>
                <div className="space-y-3 border-t border-slate-200 pt-3">
                  <div className="flex items-start gap-3">
                    <UserAvatar name={user.name} />
                    <UserIdentity user={user} />
                  </div>
                  <div className="flex flex-col gap-2">
                    <LinkButton to="/hesabim" variant="secondary" size="lg" onClick={closeMenu}>
                      Hesabım
                    </LinkButton>
                    <Button variant="secondary" size="lg" onClick={handleLogout}>
                      Çıkış yap
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="flex flex-col gap-2">
                <LinkButton to="/giris" variant="secondary" size="lg" onClick={closeMenu}>
                  Giriş yap
                </LinkButton>
                <LinkButton to="/kayit" size="lg" onClick={closeMenu}>
                  Kayıt ol
                </LinkButton>
              </div>
            )}
          </Container>
        </div>
      ) : null}
    </header>
  )
}
