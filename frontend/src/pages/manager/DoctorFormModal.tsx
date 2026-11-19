import { useState, type FormEvent } from 'react'
import { setSlotDuration } from '../../api/doctor'
import { createDoctor, updateStaff } from '../../api/manager'
import { readableMessage } from '../../api/errors'
import type { Department, StaffMember } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'
import { Select } from '../../components/Select'
import { SLOT_DURATIONS } from '../doctor/availability-utils'

interface DoctorFormModalProps {
  open: boolean
  departments: readonly Department[]
  /** The doctor being edited; null adds a new one. */
  doctor: StaffMember | null
  onClose: () => void
  onSaved: () => void
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const MIN_PASSWORD_LENGTH = 8

export function DoctorFormModal({ open, departments, doctor, onClose, onSaved }: DoctorFormModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={doctor ? 'Doktoru düzenle' : 'Doktor ekle'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="doctor-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open ? <DoctorForm departments={departments} doctor={doctor} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

/**
 * Add a doctor (name, email, temporary password, department) or edit one (name, email, phone, slot duration).
 * A doctor cannot be moved to another department here, and their availability is managed by the doctor.
 */
function DoctorForm({ departments, doctor, onSaved, onBusy }: { departments: readonly Department[]; doctor: StaffMember | null; onSaved: () => void; onBusy: (busy: boolean) => void }) {
  const [name, setName] = useState(doctor?.fullName ?? '')
  const [email, setEmail] = useState(doctor?.email ?? '')
  const [phone, setPhone] = useState(doctor?.phoneNumber ?? '')
  const [password, setPassword] = useState('')
  const [departmentId, setDepartmentId] = useState(doctor?.departmentId ?? '')
  const [slot, setSlot] = useState(String(doctor?.slotDurationMinutes ?? 30))
  const [errors, setErrors] = useState<{ name?: string; email?: string; password?: string; department?: string }>({})
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!name.trim()) found.name = 'Ad soyad zorunludur.'
    if (!EMAIL_PATTERN.test(email.trim())) found.email = 'Geçerli bir e-posta adresi girin.'
    if (!doctor && password.length < MIN_PASSWORD_LENGTH) found.password = `Geçici şifre en az ${MIN_PASSWORD_LENGTH} karakter olmalıdır.`
    if (!doctor && !departmentId) found.department = 'Bölüm seçin.'
    setErrors(found)
    if (Object.keys(found).length > 0) return

    onBusy(true)
    try {
      if (doctor) {
        await updateStaff(doctor.id, { fullName: name.trim(), email: email.trim(), phoneNumber: phone.trim() })
        if (Number(slot) !== doctor.slotDurationMinutes) await setSlotDuration(doctor.id, Number(slot))
      } else {
        await createDoctor(departmentId, {
          fullName: name.trim(),
          email: email.trim(),
          phoneNumber: phone.trim() || undefined,
          password,
          slotDurationMinutes: Number(slot),
        })
      }
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  return (
    <form id="doctor-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label="Ad soyad" value={name} onChange={(event) => setName(event.target.value)} error={errors.name} autoFocus />
      <Input label="E-posta" type="email" value={email} onChange={(event) => setEmail(event.target.value)} error={errors.email} />
      <Input label="Telefon (isteğe bağlı)" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
      {doctor ? null : (
        <>
          <Input
            label="Geçici şifre"
            autoComplete="off"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            error={errors.password}
            hint="Doktor bu e-posta ve şifreyle giriş yapar. Şifreyi doktora iletin."
          />
          <Select
            label="Bölüm"
            value={departmentId}
            onChange={(event) => setDepartmentId(event.target.value)}
            placeholder="Seçiniz"
            options={departments.map((department) => ({ value: department.id, label: department.name }))}
            error={errors.department}
          />
          <p className="-mt-1.5 text-sm text-slate-500">Doktor, bölümündeki tüm muayene hizmetlerini otomatik olarak sunar. Sonra “Hizmetler” ile değiştirebilirsiniz.</p>
        </>
      )}
      <Select
        label="Randevu süresi"
        value={slot}
        onChange={(event) => setSlot(event.target.value)}
        options={SLOT_DURATIONS.map((minutes) => ({ value: String(minutes), label: `${minutes} dakika` }))}
      />
      {doctor ? null : (
        <p className="text-sm text-slate-600">
          Çalışma takvimi otomatik oluşturulur (hafta içi 09:00-12:00 ve 13:00-16:00, önümüzdeki 14 gün). Uygunluğu doktor kendi sayfasından
          düzenler.
        </p>
      )}
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
