import { useCallback, useState } from 'react'
import { cancelAppointment, listPatientAppointments } from '../../api/booking'
import { readableMessage } from '../../api/errors'
import type { AppointmentDetail } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { LinkButton } from '../../components/LinkButton'
import { Modal } from '../../components/Modal'
import { StatusBadge } from '../../components/StatusBadge'
import { Table, type Column } from '../../components/Table'
import { useAuth } from '../../auth/useAuth'
import { cx } from '../../utils/cx'
import { formatCurrency, formatDateTime, formatPlace } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { ServiceLabel } from './ServiceLabel'
import { isUpcoming, sortPast, sortUpcoming } from './appointment-utils'

type Tab = 'upcoming' | 'past'

const TABS: ReadonlyArray<{ id: Tab; label: string }> = [
  { id: 'upcoming', label: 'Yaklaşan' },
  { id: 'past', label: 'Geçmiş' },
]

/** The patient's own appointments: upcoming and past, with cancelling for the ones still active. */
export function MyAppointmentsPage() {
  const { user } = useAuth()
  const patientId = user?.id ?? ''
  const state = useAsync(useCallback((signal: AbortSignal) => listPatientAppointments(patientId, signal), [patientId]))

  const [tab, setTab] = useState<Tab>('upcoming')
  // The moment the page opened decides what is "upcoming"; reloading the page refreshes it.
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
    { key: 'branch', header: 'Kurum ve şube', cell: (row) => formatPlace(row) },
    { key: 'service', header: 'Hizmet', cell: (row) => <ServiceLabel serviceName={row.serviceName} departmentName={row.departmentName} /> },
    { key: 'staff', header: 'Doktor', cell: (row) => row.staffMemberName ?? <span className="text-slate-600">Atanmadı</span> },
    { key: 'price', header: 'Ücret', align: 'right', cell: (row) => formatCurrency(row.price) },
    { key: 'status', header: 'Durum', cell: (row) => <StatusBadge status={row.status} /> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) =>
        isUpcoming(row, now) ? (
          <Button variant="secondary" onClick={() => setToCancel(row)}>
            İptal et
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Randevularım</h1>
        <LinkButton to="/hasta/randevu-al">Randevu al</LinkButton>
      </div>

      <div role="tablist" aria-label="Randevu listesi" className="flex gap-1 border-b border-slate-200">
        {TABS.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            onClick={() => setTab(id)}
            className={cx(
              'min-h-11 border-b-2 px-4 text-sm font-medium transition-colors',
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
        emptyTitle={tab === 'upcoming' ? 'Yaklaşan randevunuz yok' : 'Geçmiş randevunuz yok'}
        emptyDescription={tab === 'upcoming' ? 'Yeni bir randevu alarak başlayabilirsiniz.' : 'Tamamlanan, iptal edilen veya reddedilen randevularınız burada listelenir.'}
        emptyAction={tab === 'upcoming' ? <LinkButton to="/hasta/randevu-al">Randevu al</LinkButton> : undefined}
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
              <span className="font-medium tabular-nums">{formatDateTime(toCancel.scheduledStartUtc)}</span> tarihli randevunuzu iptal etmek istediğinize emin misiniz?
            </p>
            <p className="text-slate-900">
              <ServiceLabel serviceName={toCancel.serviceName} departmentName={toCancel.departmentName} />
              <span className="block text-sm text-slate-600">{formatPlace(toCancel)}</span>
            </p>
            {cancelError ? <Alert tone="error">{cancelError}</Alert> : null}
          </div>
        ) : null}
      </Modal>
    </div>
  )
}
