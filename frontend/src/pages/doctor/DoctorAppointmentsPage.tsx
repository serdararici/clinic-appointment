import { useCallback, useState } from 'react'
import { cancelAppointment } from '../../api/booking'
import { confirmAppointment, getDoctorProfile, listDoctorAppointments, rejectAppointment } from '../../api/doctor'
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
import { isUpcoming, sortConfirmedUpcoming, sortPast, sortPending } from '../patient/appointment-utils'
import { usePageTitle } from '../../utils/usePageTitle'
import { useToast } from '../../components/useToast'
import { FilterBar } from '../../components/FilterBar'
import { Input } from '../../components/Input'
import { Select } from '../../components/Select'
import { matchesQuery } from '../../utils/search'
import { STATUS_OPTIONS } from '../../components/appointment-status'
import { Check, X } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { ApprovalCard } from './ApprovalCard'
import { ConfirmDialog } from '../../components/ConfirmDialog'

type Tab = 'pending' | 'upcoming' | 'past'

const TABS: ReadonlyArray<{ id: Tab; label: string }> = [
  { id: 'pending', label: 'Bekleyen' },
  { id: 'upcoming', label: 'Yaklaşan' },
  { id: 'past', label: 'Geçmiş' },
]

const TAB_PARAM: Record<string, Tab> = { bekleyen: 'pending', yaklasan: 'upcoming', gecmis: 'past' }

/**
 * The doctor's own appointments in three tabs: Bekleyen (waiting for their decision: Onayla / Reddet), Yaklaşan (confirmed,
 * can be cancelled) and Geçmiş. At the top, the switch that decides whether new appointments wait for approval.
 */
export function DoctorAppointmentsPage() {
  usePageTitle('Randevular')
  const { user } = useAuth()
  const toast = useToast()
  const doctorId = user?.id ?? ''
  const state = useAsync(useCallback((signal: AbortSignal) => listDoctorAppointments(doctorId, signal), [doctorId]))
  const profile = useAsync(useCallback((signal: AbortSignal) => getDoctorProfile(doctorId, signal), [doctorId]))
  const [searchParams] = useSearchParams()
  const [autoConfirm, setAutoConfirmState] = useState<boolean | null>(null)
  const autoConfirmEnabled = autoConfirm ?? profile.data?.autoConfirmAppointments

  // The tab the user picked, or the one in the address (?tab=bekleyen, from the home page).
  const [pickedTab, setPickedTab] = useState<Tab | null>(() => TAB_PARAM[searchParams.get('tab') ?? ''] ?? null)
  const [search, setSearch] = useState('')
  const [serviceFilter, setServiceFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [now] = useState(() => Date.now())
  const [toCancel, setToCancel] = useState<AppointmentDetail | null>(null)
  const [cancelling, setCancelling] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)
  const [toReject, setToReject] = useState<AppointmentDetail | null>(null)
  const [confirmingId, setConfirmingId] = useState<string | null>(null)

  const pending = state.data ? sortPending(state.data, now) : undefined
  // With nothing picked yet, waiting appointments come first; otherwise the usual "Yaklaşan".
  const tab: Tab = pickedTab ?? ((pending?.length ?? 0) > 0 ? 'pending' : 'upcoming')
  const setTab = (next: Tab) => setPickedTab(next)

  const rows = state.data
    ? tab === 'pending'
      ? pending
      : tab === 'upcoming'
        ? sortConfirmedUpcoming(state.data, now)
        : sortPast(state.data, now)
    : undefined

  const approve = async (appointment: AppointmentDetail) => {
    setConfirmingId(appointment.id)
    try {
      await confirmAppointment(appointment.id)
      toast.success('Randevu onaylandı.')
      state.reload()
    } catch (error) {
      toast.error(readableMessage(error))
    } finally {
      setConfirmingId(null)
    }
  }

  const filtered = rows?.filter(
    (row) => matchesQuery(search, [row.patientName]) && (!serviceFilter || row.serviceName === serviceFilter) && (!statusFilter || row.status === statusFilter),
  )
  const serviceNames = [...new Set((state.data ?? []).map((row) => row.serviceName))].sort((a, b) => a.localeCompare(b, 'tr-TR'))
  const filtersActive = Boolean(search.trim() || serviceFilter || statusFilter)
  const noMatch = (rows?.length ?? 0) > 0 && filtered?.length === 0
  const clearFilters = () => {
    setSearch('')
    setServiceFilter('')
    setStatusFilter('')
  }

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
      toast.success('Randevu iptal edildi. Hasta randevuyu “İptal edildi” durumunda görecek.')
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
      // A waiting appointment is decided here; an upcoming confirmed one can still be cancelled.
      cell: (row) =>
        row.status === 'Pending' && isUpcoming(row, now) ? (
          <span className="inline-flex flex-wrap justify-end gap-2">
            <Button icon={Check} loading={confirmingId === row.id} disabled={confirmingId !== null} onClick={() => approve(row)}>
              Onayla
            </Button>
            <Button variant="secondary" icon={X} disabled={confirmingId !== null} onClick={() => setToReject(row)}>
              Reddet
            </Button>
          </span>
        ) : isUpcoming(row, now) && row.status === 'Confirmed' ? (
          <Button variant="secondary" onClick={() => setToCancel(row)}>
            İptal et
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Randevular</h1>

      <ApprovalCard
        doctorId={doctorId}
        enabled={autoConfirmEnabled}
        loadError={profile.error}
        onChanged={setAutoConfirmState}
      />

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
            {id === 'pending' && (pending?.length ?? 0) > 0 ? (
              <span className="ml-2 inline-flex min-w-5 items-center justify-center rounded-full bg-amber-100 px-1.5 text-xs font-semibold tabular-nums text-amber-800">
                {pending?.length}
                <span className="sr-only"> bekleyen randevu</span>
              </span>
            ) : null}
          </button>
        ))}
      </div>

      {rows && rows.length > 0 ? (
        <FilterBar count={filtered?.length ?? 0} active={filtersActive} onClear={clearFilters}>
          <Input label="Hasta ara" type="search" placeholder="Hasta adı" value={search} onChange={(event) => setSearch(event.target.value)} />
          <Select
            label="Hizmet"
            placeholder="Tümü"
            value={serviceFilter}
            onChange={(event) => setServiceFilter(event.target.value)}
            options={serviceNames.map((name) => ({ value: name, label: name }))}
          />
          <Select label="Durum" placeholder="Tümü" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} options={STATUS_OPTIONS} />
        </FilterBar>
      ) : null}

      <Table
        columns={columns}
        rows={filtered}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle={tab === 'pending' ? 'Onay bekleyen randevu yok' : tab === 'upcoming' ? 'Yaklaşan randevu yok' : 'Geçmiş randevu yok'}
        emptyDescription={
          tab === 'pending'
            ? autoConfirmEnabled === false
              ? 'Yeni randevular onayınız için burada listelenir.'
              : 'Randevular otomatik onaylanıyor. Onay istemek için yukarıdaki ayarı kapatın.'
            : tab === 'upcoming'
              ? 'Onaylanmış randevular burada listelenir.'
              : 'Tamamlanan, iptal edilen veya reddedilen randevular burada listelenir.'
        }
        filtersHideAll={noMatch}
        onClearFilters={clearFilters}
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

      <ConfirmDialog
        open={toReject !== null}
        title="Randevuyu reddet"
        confirmLabel="Randevuyu reddet"
        danger
        onClose={() => setToReject(null)}
        onConfirm={async () => {
          if (!toReject) return
          await rejectAppointment(toReject.id)
          toast.success('Randevu reddedildi.')
          state.reload()
        }}
      >
        {toReject ? (
          <p>
            <span className="font-medium">{toReject.patientName}</span> adlı hastanın{' '}
            <span className="font-medium tabular-nums">{formatDateTime(toReject.scheduledStartUtc)}</span> tarihli {toReject.serviceName} randevusu reddedilecek. Hasta bu
            randevuyu “Reddedildi” durumunda görecek ve saat yeniden boşa çıkacak.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
