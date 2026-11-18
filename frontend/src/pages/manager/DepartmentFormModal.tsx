import { useState, type FormEvent } from 'react'
import { createDepartment, updateDepartment } from '../../api/manager'
import { readableMessage } from '../../api/errors'
import type { Department } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'

interface DepartmentFormModalProps {
  open: boolean
  branchId: string
  /** The department being edited; null adds a new one. */
  department: Department | null
  onClose: () => void
  onSaved: () => void
}

/** Add or rename a department. The form is only mounted while the modal is open, so it always starts fresh. */
export function DepartmentFormModal({ open, branchId, department, onClose, onSaved }: DepartmentFormModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={department ? 'Bölümü düzenle' : 'Bölüm ekle'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="department-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open ? <DepartmentForm branchId={branchId} department={department} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

function DepartmentForm({ branchId, department, onSaved, onBusy }: { branchId: string; department: Department | null; onSaved: () => void; onBusy: (busy: boolean) => void }) {
  const [name, setName] = useState(department?.name ?? '')
  const [error, setError] = useState<string | undefined>()
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)
    if (!name.trim()) {
      setError('Bölüm adı zorunludur.')
      return
    }
    setError(undefined)
    onBusy(true)
    try {
      if (department) await updateDepartment(department.id, name.trim())
      else await createDepartment(branchId, name.trim())
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  return (
    <form id="department-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label="Bölüm adı" value={name} onChange={(event) => setName(event.target.value)} error={error} autoFocus />
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
