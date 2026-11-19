import { useCallback, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { getAccount } from '../../api/account'
import type { AppointmentDetail } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { EmptyState } from '../../components/EmptyState'
import { LinkButton } from '../../components/LinkButton'
import { StatusBadge } from '../../components/StatusBadge'
import { firstNameOf } from '../../utils/avatar'
import { formatCurrency, formatLongDate, formatPlace, isoDateOf, todayIsoDate } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { usePageTitle } from '../../utils/usePageTitle'
import { AppointmentCalendar } from './AppointmentCalendar'
import { isActive, sortPast, sortUpcoming } from './appointment-utils'
import { ServiceLabel } from './ServiceLabel'
import { usePatientAppointments } from './usePatientAppointments'
import { CalendarClock, CalendarDays, CalendarPlus, ChartColumn, ClipboardList, History, ShieldCheck, type LucideIcon } from 'lucide-react'
import { Icon } from '../../components/Icon'
import { AppointmentTime } from '../../components/AppointmentTime'

/** Grey bars in the shape of a card's content while it loads. */
function Skeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div role="status" aria-label="Yükleniyor" className="space-y-3">
      {Array.from({ length: lines }, (_, index) => (
        <div key={index} className={`h-4 animate-pulse rounded bg-slate-100 ${index === 0 ? 'w-2/3' : index % 2 ? 'w-full' : 'w-5/6'}`} />
      ))}
    </div>
  )
}

function QuickActions() {
  return (
    <Card title="Hızlı işlemler" icon={CalendarPlus}>
      <div className="flex flex-col gap-3">
        <LinkButton to="/hasta/randevu-al" size="lg" icon={CalendarPlus}>
          Randevu al
        </LinkButton>
        <LinkButton to="/hasta/randevularim" variant="secondary" size="lg" icon={ClipboardList}>
          Randevularım
        </LinkButton>
      </div>
    </Card>
  )
}

/** The "Sıradaki randevunuz" card, unchanged in content. */
function NextAppointment({ next }: { next: AppointmentDetail | undefined }) {
  return (
    <Card title="Sıradaki randevunuz" icon={CalendarClock}>
      {next ? (
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-3">
            <p className="text-lg font-semibold tabular-nums text-slate-900">
              {formatLongDate(next.scheduledStartUtc)}, <AppointmentTime startUtc={next.scheduledStartUtc} endUtc={next.scheduledEndUtc} show="time" />
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
  )
}

function Counter({ label, value, hint, icon }: { label: string; value: ReactNode; hint: string; icon: LucideIcon }) {
  return (
    <div className="space-y-0.5">
      <dt className="flex items-center gap-1.5 text-sm text-slate-600">
        <Icon icon={icon} size={16} className="text-slate-400" />
        {label}
      </dt>
      <dd className="text-3xl font-semibold tabular-nums text-slate-900">{value}</dd>
      <dd className="text-xs text-slate-500">{hint}</dd>
    </div>
  )
}

/** Whether the patient has an insurance number (from the account call); it decides the price they see when booking. */
function InsuranceCard() {
  const account = useAsync(useCallback((signal: AbortSignal) => getAccount(signal), []))
  return (
    <Card title="Sigorta" icon={ShieldCheck}>
      {account.loading ? (
        <Skeleton lines={2} />
      ) : account.error ? (
        <div className="space-y-3">
          <Alert tone="error">{account.error}</Alert>
          <Button variant="secondary" onClick={account.reload}>
            Yeniden dene
          </Button>
        </div>
      ) : account.data?.insuranceNumber ? (
        <div className="space-y-1">
          <p className="font-semibold text-slate-900">Sigorta kaydınız var</p>
          <p className="text-sm text-slate-600">Randevu ücretleri sigortalı tutar üzerinden hesaplanır.</p>
        </div>
      ) : (
        <div className="space-y-2">
          <p className="font-semibold text-slate-900">Sigorta bilginiz yok</p>
          <p className="text-sm text-slate-600">Sigorta numaranızı eklerseniz ücretler sigortalı tutar üzerinden hesaplanır.</p>
          <Link to="/hesabim" className="inline-flex min-h-11 items-center text-sm font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800 sm:min-h-0">
            Hesabım’a git
          </Link>
        </div>
      )}
    </Card>
  )
}

/**
 * The patient's home: a greeting with today's date, the next appointment, a month calendar of all appointments and,
 * on the side, quick actions, counters and the insurance status. One appointment request feeds every card. On a phone
 * the order is quick actions, next appointment, calendar, then the rest.
 */
export function PatientHome() {
  usePageTitle('Ana sayfa')
  const { user } = useAuth()
  const state = usePatientAppointments()
  const [now] = useState(() => Date.now())
  const today = todayIsoDate()

  const appointments = state.data
  const upcoming = appointments ? sortUpcoming(appointments, now) : []
  const next = upcoming[0]
  const thisMonth = today.slice(0, 7)
  const counts = appointments
    ? {
        upcoming: upcoming.length,
        // This month's appointments (Turkey days) that still hold their time: cancelled and rejected ones are left out.
        thisMonth: appointments.filter((appointment) => isActive(appointment) && isoDateOf(appointment.scheduledStartUtc).startsWith(thisMonth)).length,
        past: sortPast(appointments, now).length,
      }
    : null

  const main = state.loading ? (
    <>
      <Card title="Sıradaki randevunuz" icon={CalendarClock}>
        <Skeleton />
      </Card>
      <Card title="Takvim" icon={CalendarDays}>
        <Skeleton lines={6} />
      </Card>
    </>
  ) : state.error ? (
    <Card>
      <div className="space-y-3">
        <Alert tone="error">{state.error}</Alert>
        <Button variant="secondary" onClick={state.reload}>
          Yeniden dene
        </Button>
      </div>
    </Card>
  ) : appointments && appointments.length === 0 ? (
    <Card>
      <EmptyState
        icon={CalendarDays}
        title="Henüz randevunuz yok"
        description="İlk randevunuzu birkaç adımda alabilirsiniz. Aldığınız randevular burada ve takvimde görünür."
        action={
          <LinkButton to="/hasta/randevu-al" size="lg">
            Randevu al
          </LinkButton>
        }
      />
    </Card>
  ) : appointments ? (
    <>
      <NextAppointment next={next} />
      <Card title="Takvim" icon={CalendarDays}>
        <AppointmentCalendar appointments={appointments} today={today} initialDay={next ? isoDateOf(next.scheduledStartUtc) : today} />
      </Card>
    </>
  ) : null

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user ? firstNameOf(user.name) : ''}</h1>
        <p className="text-slate-600">{formatLongDate(new Date(now).toISOString())}</p>
      </div>

      {/* Phone and tablet: the quick actions come first; from lg they sit at the top of the side column. */}
      <div className="lg:hidden">
        <QuickActions />
      </div>

      <div className="grid gap-6 lg:grid-cols-3 lg:items-start">
        <div className="min-w-0 space-y-6 lg:col-span-2">{main}</div>

        <div className="space-y-6">
          <div className="hidden lg:block">
            <QuickActions />
          </div>
          <Card title="Randevu özeti" icon={ChartColumn}>
            {counts ? (
              <dl className="grid grid-cols-3 gap-4">
                <Counter label="Yaklaşan" icon={CalendarClock} value={counts.upcoming} hint="aktif randevu" />
                <Counter label="Bu ay" icon={CalendarDays} value={counts.thisMonth} hint="iptal hariç" />
                <Counter label="Geçmiş" icon={History} value={counts.past} hint="tümü" />
              </dl>
            ) : state.error ? (
              <p className="text-sm text-slate-600">Randevular yüklenemedi.</p>
            ) : (
              <Skeleton lines={2} />
            )}
          </Card>
          <InsuranceCard />
        </div>
      </div>
    </div>
  )
}
