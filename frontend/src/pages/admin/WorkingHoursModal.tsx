import { useCallback, useState, type FormEvent } from 'react'
import { addWorkingHour, getWorkingHours, removeWorkingHour } from '../../api/admin'
import { readableMessage } from '../../api/errors'
import type { WeekdayName, WorkingHour } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { Modal } from '../../components/Modal'
import { Select } from '../../components/Select'
import { Table, type Column } from '../../components/Table'
import { minutesOf } from '../doctor/availability-utils'
import { useAsync } from '../../utils/useAsync'
import { DAY_ORDER, shortTime } from './admin-utils'

interface WorkingHoursModalProps {
  open: boolean
  branchId: string
  branchName: string
  onClose: () => void
  /** Called after every change so the page can refresh its summary. */
  onChanged: () => void
}

/** The weekly opening hours of a branch: the list with delete buttons and a form that adds a range. */
export function WorkingHoursModal({ open, branchId, branchName, onClose, onChanged }: WorkingHoursModalProps) {
  return (
    <Modal open={open} onClose={onClose} title={`Çalışma saatleri · ${branchName}`} footer={<Button onClick={onClose}>Kapat</Button>}>
      {open ? <WorkingHoursEditor branchId={branchId} onChanged={onChanged} /> : null}
    </Modal>
  )
}

function WorkingHoursEditor({ branchId, onChanged }: { branchId: string; onChanged: () => void }) {
  const state = useAsync(useCallback((signal: AbortSignal) => getWorkingHours(branchId, signal), [branchId]))
  const [day, setDay] = useState<WeekdayName>('Monday')
  const [opens, setOpens] = useState('08:00')
  const [closes, setCloses] = useState('17:00')
  const [error, setError] = useState<string | undefined>()
  const [apiError, setApiError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const changed = () => {
    state.reload()
    onChanged()
  }

  const add = async (event: FormEvent) => {
    event.preventDefault()
    setApiError(null)
    if (!opens || !closes || minutesOf(closes) <= minutesOf(opens)) {
      setError('Kapanış saati açılış saatinden sonra olmalıdır.')
      return
    }
    setError(undefined)
    setBusy(true)
    try {
      await addWorkingHour(branchId, { dayOfWeek: day, opensAt: opens, closesAt: closes })
      changed()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      setBusy(false)
    }
  }

  const remove = async (hour: WorkingHour) => {
    setApiError(null)
    setBusy(true)
    try {
      await removeWorkingHour(branchId, hour.id)
      changed()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      setBusy(false)
    }
  }

  const columns: Column<WorkingHour>[] = [
    { key: 'day', header: 'Gün', primary: true, cell: (row) => DAY_ORDER.find((entry) => entry.name === row.dayOfWeek)?.label ?? row.dayOfWeek },
    { key: 'hours', header: 'Saat aralığı', cell: (row) => <span className="tabular-nums">{shortTime(row.opensAt)} – {shortTime(row.closesAt)}</span> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <Button variant="ghost" disabled={busy} onClick={() => remove(row)}>
          Sil
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-600">Saatler Türkiye saatidir. Çalışma saati olmayan günler kapalı sayılır; hiç aralık yoksa şubede randevu alınamaz.</p>
      <Table
        columns={columns}
        rows={state.data}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle="Çalışma saati tanımlı değil"
        emptyDescription="Aşağıdan bir aralık ekleyin."
      />
      <form onSubmit={add} noValidate className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-3">
          <Select
            label="Gün"
            value={day}
            onChange={(event) => setDay(event.target.value as WeekdayName)}
            options={DAY_ORDER.map((entry) => ({ value: entry.name, label: entry.label }))}
          />
          <Input label="Açılış" type="time" value={opens} onChange={(event) => setOpens(event.target.value)} />
          <Input label="Kapanış" type="time" value={closes} onChange={(event) => setCloses(event.target.value)} error={error} />
        </div>
        {apiError ? <Alert tone="error">{apiError}</Alert> : null}
        <Button type="submit" variant="secondary" loading={busy}>
          Aralık ekle
        </Button>
      </form>
    </div>
  )
}
