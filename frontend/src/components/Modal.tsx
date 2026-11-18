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

/**
 * A dialog built on the browser's native <dialog> element: it traps focus, closes with Escape and
 * is announced by screen readers without extra code. A click on the dimmed backdrop closes it too.
 */
export function Modal({ open, onClose, title, children, footer, dismissible = true }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  // Keep the native dialog in step with the `open` prop.
  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      // Escape: keep the dialog open while it is busy.
      onCancel={(event) => {
        if (!dismissible) event.preventDefault()
      }}
      // The backdrop belongs to the dialog element itself, so a click whose target is the dialog is a backdrop click.
      onClick={(event) => {
        if (event.target === event.currentTarget && dismissible) onClose()
      }}
      className="m-auto max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] max-w-lg overflow-y-auto rounded-lg border border-slate-200 bg-white p-0 text-slate-900 backdrop:bg-slate-900/40"
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
