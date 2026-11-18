import { useState, type FormEvent } from 'react'
import { updateAccount } from '../../api/account'
import { readableMessage } from '../../api/errors'
import type { Account } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { useToast } from '../../components/useToast'

const MAX_PHONE_LENGTH = 30
const MAX_INSURANCE_LENGTH = 50

/**
 * "Kişisel bilgiler": name and phone (and the insurance number of a patient) can be edited, the email is shown
 * read-only. After saving, the name in the navbar and greetings is refreshed from the API's answer.
 */
export function ProfileCard({ account, onSaved }: { account: Account; onSaved: (account: Account) => void }) {
  const { updateName } = useAuth()
  const toast = useToast()
  const isPatient = account.role === 'Patient'

  const [name, setName] = useState(account.fullName)
  const [phone, setPhone] = useState(account.phoneNumber)
  const [insurance, setInsurance] = useState(account.insuranceNumber ?? '')
  const [errors, setErrors] = useState<{ name?: string; phone?: string; insurance?: string }>({})
  const [saving, setSaving] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!name.trim()) found.name = 'Ad soyad zorunludur.'
    if (phone.trim().length > MAX_PHONE_LENGTH) found.phone = `Telefon numarası en fazla ${MAX_PHONE_LENGTH} karakter olabilir.`
    if (isPatient && insurance.trim().length > MAX_INSURANCE_LENGTH) found.insurance = `Sigorta numarası en fazla ${MAX_INSURANCE_LENGTH} karakter olabilir.`
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      const updated = await updateAccount({ fullName: name.trim(), phoneNumber: phone.trim(), insuranceNumber: isPatient ? insurance.trim() : undefined })
      updateName(updated.fullName)
      onSaved(updated)
      setName(updated.fullName)
      setPhone(updated.phoneNumber)
      setInsurance(updated.insuranceNumber ?? '')
      toast.success('Bilgileriniz kaydedildi.')
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Kişisel bilgiler">
      <form onSubmit={submit} noValidate className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Input label="Ad soyad" value={name} onChange={(event) => setName(event.target.value)} error={errors.name} />
          <Input label="Telefon" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} error={errors.phone} />
          <Input label="E-posta" value={account.email} readOnly disabled hint="E-posta adresi değiştirilemez." />
          {isPatient ? (
            <Input
              label="Sigorta numarası"
              value={insurance}
              onChange={(event) => setInsurance(event.target.value)}
              error={errors.insurance}
              hint="Sigorta numaranız randevu özetinde gördüğünüz ödenecek tutarı etkiler. Boş bırakırsanız tam ücret gösterilir."
            />
          ) : null}
        </div>
        {apiError ? <Alert tone="error">{apiError}</Alert> : null}
        <Button type="submit" loading={saving}>
          Kaydet
        </Button>
      </form>
    </Card>
  )
}
