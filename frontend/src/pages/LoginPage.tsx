import { useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { readableMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'
import { Alert } from '../components/Alert'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Input } from '../components/Input'
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
    <div className="mx-auto grid max-w-5xl gap-6 lg:grid-cols-2 lg:items-start">
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Giriş yap</h1>
          <p className="mt-1 text-slate-600">Randevularınızı görmek ve yönetmek için hesabınızla giriş yapın.</p>
        </div>

        <Card>
          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            {notice ? <Alert tone="success">{notice}</Alert> : null}
            {error ? <Alert tone="error">{error}</Alert> : null}

            <Input
              label="E-posta"
              type="email"
              autoComplete="username"
              size="lg"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
            <Input
              label="Şifre"
              type="password"
              autoComplete="current-password"
              size="lg"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />

            <Button type="submit" size="lg" loading={submitting} className="w-full">
              Giriş yap
            </Button>
          </form>
        </Card>

        <p className="text-sm text-slate-600">
          Hesabınız yok mu?{' '}
          <Link to="/kayit" className="font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800">
            Hasta olarak kayıt olun
          </Link>
        </p>
      </div>

      <DemoAccounts
        onPick={(pickedEmail, pickedPassword) => {
          setEmail(pickedEmail)
          setPassword(pickedPassword)
          setError(null)
        }}
      />
    </div>
  )
}
