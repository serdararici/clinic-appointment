import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { CircleAlert, CircleCheck, X } from 'lucide-react'
import { cx } from '../utils/cx'
import { Icon } from './Icon'
import { ToastContext, type ToastApi, type ToastVariant } from './toast-context'

const AUTO_DISMISS_MS = 4000

interface ToastItem {
  id: number
  variant: ToastVariant
  message: string
}

const styles: Record<ToastVariant, string> = {
  success: 'border-green-200 bg-green-50 text-green-900',
  error: 'border-red-200 bg-red-50 text-red-900',
}

// The icon says "done" or "problem" too, so the variant is never carried by color alone.
function ToastIcon({ variant }: { variant: ToastVariant }) {
  return <Icon icon={variant === 'success' ? CircleCheck : CircleAlert} size={20} className="mt-px" />
}

/**
 * Short, self-closing messages for the result of an action. They stack at the bottom (bottom-right from the sm
 * breakpoint up), disappear after 4 seconds, can be closed, and live in a polite live region so a screen reader
 * announces them without interrupting.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(1)
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>())

  const dismiss = useCallback((id: number) => {
    const timer = timers.current.get(id)
    if (timer) clearTimeout(timer)
    timers.current.delete(id)
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const show = useCallback(
    (variant: ToastVariant, message: string) => {
      const id = nextId.current++
      setToasts((current) => [...current, { id, variant, message }])
      timers.current.set(id, setTimeout(() => dismiss(id), AUTO_DISMISS_MS))
    },
    [dismiss],
  )

  // No timer may fire after the provider is gone.
  useEffect(() => {
    const pending = timers.current
    return () => {
      pending.forEach((timer) => clearTimeout(timer))
      pending.clear()
    }
  }, [])

  const api = useMemo<ToastApi>(() => ({ success: (message) => show('success', message), error: (message) => show('error', message) }), [show])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div aria-live="polite" className="pointer-events-none fixed inset-x-0 bottom-0 z-50 flex flex-col items-center gap-2 p-4 sm:inset-x-auto sm:right-0 sm:items-end">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cx('pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border px-4 py-3 text-sm shadow-sm sm:w-auto sm:min-w-72', styles[toast.variant])}
          >
            <ToastIcon variant={toast.variant} />
            <p className="flex-1">{toast.message}</p>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              aria-label="Bildirimi kapat"
              className="-m-2 flex h-11 w-11 shrink-0 items-center justify-center rounded-md hover:bg-black/5 sm:-m-1 sm:h-8 sm:w-8"
            >
              <Icon icon={X} size={16} />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
