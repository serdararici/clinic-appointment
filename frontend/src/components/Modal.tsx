import { useEffect, useRef, type ReactNode } from 'react'
import { Button } from './Button'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Buttons for the bottom of the dialog (confirm / cancel). */
  footer?: ReactNode
  /** false while an action is running: Escape and a click on the backdrop then do not close the dialog. */
  dismissible?: boolean
}

// Where focus goes back to when a dialog closes: the element that opened it. A dialog opened from inside another dialog
// (a type picker that opens a form) takes over the opener of the first one, so focus returns to the real button.
const openers = new WeakMap<HTMLDialogElement, HTMLElement>()

function findOpener(): HTMLElement | null {
  const active = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const insideDialog = active?.closest('dialog')
  return (insideDialog ? openers.get(insideDialog) : null) ?? active
}

/**
 * A dialog built on the browser's native <dialog> element: it traps focus and is announced by screen readers without
 * extra code. The `open` prop is the single source of truth:
 * - Opening and closing is done only by the prop (an effect calls showModal / close), and `onClose` is only called for
 *   something the user did: Escape, the "Kapat" button or a click on the dimmed backdrop. A close caused by the prop
 *   itself never calls `onClose`, so a parent can close one dialog and open the next in the same step.
 * - Escape is handled through the `cancel` event: the browser's own close is prevented and `onClose` decides.
 * - If the browser closes the dialog on its own while the prop is still true, `onClose` is called so the parent follows.
 */
export function Modal({ open, onClose, title, children, footer, dismissible = true }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const openRef = useRef(open)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    openRef.current = open

    if (open && !dialog.open) {
      const opener = findOpener()
      if (opener) openers.set(dialog, opener)
      dialog.showModal()
    }

    if (!open && dialog.open) {
      dialog.close()
      // The browser returns focus to what was focused before; make sure it is the opener, unless another dialog took over.
      if (!document.querySelector('dialog[open]')) openers.get(dialog)?.focus()
    }
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      // The native close event: for a close the browser did by itself while `open` is still true. A close caused by the
      // prop has already set openRef to false, so it is ignored here.
      onClose={() => {
        if (openRef.current) onClose()
      }}
      // Escape: never close natively; the parent decides through onClose (and not at all while busy).
      onCancel={(event) => {
        event.preventDefault()
        if (dismissible) onClose()
      }}
      // The backdrop belongs to the dialog element itself, so a click whose target is the dialog is a backdrop click.
      onClick={(event) => {
        if (event.target === event.currentTarget && dismissible) onClose()
      }}
      className="m-auto max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] max-w-lg overflow-y-auto rounded-xl border border-slate-200 bg-white p-0 text-slate-900 shadow-sm backdrop:bg-slate-900/40"
    >
      <div className="flex items-center justify-between gap-4 border-b border-slate-200 px-5 py-3">
        <h2 className="text-base font-semibold">{title}</h2>
        <Button variant="ghost" onClick={onClose} disabled={!dismissible} aria-label="Kapat">
          Kapat
        </Button>
      </div>
      <div className="px-5 py-4 text-sm">{children}</div>
      {footer ? <div className="flex flex-wrap justify-end gap-2 border-t border-slate-200 px-5 py-3">{footer}</div> : null}
    </dialog>
  )
}
