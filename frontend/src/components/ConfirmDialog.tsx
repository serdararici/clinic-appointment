import { useState, type ReactNode } from 'react'
import { readableMessage } from '../api/errors'
import { Alert } from './Alert'
import { Button } from './Button'
import { Modal } from './Modal'

interface ConfirmDialogProps {
  open: boolean
  title: string
  /** What is about to happen, in plain words. */
  children: ReactNode
  confirmLabel: string
  /** Red button for destructive actions (delete). */
  danger?: boolean
  /** Runs the action; the dialog closes when it succeeds and shows the (Turkish) API error when it fails. */
  onConfirm: () => Promise<void>
  onClose: () => void
}

/** A confirmation modal for deleting or deactivating something; it owns the loading and error state of the action. */
export function ConfirmDialog({ open, title, children, confirmLabel, danger = false, onConfirm, onClose }: ConfirmDialogProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const close = () => {
    if (busy) return
    setError(null)
    onClose()
  }

  const confirm = async () => {
    setBusy(true)
    setError(null)
    try {
      await onConfirm()
      onClose()
    } catch (caught) {
      setError(readableMessage(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal
      open={open}
      onClose={close}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={close} disabled={busy}>
            Vazgeç
          </Button>
          <Button variant={danger ? 'danger' : 'primary'} loading={busy} onClick={confirm}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <div className="text-slate-700">{children}</div>
        {error ? <Alert tone="error">{error}</Alert> : null}
      </div>
    </Modal>
  )
}
