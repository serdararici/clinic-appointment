import { useCallback, useState } from 'react'
import { getDoctorProfile, listDoctorAppointments } from '../../api/doctor'
import type { AppointmentDetail } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Badge } from '../../components/Badge'
import { Card } from '../../components/Card'
import { LinkButton } from '../../components/LinkButton'
import { StatusBadge } from '../../components/StatusBadge'
import { Table, type Column } from '../../components/Table'
import { formatLongDate, formatTime } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { sortToday, sortUpcoming } from '../patient/appointment-utils'
import { usePageTitle } from '../../utils/usePageTitle'

const todayColumns: Column<AppointmentDetail>[] = [
  { key: 'time', header: 'Saat', primary: true, cell: (row) => <span className="font-medium tabular-nums">{formatTime(row.scheduledStartUtc)}</span> },
  { key: 'patient', header: 'Hasta', cell: (row) => row.patientName },
  { key: 'service', header: 'Hizmet', cell: (row) => row.serviceName },
  { key: 'status', header: 'Durum', cell: (row) => <StatusBadge status={row.status} /> },
]

/** The doctor's home: who and where they are, today's appointments and a short look at what is coming. */
export function DoctorHome() {
  usePageTitle('Ana sayfa')
  const { user } = useAuth()
  const doctorId = user?.id ?? ''
  const profile = useAsync(useCallback((signal: AbortSignal) => getDoctorProfile(doctorId, signal), [doctorId]))
  const appointments = useAsync(useCallback((signal: AbortSignal) => listDoctorAppointments(doctorId, signal), [doctorId]))
  const [now] = useState(() => Date.now())

  const today = appointments.data ? sortToday(appointments.data) : undefined
  const upcoming = appointments.data ? sortUpcoming(appointments.data, now) : undefined
  const next = upcoming?.[0]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user?.name}</h1>
          {/* The department is the doctor's specialty; if the profile cannot be loaded the greeting still works. */}
          {profile.data ? <Badge tone="teal">{profile.data.departmentName}</Badge> : null}
        </div>
        <LinkButton to="/doktor/randevular" variant="secondary">
          Tüm randevular
        </LinkButton>
      </div>

      <section aria-labelledby="today-heading" className="space-y-3">
        <h2 id="today-heading" className="text-base font-semibold text-slate-900">
          Bugünün randevuları
          {today ? <span className="ml-2 font-normal tabular-nums text-slate-600">({today.length})</span> : null}
        </h2>
        <Table
          columns={todayColumns}
          rows={today}
          rowKey={(row) => row.id}
          loading={appointments.loading}
          error={appointments.error}
          onRetry={appointments.reload}
          emptyTitle="Bugün için randevu yok"
          emptyDescription="Yeni randevular oluştuğunda burada listelenir."
        />
      </section>

      <Card title="Yaklaşan randevular">
        {appointments.loading || appointments.error ? (
          <p className="text-sm text-slate-600">{appointments.error ?? 'Yükleniyor...'}</p>
        ) : (
          <dl className="grid gap-4 text-sm sm:grid-cols-2">
            <div>
              <dt className="text-slate-600">Toplam</dt>
              <dd className="text-2xl font-semibold tabular-nums text-slate-900">{upcoming?.length ?? 0}</dd>
            </div>
            <div>
              <dt className="text-slate-600">Sıradaki randevu</dt>
              <dd className="text-slate-900">
                {next ? (
                  <>
                    <span className="font-medium tabular-nums">
                      {formatLongDate(next.scheduledStartUtc)}, {formatTime(next.scheduledStartUtc)}
                    </span>
                    <span className="block text-slate-700">
                      {next.patientName} · {next.serviceName}
                    </span>
                  </>
                ) : (
                  'Yaklaşan randevu yok'
                )}
              </dd>
            </div>
          </dl>
        )}
      </Card>
    </div>
  )
}
