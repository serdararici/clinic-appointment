import { useState, type FormEvent } from 'react'
import { readableMessage } from '../../api/errors'
import { setStaffServices } from '../../api/manager'
import type { StaffMember } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Modal } from '../../components/Modal'
import { useToast } from '../../components/useToast'
import { ServiceChecklist } from './ServiceChecklist'

interface StaffServicesModalProps {
  open: boolean
  /** A doctor (examination services) or a health technician (diagnostic services). */
  member: StaffMember | null
  onClose: () => void
  onSaved: () => void
}

/** "Hizmetler": the services a staff member is linked to. A doctor offers examination services, a technician diagnostic ones. */
export function StaffServicesModal({ open, member, onClose, onSaved }: StaffServicesModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={member ? `Hizmetler · ${member.fullName}` : 'Hizmetler'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="staff-services-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open && member ? <ServicesForm member={member} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

function ServicesForm({ member, onSaved, onBusy }: { member: StaffMember; onSaved: () => void; onBusy: (busy: boolean) => void }) {
  const toast = useToast()
  const isDoctor = member.role === 'Doctor'
  const [selected, setSelected] = useState<ReadonlySet<string>>(() => new Set(member.serviceIds))
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)
    onBusy(true)
    try {
      await setStaffServices(member.id, [...selected])
      toast.success('Hizmetler kaydedildi.')
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  return (
    <form id="staff-services-form" onSubmit={submit} noValidate className="space-y-3">
      <p className="text-sm text-slate-600">
        {isDoctor
          ? 'Doktorun verdiği muayene hizmetlerini seçin. Hiç hizmet seçilmezse bu doktordan randevu alınamaz.'
          : 'Teknisyenin bağlı olduğu tetkik hizmetlerini seçin. Bu yalnızca bir tanımdır: tetkik randevuları teknisyene atanmaz.'}
      </p>
      <ServiceChecklist
        departmentId={member.departmentId}
        kind={isDoctor ? 'Examination' : 'Diagnostic'}
        selected={selected}
        onSelectedChange={setSelected}
        examinationMinutes={member.slotDurationMinutes}
      />
      {isDoctor ? <p className="text-xs text-slate-500">Muayene süresi doktorun randevu süresine göre belirlenir.</p> : null}
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
