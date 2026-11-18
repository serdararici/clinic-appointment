import { useState, type FormEvent } from 'react'
import { createService, updateService } from '../../api/manager'
import { readableMessage } from '../../api/errors'
import type { SaveServiceRequest, Service, ServiceType } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'
import { Select } from '../../components/Select'
import { SERVICE_TYPE_LABELS } from '../patient/labels'

interface ServiceFormModalProps {
  open: boolean
  departmentId: string
  /** The service being edited; null adds a new one. */
  service: Service | null
  onClose: () => void
  onSaved: () => void
}

// An Examination service takes its length from the doctor's slot duration, but the API still stores a duration
// for it: a new one gets this value and an edited one keeps what it has.
const DEFAULT_EXAMINATION_MINUTES = 30
const MAX_PRICE = 99_999_999.99

export function ServiceFormModal({ open, departmentId, service, onClose, onSaved }: ServiceFormModalProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={service ? 'Hizmeti düzenle' : 'Hizmet ekle'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Vazgeç
          </Button>
          <Button type="submit" form="service-form">
            Kaydet
          </Button>
        </>
      }
    >
      {open ? <ServiceForm departmentId={departmentId} service={service} onSaved={onSaved} /> : null}
    </Modal>
  )
}

/** Reads a price typed the Turkish way ("450,50") or the plain way ("450.50"); null if it is not a valid amount. */
function parsePrice(text: string): number | null {
  const normalized = text.trim().replace(',', '.')
  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) return null
  const value = Number(normalized)
  return value <= MAX_PRICE ? value : null
}

function ServiceForm({ departmentId, service, onSaved }: { departmentId: string; service: Service | null; onSaved: () => void }) {
  const [name, setName] = useState(service?.name ?? '')
  const [type, setType] = useState<ServiceType>(service?.serviceType ?? 'Examination')
  const [duration, setDuration] = useState(String(service && service.serviceType === 'Diagnostic' ? service.durationMinutes : 20))
  const [price, setPrice] = useState(service ? String(service.basePrice).replace('.', ',') : '')
  const [errors, setErrors] = useState<{ name?: string; duration?: string; price?: string }>({})
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!name.trim()) found.name = 'Hizmet adı zorunludur.'
    const amount = parsePrice(price)
    if (amount === null) found.price = 'Geçerli bir ücret girin (en fazla iki ondalık basamak, örn. 450,50).'
    const minutes = Number(duration)
    if (type === 'Diagnostic' && (!Number.isInteger(minutes) || minutes < 1 || minutes > 1440)) {
      found.duration = 'Süre 1 ile 1440 dakika arasında bir tam sayı olmalıdır.'
    }
    setErrors(found)
    if (Object.keys(found).length > 0 || amount === null) return

    const request: SaveServiceRequest = {
      name: name.trim(),
      serviceType: type,
      durationMinutes: type === 'Diagnostic' ? minutes : (service?.durationMinutes ?? DEFAULT_EXAMINATION_MINUTES),
      basePrice: amount,
    }
    try {
      if (service) await updateService(service.id, request)
      else await createService(departmentId, request)
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    }
  }

  return (
    <form id="service-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label="Hizmet adı" value={name} onChange={(event) => setName(event.target.value)} error={errors.name} autoFocus />
      <Select
        label="Hizmet türü"
        value={type}
        onChange={(event) => setType(event.target.value as ServiceType)}
        options={(Object.keys(SERVICE_TYPE_LABELS) as ServiceType[]).map((value) => ({ value, label: SERVICE_TYPE_LABELS[value] }))}
        hint={type === 'Examination' ? 'Muayene süresi, doktorun randevu süresine göre belirlenir.' : 'Tetkik, doktor seçmeden belirli bir süreyle alınır.'}
      />
      {type === 'Diagnostic' ? (
        <Input label="Süre (dakika)" type="number" min={1} max={1440} value={duration} onChange={(event) => setDuration(event.target.value)} error={errors.duration} />
      ) : null}
      <Input label="Ücret (₺)" inputMode="decimal" value={price} onChange={(event) => setPrice(event.target.value)} error={errors.price} />
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
