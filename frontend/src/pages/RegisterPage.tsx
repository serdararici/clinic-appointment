import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { registerRequest } from '../api/auth'
import { readableMessage } from '../api/errors'
import { Alert } from '../components/Alert'
import { todayIsoDate } from '../utils/format'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Input } from '../components/Input'
import { AuthLayout } from '../layout/AuthLayout'

const MIN_PASSWORD_LENGTH = 8 // the same rule the API enforces

interface FormValues {
  fullName: string
  email: string
  phoneNumber: string
  dateOfBirth: string
  insuranceNumber: string
  password: string
}

type FormErrors = Partial<Record<keyof FormValues, string>>

// Checks the form before calling the API, so mistakes are explained next to the field in Turkish.
// The API validates again; this is only for quicker feedback.
function validate(values: FormValues, today: string): FormErrors {
  const errors: FormErrors = {}

  if (!values.fullName.trim()) errors.fullName = 'Ad soyad zorunludur.'

  if (!values.email.trim()) errors.email = 'E-posta zorunludur.'
  else if (!/^\S+@\S+\.\S+$/.test(values.email.trim())) errors.email = 'Geçerli bir e-posta adresi girin.'

  if (!values.phoneNumber.trim()) errors.phoneNumber = 'Telefon numarası zorunludur.'

  if (!values.dateOfBirth) errors.dateOfBirth = 'Doğum tarihi zorunludur.'
  else if (values.dateOfBirth > today) errors.dateOfBirth = 'Doğum tarihi bugünden sonra olamaz.'

  if (values.password.length < MIN_PASSWORD_LENGTH) errors.password = `Şifre en az ${MIN_PASSWORD_LENGTH} karakter olmalıdır.`

  return errors
}

export function RegisterPage() {
  const navigate = useNavigate()
  // "YYYY-MM-DD" in Turkey (the same helper as everywhere else); the native date input needs this format for its max value.
  const today = todayIsoDate()

  const [values, setValues] = useState<FormValues>({
    fullName: '',
    email: '',
    phoneNumber: '',
    dateOfBirth: '',
    insuranceNumber: '',
    password: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Returns the props shared by every field: its value and the change handler.
  const field = (name: keyof FormValues) => ({
    value: values[name],
    error: errors[name],
    onChange: (event: { target: { value: string } }) => setValues((current) => ({ ...current, [name]: event.target.value })),
  })

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    const found = validate(values, today)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    try {
      await registerRequest({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        phoneNumber: values.phoneNumber.trim(),
        dateOfBirth: values.dateOfBirth,
        insuranceNumber: values.insuranceNumber.trim() || undefined,
      })
      navigate('/giris', { state: { notice: 'Kaydınız oluşturuldu. Şimdi giriş yapabilirsiniz.' } })
    } catch (caught) {
      setFormError(readableMessage(caught))
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Hasta kaydı"
      subtitle="Randevu almak için bir hesap oluşturun."
      footer={
        <p className="text-sm text-slate-600">
          Zaten hesabınız var mı?{' '}
          <Link to="/giris" className="font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800">
            Giriş yapın
          </Link>
        </p>
      }
    >
    <Card>
      <form onSubmit={handleSubmit} noValidate className="space-y-5">
        {formError ? <Alert tone="error">{formError}</Alert> : null}

        <Input label="Ad soyad" size="lg" autoComplete="name" required {...field('fullName')} />
        <Input label="E-posta" type="email" size="lg" autoComplete="email" required {...field('email')} />
        <Input label="Telefon" type="tel" size="lg" autoComplete="tel" required {...field('phoneNumber')} />
        <Input
          label="Doğum tarihi"
          type="date"
          size="lg"
          max={today}
          autoComplete="bday"
          required
          {...field('dateOfBirth')}
        />
        <Input
          label="Sigorta numarası"
          size="lg"
          hint="İsteğe bağlı. Sigortanız varsa girin."
          {...field('insuranceNumber')}
        />
        <Input
          label="Şifre"
          type="password"
          size="lg"
          autoComplete="new-password"
          hint={`En az ${MIN_PASSWORD_LENGTH} karakter.`}
          required
          {...field('password')}
        />

        <Button type="submit" size="lg" loading={submitting} className="w-full">
          Kayıt ol
        </Button>
      </form>
    </Card>
    </AuthLayout>
  )
}
