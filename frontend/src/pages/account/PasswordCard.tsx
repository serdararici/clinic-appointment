import { useState, type FormEvent } from 'react'
import { changePassword } from '../../api/account'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { useToast } from '../../components/useToast'

// The same limits as registration (the API checks them too).
const MIN_PASSWORD_LENGTH = 8
const MAX_PASSWORD_LENGTH = 128

/** "Şifre değiştir": current password, new password and a repeat; the checks that need no server run first. */
export function PasswordCard() {
  const toast = useToast()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [repeat, setRepeat] = useState('')
  const [errors, setErrors] = useState<{ current?: string; next?: string; repeat?: string }>({})
  const [saving, setSaving] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)

    const found: typeof errors = {}
    if (!current) found.current = 'Mevcut şifrenizi girin.'
    if (next.length < MIN_PASSWORD_LENGTH) found.next = `Yeni şifre en az ${MIN_PASSWORD_LENGTH} karakter olmalıdır.`
    else if (next.length > MAX_PASSWORD_LENGTH) found.next = `Yeni şifre en fazla ${MAX_PASSWORD_LENGTH} karakter olabilir.`
    else if (next === current) found.next = 'Yeni şifre mevcut şifreden farklı olmalıdır.'
    if (next !== repeat) found.repeat = 'Şifreler eşleşmiyor.'
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      await changePassword({ currentPassword: current, newPassword: next })
      setCurrent('')
      setNext('')
      setRepeat('')
      toast.success('Şifreniz değiştirildi.')
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Şifre değiştir">
      <form onSubmit={submit} noValidate className="max-w-md space-y-4">
        <Input label="Mevcut şifre" type="password" autoComplete="current-password" value={current} onChange={(event) => setCurrent(event.target.value)} error={errors.current} />
        <Input
          label="Yeni şifre"
          type="password"
          autoComplete="new-password"
          value={next}
          onChange={(event) => setNext(event.target.value)}
          error={errors.next}
          hint={`En az ${MIN_PASSWORD_LENGTH} karakter.`}
        />
        <Input label="Yeni şifre (tekrar)" type="password" autoComplete="new-password" value={repeat} onChange={(event) => setRepeat(event.target.value)} error={errors.repeat} />
        {apiError ? <Alert tone="error">{apiError}</Alert> : null}
        <Button type="submit" loading={saving}>
          Şifreyi değiştir
        </Button>
      </form>
    </Card>
  )
}
