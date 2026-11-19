import { useCallback, useState, type FormEvent } from 'react'
import { createTechnician, updateStaff } from '../../api/manager'
import { readableMessage } from '../../api/errors'
import type { Department, StaffMember } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'
import { Select } from '../../components/Select'
import { ServiceChecklist } from './ServiceChecklist'

interface TechnicianFormModalProps {
  open: boolean
  departments: readonly Department[]
  /** The technician being edited; null adds a new one. */
  technician: StaffMember | null
  onClose: () => void
  onSaved: () => void
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function TechnicianFormModal({ open, departments, technician, onClose, onSaved }: TechnicianFormModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={technician ? 'Sağlık teknisyenini düzenle' : 'Sağlık teknisyeni ekle'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="technician-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open ? <TechnicianForm departments={departments} technician={technician} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

/** Add a health technician (name, email, phone, department) or edit one (name, email, phone). Technicians have no login. */
function TechnicianForm({
  departments,
  technician,
  onSaved,
  onBusy,
}: {
  departments: readonly Department[]
  technician: StaffMember | null
  onSaved: () => void
  onBusy: (busy: boolean) => void
}) {
  const [name, setName] = useState(technician?.fullName ?? '')
  const [email, setEmail] = useState(technician?.email ?? '')
  const [phone, setPhone] = useState(technician?.phoneNumber ?? '')
  const [departmentId, setDepartmentId] = useState(technician?.departmentId ?? '')
  const [services, setServices] = useState<ReadonlySet<string>>(new Set())
  const [errors, setErrors] = useState<{ name?: string; email?: string; department?: string }>({})
  // Each time a department's list has loaded, all of its services start checked.
  const checkAll = useCallback((ids: string[]) => setServices(new Set(ids)), [])
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!name.trim()) found.name = 'Ad soyad zorunludur.'
    if (!EMAIL_PATTERN.test(email.trim())) found.email = 'Geçerli bir e-posta adresi girin.'
    if (!technician && !departmentId) found.department = 'Bölüm seçin.'
    setErrors(found)
    if (Object.keys(found).length > 0) return

    onBusy(true)
    try {
      if (technician) await updateStaff(technician.id, { fullName: name.trim(), email: email.trim(), phoneNumber: phone.trim() })
      else await createTechnician(departmentId, { fullName: name.trim(), email: email.trim(), phoneNumber: phone.trim() || undefined, serviceIds: [...services] })
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  return (
    <form id="technician-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label="Ad soyad" value={name} onChange={(event) => setName(event.target.value)} error={errors.name} autoFocus />
      <Input label="E-posta" type="email" value={email} onChange={(event) => setEmail(event.target.value)} error={errors.email} />
      <Input label="Telefon (isteğe bağlı)" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
      {technician ? null : (
        <Select
          label="Bölüm"
          value={departmentId}
          onChange={(event) => setDepartmentId(event.target.value)}
          placeholder="Seçiniz"
          options={departments.map((department) => ({ value: department.id, label: department.name }))}
          error={errors.department}
        />
      )}
      {technician || !departmentId ? null : (
        <ServiceChecklist key={departmentId} departmentId={departmentId} kind="Diagnostic" selected={services} onSelectedChange={setServices} onLoaded={checkAll} />
      )}
      {technician ? null : <p className="text-sm text-slate-600">Sağlık teknisyenleri sisteme giriş yapmaz; yalnızca personel kaydı olarak tutulur.</p>}
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
