import { useCallback, useState } from 'react'
import { Link } from 'react-router-dom'
import { listPatientAppointments } from '../../api/booking'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { EmptyState } from '../../components/EmptyState'
import { LinkButton } from '../../components/LinkButton'
import { Spinner } from '../../components/Spinner'
import { StatusBadge } from '../../components/StatusBadge'
import { formatCurrency, formatLongDate, formatPlace, formatTime } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { sortUpcoming } from './appointment-utils'
import { ServiceLabel } from './ServiceLabel'
import { usePageTitle } from '../../utils/usePageTitle'

function BigAction({ to, title, description }: { to: string; title: string; description: string }) {
  return (
    <Link
      to={to}
      className="flex min-h-28 flex-col justify-center gap-1 rounded-lg border border-slate-200 bg-white p-5 transition-colors hover:border-teal-700 hover:bg-teal-50"
    >
      <span className="text-xl font-semibold text-slate-900">{title}</span>
      <span className="text-sm text-slate-600">{description}</span>
    </Link>
  )
}

/** The patient's home: the two main actions and the next upcoming appointment. */
export function PatientHome() {
  usePageTitle('Ana sayfa')
  const { user } = useAuth()
  const patientId = user?.id ?? ''
  const state = useAsync(useCallback((signal: AbortSignal) => listPatientAppointments(patientId, signal), [patientId]))
  const [now] = useState(() => Date.now())
  const next = state.data ? sortUpcoming(state.data, now)[0] : undefined

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user?.name}</h1>

      <div className="grid gap-4 sm:grid-cols-2">
        <BigAction to="/hasta/randevu-al" title="Randevu al" description="Şube, bölüm ve saat seçerek yeni randevu oluşturun." />
        <BigAction to="/hasta/randevularim" title="Randevularım" description="Yaklaşan ve geçmiş randevularınızı görüntüleyin." />
      </div>

      <Card title="Sıradaki randevunuz">
        {state.loading ? (
          <div className="flex justify-center py-6">
            <Spinner label="Yükleniyor..." />
          </div>
        ) : state.error ? (
          <div className="space-y-3">
            <Alert tone="error">{state.error}</Alert>
            <Button variant="secondary" onClick={state.reload}>
              Yeniden dene
            </Button>
          </div>
        ) : next ? (
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-3">
              <p className="text-lg font-semibold tabular-nums text-slate-900">
                {formatLongDate(next.scheduledStartUtc)}, {formatTime(next.scheduledStartUtc)}
              </p>
              <StatusBadge status={next.status} />
            </div>
            <p className="text-slate-900">
              <ServiceLabel serviceName={next.serviceName} departmentName={next.departmentName} />
            </p>
            <p className="text-slate-700">{formatPlace(next)}</p>
            <p className="text-sm text-slate-600">
              {next.staffMemberName ?? 'Doktor atanmadı'} · <span className="tabular-nums">{formatCurrency(next.price)}</span>
            </p>
          </div>
        ) : (
          <EmptyState
            title="Yaklaşan randevunuz yok"
            description="Yeni bir randevu alarak başlayabilirsiniz."
            action={<LinkButton to="/hasta/randevu-al">Randevu al</LinkButton>}
          />
        )}
      </Card>
    </div>
  )
}
