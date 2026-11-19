import { useCallback, useState, type FormEvent } from 'react'
import { listServices } from '../../api/booking'
import { readableMessage } from '../../api/errors'
import { setStaffServices } from '../../api/manager'
import type { Service, StaffMember } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { EmptyState } from '../../components/EmptyState'
import { Modal } from '../../components/Modal'
import { Spinner } from '../../components/Spinner'
import { useToast } from '../../components/useToast'
import { formatCurrency } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'

interface DoctorServicesModalProps {
  open: boolean
  doctor: StaffMember | null
  onClose: () => void
  onSaved: () => void
}

/** "Hizmetler": which examination services of the department a doctor offers. */
export function DoctorServicesModal({ open, doctor, onClose, onSaved }: DoctorServicesModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={doctor ? `Hizmetler · ${doctor.fullName}` : 'Hizmetler'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="doctor-services-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open && doctor ? <ServicesForm doctor={doctor} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

function ServicesForm({ doctor, onSaved, onBusy }: { doctor: StaffMember; onSaved: () => void; onBusy: (busy: boolean) => void }) {
  const toast = useToast()
  // A doctor offers the examination services of their own department.
  const state = useAsync(
    useCallback(async (signal: AbortSignal): Promise<Service[]> => (await listServices(doctor.departmentId, signal)).filter((service) => service.serviceType === 'Examination'), [doctor.departmentId]),
  )
  const [selected, setSelected] = useState<Set<string>>(() => new Set(doctor.serviceIds))
  const [apiError, setApiError] = useState<string | null>(null)

  const toggle = (id: string) =>
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)
    onBusy(true)
    try {
      // Ids that are not examination services of this department (none today) are dropped: the list shown is the whole choice.
      const allowed = new Set((state.data ?? []).map((service) => service.id))
      await setStaffServices(doctor.id, [...selected].filter((id) => allowed.has(id)))
      toast.success('Hizmetler kaydedildi.')
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  if (state.loading) {
    return (
      <div className="flex justify-center py-6">
        <Spinner label="Yükleniyor..." />
      </div>
    )
  }
  if (state.error) {
    return (
      <div className="space-y-3">
        <Alert tone="error">{state.error}</Alert>
        <Button variant="secondary" onClick={state.reload}>
          Yeniden dene
        </Button>
      </div>
    )
  }
  if (!state.data || state.data.length === 0) {
    return <EmptyState title="Bu bölümde muayene hizmeti yok" description="Önce Bölümler sayfasından bir muayene hizmeti ekleyin." />
  }

  return (
    <form id="doctor-services-form" onSubmit={submit} noValidate className="space-y-3">
      <p className="text-sm text-slate-600">Doktorun verdiği muayene hizmetlerini seçin. Hiç hizmet seçilmezse bu doktordan randevu alınamaz.</p>
      <ul className="divide-y divide-slate-200 rounded-lg border border-slate-200">
        {state.data.map((service) => (
          <li key={service.id}>
            <label className="flex min-h-11 cursor-pointer items-center gap-3 px-3 py-2">
              <input type="checkbox" checked={selected.has(service.id)} onChange={() => toggle(service.id)} className="h-4 w-4 accent-teal-700" />
              <span className="flex-1 text-sm font-medium text-slate-900">{service.name}</span>
              <span className="text-sm tabular-nums text-slate-600">{doctor.slotDurationMinutes} dk</span>
              <span className="w-24 text-right text-sm tabular-nums text-slate-900">{formatCurrency(service.basePrice)}</span>
            </label>
          </li>
        ))}
      </ul>
      <p className="text-xs text-slate-500">Muayene süresi doktorun randevu süresine göre belirlenir.</p>
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
