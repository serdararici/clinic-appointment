import { useState } from 'react'
import { deleteAvailability } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import type { Availability } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Modal } from '../../components/Modal'
import { Table, type Column } from '../../components/Table'
import { formatDate, formatLongDate, formatTime } from '../../utils/format'
import { slotCount } from './availability-utils'
import { useToast } from '../../components/useToast'

interface AvailabilityListProps {
  doctorId: string
  /** Upcoming ranges, or undefined while loading. */
  rows: Availability[] | undefined
  loading: boolean
  error: string | null
  slotDurationMinutes: number
  onReload: () => void
}

const rangeMinutes = (row: Availability) => (new Date(row.endUtc).getTime() - new Date(row.startUtc).getTime()) / 60_000

/** The doctor's upcoming availability ranges, each with its number of slots and a delete button. */
export function AvailabilityList({ doctorId, rows, loading, error, slotDurationMinutes, onReload }: AvailabilityListProps) {
  const toast = useToast()
  const [toDelete, setToDelete] = useState<Availability | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const closeModal = () => {
    if (deleting) return
    setToDelete(null)
    setDeleteError(null)
  }

  const confirmDelete = async () => {
    if (!toDelete) return
    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteAvailability(doctorId, toDelete.id)
      toast.success('Uygunluk silindi.')
      setToDelete(null)
      onReload()
    } catch (caught) {
      // For example 409: the range still has active appointments.
      setDeleteError(readableMessage(caught))
    } finally {
      setDeleting(false)
    }
  }

  const columns: Column<Availability>[] = [
    { key: 'date', header: 'Tarih', primary: true, cell: (row) => <span className="tabular-nums">{formatDate(row.startUtc)}</span> },
    { key: 'time', header: 'Saat aralığı', cell: (row) => <span className="tabular-nums">{formatTime(row.startUtc)} – {formatTime(row.endUtc)}</span> },
    { key: 'slots', header: 'Slot sayısı', align: 'right', cell: (row) => slotCount(rangeMinutes(row), slotDurationMinutes) },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <Button variant="secondary" onClick={() => setToDelete(row)}>
          Sil
        </Button>
      ),
    },
  ]

  return (
    <>
      <Table
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        error={error}
        onRetry={onReload}
        emptyTitle="Yaklaşan uygunluk yok"
        emptyDescription="Yukarıdan tek aralık veya toplu uygunluk ekleyebilirsiniz."
      />

      <Modal
        open={toDelete !== null}
        onClose={closeModal}
        title="Uygunluğu sil"
        footer={
          <>
            <Button variant="secondary" onClick={closeModal} disabled={deleting}>
              Vazgeç
            </Button>
            <Button variant="danger" loading={deleting} onClick={confirmDelete}>
              Sil
            </Button>
          </>
        }
      >
        {toDelete ? (
          <div className="space-y-3">
            <p className="text-slate-700">
              <span className="font-medium tabular-nums">
                {formatLongDate(toDelete.startUtc)}, {formatTime(toDelete.startUtc)} – {formatTime(toDelete.endUtc)}
              </span>{' '}
              aralığı silinecek. Bu aralığa randevu alınamaz hale gelir.
            </p>
            {deleteError ? <Alert tone="error">{deleteError}</Alert> : null}
          </div>
        ) : null}
      </Modal>
    </>
  )
}
