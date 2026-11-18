import { useEffect, useId, useRef, useState } from 'react'
import { ChevronDown, LogOut, UserRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { SessionUser } from '../auth/jwt'
import { ROLE_LABELS } from '../auth/roles'
import { Badge } from '../components/Badge'
import { Icon } from '../components/Icon'
import { avatarToneOf, firstNameOf, initialsOf } from '../utils/avatar'
import { cx } from '../utils/cx'

/** The initials circle of the logged-in user (also used in the phone menu). */
export function UserAvatar({ name, className }: { name: string; className?: string }) {
  return (
    <span className={cx('flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-semibold', avatarToneOf(name), className)} aria-hidden="true">
      {initialsOf(name)}
    </span>
  )
}

/** Name, email and role: the head of the account menu and of the phone menu's account block. */
export function UserIdentity({ user }: { user: SessionUser }) {
  return (
    <div className="min-w-0 space-y-1">
      <p className="truncate text-sm font-semibold text-slate-900">{user.name}</p>
      <p className="truncate text-sm text-slate-600">{user.email}</p>
      <Badge tone="teal">{ROLE_LABELS[user.role]}</Badge>
    </div>
  )
}

interface UserMenuProps {
  user: SessionUser
  onLogout: () => void
}

/**
 * The account button on the right of the navbar (lg and up): initials, the first name, and a small dropdown with the
 * name, email, role, "Hesabım" and "Çıkış yap". A disclosure (button + panel) rather than an ARIA menu, so its items
 * are plain links and buttons reachable with Tab. Escape closes it and returns focus to the button; a click outside
 * or tabbing away closes it too.
 */
export function UserMenu({ user, onLogout }: UserMenuProps) {
  const [open, setOpen] = useState(false)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const panelId = useId()

  useEffect(() => {
    if (!open) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        buttonRef.current?.focus()
      }
    }
    const onPointerDown = (event: PointerEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        // Hand focus back to the button only if it was in the menu; a click elsewhere keeps its own focus.
        const focusInside = containerRef.current.contains(document.activeElement)
        setOpen(false)
        if (focusInside) buttonRef.current?.focus()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    document.addEventListener('pointerdown', onPointerDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.removeEventListener('pointerdown', onPointerDown)
    }
  }, [open])

  return (
    <div
      ref={containerRef}
      className="relative"
      // Tabbing past the last item closes the menu.
      onBlur={(event) => {
        if (open && !event.currentTarget.contains(event.relatedTarget as Node | null)) setOpen(false)
      }}
    >
      <button
        ref={buttonRef}
        type="button"
        aria-haspopup="true"
        aria-expanded={open}
        aria-controls={panelId}
        aria-label={`Hesap menüsü: ${user.name}`}
        onClick={() => setOpen((current) => !current)}
        className="flex min-h-10 items-center gap-2 rounded-full py-1 pl-1 pr-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
      >
        <UserAvatar name={user.name} className="h-8 w-8" />
        <span className="max-w-36 truncate">{firstNameOf(user.name)}</span>
        <Icon icon={ChevronDown} size={16} className={cx('text-slate-500 transition-transform duration-150', open && 'rotate-180')} />
      </button>

      {open ? (
        <div id={panelId} className="absolute right-0 z-40 mt-2 w-72 rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-start gap-3 border-b border-slate-200 p-4">
            <UserAvatar name={user.name} />
            <UserIdentity user={user} />
          </div>
          <div className="p-1.5">
            <Link
              to="/hesabim"
              onClick={() => setOpen(false)}
              className="flex min-h-10 items-center gap-2.5 rounded-md px-3 text-sm font-medium text-slate-700 hover:bg-slate-100 hover:text-slate-900"
            >
              <Icon icon={UserRound} className="text-slate-500" />
              Hesabım
            </Link>
            <button
              type="button"
              onClick={() => {
                setOpen(false)
                onLogout()
              }}
              className="flex min-h-10 w-full items-center gap-2.5 rounded-md px-3 text-left text-sm font-medium text-slate-700 hover:bg-slate-100 hover:text-slate-900"
            >
              <Icon icon={LogOut} className="text-slate-500" />
              Çıkış yap
            </button>
          </div>
        </div>
      ) : null}
    </div>
  )
}
