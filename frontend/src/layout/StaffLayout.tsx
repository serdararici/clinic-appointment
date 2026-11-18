import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react'
import { Menu, X } from 'lucide-react'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { ROLE_LABELS } from '../auth/roles'
import { useAuth } from '../auth/useAuth'
import { Icon } from '../components/Icon'
import { LogoMark } from '../components/LogoMark'
import { Container } from './Container'
import { Footer } from './Footer'
import { StaffNav } from './StaffNav'
import { UserMenu } from './UserMenu'

function Brand() {
  return (
    <Link to="/" className="flex items-center gap-2.5 text-base font-semibold tracking-tight text-slate-900">
      <LogoMark />
      Klinik Randevu
    </Link>
  )
}

/**
 * The shell of the staff roles (doctor, clinic manager, system administrator). From lg up: a fixed left sidebar with
 * the role's links, a slim top bar (role label, account menu) and a wider content area (max-w-7xl) with the footer.
 * Below lg: the top bar has a menu button that opens the same links as a drawer from the left.
 */
export function StaffLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const menuButtonRef = useRef<HTMLButtonElement>(null)
  const drawerRef = useRef<HTMLDivElement>(null)

  const closeDrawer = () => setDrawerOpen(false)

  // An explicit logout: the next login goes to the role's home page (see AuthProvider).
  const handleLogout = () => {
    setDrawerOpen(false)
    logout()
    navigate('/giris', { replace: true })
  }

  // While the drawer is open: the page behind does not scroll, Escape closes it, and focus moves into it; when it
  // closes, focus goes back to the menu button.
  useEffect(() => {
    if (!drawerOpen) return
    const button = menuButtonRef.current
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    drawerRef.current?.querySelector<HTMLElement>('a, button')?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setDrawerOpen(false)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', onKeyDown)
      button?.focus()
    }
  }, [drawerOpen])

  // Keep Tab inside the open drawer (it is a modal panel).
  const trapFocus = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    if (event.key !== 'Tab' || !drawerRef.current) return
    const focusable = Array.from(drawerRef.current.querySelectorAll<HTMLElement>('a[href], button:not([disabled])'))
    if (focusable.length === 0) return
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }

  // RequireRole inside the routes sends anonymous visitors away; nothing to show until then.
  if (!user) return <Outlet />
  const roleLabel = ROLE_LABELS[user.role]

  return (
    <div className="min-h-screen">
      {/* Sidebar, lg and up */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-60 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="flex h-14 items-center border-b border-slate-200 px-5">
          <Brand />
        </div>
        <div className="flex-1 overflow-y-auto px-3 py-4">
          <StaffNav role={user.role} />
        </div>
        <p className="border-t border-slate-200 px-5 py-4 text-sm text-slate-500">{roleLabel}</p>
      </aside>

      <div className="flex min-h-screen flex-col lg:pl-60">
        {/* Top bar */}
        <header className="sticky top-0 z-20 border-b border-slate-200 bg-white">
          <div className="flex h-14 items-center gap-3 px-4 sm:px-6">
            <button
              ref={menuButtonRef}
              type="button"
              onClick={() => setDrawerOpen(true)}
              aria-label="Menüyü aç"
              aria-expanded={drawerOpen}
              aria-controls="staff-drawer"
              className="-ml-2 flex h-11 w-11 items-center justify-center rounded-lg text-slate-700 hover:bg-slate-100 lg:hidden"
            >
              <Icon icon={Menu} size={20} />
            </button>
            <div className="lg:hidden">
              <Brand />
            </div>
            <p className="hidden text-sm text-slate-500 lg:block">{roleLabel}</p>
            <div className="ml-auto">
              <UserMenu user={user} onLogout={handleLogout} />
            </div>
          </div>
        </header>

        <main className="flex-1">
          <Container wide className="py-6 sm:py-8">
            <Outlet />
          </Container>
        </main>
        <Footer wide />
      </div>

      {/* Phone and tablet: the same navigation as a drawer from the left */}
      {drawerOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-slate-900/40" onClick={closeDrawer} aria-hidden="true" />
          <div
            id="staff-drawer"
            ref={drawerRef}
            role="dialog"
            aria-modal="true"
            aria-label="Menü"
            onKeyDown={trapFocus}
            className="absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col border-r border-slate-200 bg-white shadow-sm"
          >
            <div className="flex h-14 items-center justify-between border-b border-slate-200 pl-5 pr-2">
              <Brand />
              <button
                type="button"
                onClick={closeDrawer}
                aria-label="Menüyü kapat"
                className="flex h-11 w-11 items-center justify-center rounded-lg text-slate-700 hover:bg-slate-100"
              >
                <Icon icon={X} size={20} />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto px-3 py-4">
              <StaffNav role={user.role} touch onNavigate={closeDrawer} />
            </div>
            <p className="border-t border-slate-200 px-5 py-4 text-sm text-slate-500">{roleLabel}</p>
          </div>
        </div>
      ) : null}
    </div>
  )
}
