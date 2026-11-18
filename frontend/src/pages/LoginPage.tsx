import { useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { readableMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'
import { Alert } from '../components/Alert'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Input } from '../components/Input'
import { AuthLayout } from '../layout/AuthLayout'
import { DemoAccounts } from './DemoAccounts'

// What other pages pass to the login page through the router state.
interface LoginLocationState {
  /** A notice to show above the form (registration done, session expired). */
  notice?: string
}

export function LoginPage() {
  const { login } = useAuth()
  const location = useLocation()
  const notice = (location.state as LoginLocationState | null)?.notice

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!email.trim() || !password) {
      setError('E-posta ve şifre zorunludur.')
      return
    }

    setSubmitting(true)
    try {
      // On success the user state changes and <GuestOnly> moves us on to the right page.
      await login(email.trim(), password)
    } catch (caught) {
      setError(readableMessage(caught))
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Giriş yap"
      subtitle="Randevularınızı görmek ve yönetmek için hesabınızla giriş yapın."
      footer={
        <p className="text-sm text-slate-600">
          Hesabınız yok mu?{' '}
          <Link to="/kayit" className="font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800">
            Hasta olarak kayıt olun
          </Link>
        </p>
      }
      demo={
        <DemoAccounts
          onPick={(pickedEmail, pickedPassword) => {
            setEmail(pickedEmail)
            setPassword(pickedPassword)
            setError(null)
            // The form is above the demo list: bring it back into view with the account filled in.
            window.scrollTo({ top: 0 })
          }}
        />
      }
    >
      <Card>
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          {notice ? <Alert tone="success">{notice}</Alert> : null}
          {error ? <Alert tone="error">{error}</Alert> : null}

          <Input label="E-posta" type="email" autoComplete="username" size="lg" value={email} onChange={(event) => setEmail(event.target.value)} />
          <Input label="Şifre" type="password" autoComplete="current-password" size="lg" value={password} onChange={(event) => setPassword(event.target.value)} />

          <Button type="submit" size="lg" loading={submitting} className="w-full">
            Giriş yap
          </Button>
        </form>
      </Card>
    </AuthLayout>
  )
}
