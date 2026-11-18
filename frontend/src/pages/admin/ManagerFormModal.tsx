import { useState, type FormEvent } from 'react'
import { createManager, updateManager } from '../../api/admin'
import { readableMessage } from '../../api/errors'
import type { BranchSummary, ClinicManagerAccount } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'
import { Select } from '../../components/Select'

interface ManagerFormModalProps {
  open: boolean
  branches: readonly BranchSummary[]
  /** The manager being edited; null adds a new one. */
  manager: ClinicManagerAccount | null
  onClose: () => void
  onSaved: () => void
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const MIN_PASSWORD_LENGTH = 8

export function ManagerFormModal({ open, branches, manager, onClose, onSaved }: ManagerFormModalProps) {
  const [busy, setBusy] = useState(false)
  return (
    <Modal
      open={open}
      onClose={onClose}
      dismissible={!busy}
      title={manager ? 'Yöneticiyi düzenle' : 'Yönetici ekle'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
          <Button type="submit" form="manager-form" loading={busy}>
            Kaydet
          </Button>
        </>
      }
    >
      {open ? <ManagerForm branches={branches} manager={manager} onSaved={onSaved} onBusy={setBusy} /> : null}
    </Modal>
  )
}

/** Add a clinic manager (name, email, temporary password, branch) or edit one (name, email, phone). */
function ManagerForm({ branches, manager, onSaved, onBusy }: { branches: readonly BranchSummary[]; manager: ClinicManagerAccount | null; onSaved: () => void; onBusy: (busy: boolean) => void }) {
  const [name, setName] = useState(manager?.fullName ?? '')
  const [email, setEmail] = useState(manager?.email ?? '')
  const [phone, setPhone] = useState(manager?.phoneNumber ?? '')
  const [password, setPassword] = useState('')
  const [branchId, setBranchId] = useState('')
  const [errors, setErrors] = useState<{ name?: string; email?: string; password?: string; branch?: string }>({})
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!name.trim()) found.name = 'Ad soyad zorunludur.'
    if (!EMAIL_PATTERN.test(email.trim())) found.email = 'Geçerli bir e-posta adresi girin.'
    if (!manager && password.length < MIN_PASSWORD_LENGTH) found.password = `Geçici şifre en az ${MIN_PASSWORD_LENGTH} karakter olmalıdır.`
    if (!manager && !branchId) found.branch = 'Şube seçin.'
    setErrors(found)
    if (Object.keys(found).length > 0) return

    onBusy(true)
    try {
      if (manager) await updateManager(manager.id, { fullName: name.trim(), email: email.trim(), phoneNumber: phone.trim() })
      else await createManager(branchId, { fullName: name.trim(), email: email.trim(), password, phoneNumber: phone.trim() || undefined })
      onSaved()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      onBusy(false)
    }
  }

  return (
    <form id="manager-form" onSubmit={submit} noValidate className="space-y-3">
      <Input label="Ad soyad" value={name} onChange={(event) => setName(event.target.value)} error={errors.name} autoFocus />
      <Input label="E-posta" type="email" value={email} onChange={(event) => setEmail(event.target.value)} error={errors.email} />
      <Input label="Telefon (isteğe bağlı)" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
      {manager ? null : (
        <>
          <Input
            label="Geçici şifre"
            autoComplete="off"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            error={errors.password}
            hint="Yönetici bu e-posta ve şifreyle giriş yapar. Şifreyi yöneticiye iletin."
          />
          <Select
            label="Şube"
            value={branchId}
            onChange={(event) => setBranchId(event.target.value)}
            placeholder="Seçiniz"
            options={branches.map((branch) => ({ value: branch.id, label: `${branch.organizationName} · ${branch.name}` }))}
            error={errors.branch}
          />
        </>
      )}
      {apiError ? <Alert tone="error">{apiError}</Alert> : null}
    </form>
  )
}
