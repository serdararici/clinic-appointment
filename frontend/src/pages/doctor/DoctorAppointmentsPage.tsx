import { useCallback, useState } from 'react'
import { cancelAppointment } from '../../api/booking'
import { listDoctorAppointments } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import type { AppointmentDetail } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Modal } from '../../components/Modal'
import { StatusBadge } from '../../components/StatusBadge'
import { Table, type Column } from '../../components/Table'
import { cx } from '../../utils/cx'
import { formatCurrency, formatDateTime, formatPlace } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { isUpcoming, sortPast, sortUpcoming } from '../patient/appointment-utils'

type Tab = 'upcoming' | 'past'

const TABS: ReadonlyArray<{ id: Tab; label: string }> = [
  { id: 'upcoming', label: 'Yaklaşan' },
  { id: 'past', label: 'Geçmiş' },
]

/** The doctor's own appointments, upcoming and past. A confirmed upcoming one can be cancelled. */
export function DoctorAppointmentsPage() {
  const { user } = useAuth()
  const doctorId = user?.id ?? ''
  const state = useAsync(useCallback((signal: AbortSignal) => listDoctorAppointments(doctorId, signal), [doctorId]))

  const [tab, setTab] = useState<Tab>('upcoming')
  const [now] = useState(() => Date.now())
  const [toCancel, setToCancel] = useState<AppointmentDetail | null>(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)

  const rows = state.data ? (tab === 'upcoming' ? sortUpcoming(state.data, now) : sortPast(state.data, now)) : undefined

  const closeModal = () => {
    if (cancelling) return
    setToCancel(null)
    setCancelError(null)
  }

  const confirmCancel = async () => {
    if (!toCancel) return
    setCancelling(true)
    setCancelError(null)
    try {
      await cancelAppointment(toCancel.id)
      setToCancel(null)
      state.reload()
    } catch (error) {
      setCancelError(readableMessage(error))
    } finally {
      setCancelling(false)
    }
  }

  const columns: Column<AppointmentDetail>[] = [
    { key: 'when', header: 'Tarih ve saat', primary: true, cell: (row) => <span className="tabular-nums">{formatDateTime(row.scheduledStartUtc)}</span> },
    { key: 'patient', header: 'Hasta', cell: (row) => row.patientName },
    { key: 'service', header: 'Hizmet', cell: (row) => row.serviceName },
    { key: 'place', header: 'Kurum ve şube', cell: (row) => formatPlace(row) },
    { key: 'price', header: 'Ücret', align: 'right', cell: (row) => formatCurrency(row.price) },
    { key: 'status', header: 'Durum', cell: (row) => <StatusBadge status={row.status} /> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      // Only an upcoming confirmed appointment can be cancelled from here.
      cell: (row) =>
        isUpcoming(row, now) && row.status === 'Confirmed' ? (
          <Button variant="secondary" onClick={() => setToCancel(row)}>
            İptal et
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Randevular</h1>

      <div role="tablist" aria-label="Randevu listesi" className="flex gap-1 border-b border-slate-200">
        {TABS.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            onClick={() => setTab(id)}
            className={cx(
              'h-10 border-b-2 px-4 text-sm font-medium transition-colors',
              tab === id ? 'border-teal-700 text-teal-800' : 'border-transparent text-slate-600 hover:text-slate-900',
            )}
          >
            {label}
          </button>
        ))}
      </div>

      <Table
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle={tab === 'upcoming' ? 'Yaklaşan randevu yok' : 'Geçmiş randevu yok'}
        emptyDescription={tab === 'upcoming' ? 'Yeni randevular oluştuğunda burada listelenir.' : 'Tamamlanan, iptal edilen veya reddedilen randevular burada listelenir.'}
      />

      <Modal
        open={toCancel !== null}
        onClose={closeModal}
        title="Randevuyu iptal et"
        footer={
          <>
            <Button variant="secondary" onClick={closeModal} disabled={cancelling}>
              Vazgeç
            </Button>
            <Button variant="danger" loading={cancelling} onClick={confirmCancel}>
              Randevuyu iptal et
            </Button>
          </>
        }
      >
        {toCancel ? (
          <div className="space-y-3">
            <p className="text-slate-700">
              <span className="font-medium">{toCancel.patientName}</span> adlı hastanın{' '}
              <span className="font-medium tabular-nums">{formatDateTime(toCancel.scheduledStartUtc)}</span> tarihli {toCancel.serviceName} randevusu iptal edilecek.
            </p>
            <Alert tone="info">Hasta bu randevuyu “İptal edildi” durumunda görecek. Bu işlem geri alınamaz.</Alert>
            {cancelError ? <Alert tone="error">{cancelError}</Alert> : null}
          </div>
        ) : null}
      </Modal>
    </div>
  )
}
