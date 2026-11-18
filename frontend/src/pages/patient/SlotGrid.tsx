import type { Slot } from '../../api/types'
import { Check } from 'lucide-react'
import { Icon } from '../../components/Icon'
import { cx } from '../../utils/cx'
import { formatTime } from '../../utils/format'

interface SlotGridProps {
  slots: readonly Slot[]
  selectedStartUtc: string | null
  onSelect: (slot: Slot) => void
}

/**
 * The time buttons of one day. States never rely on color alone:
 * available = neutral gray, booked = red with "Dolu" and a struck-through time (disabled),
 * past = muted with "Geçti" (disabled), selected = green with a check mark.
 */
export function SlotGrid({ slots, selectedStartUtc, onSelect }: SlotGridProps) {
  return (
    <ul className="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-6">
      {slots.map((slot) => {
        const selected = slot.startUtc === selectedStartUtc
        const available = slot.status === 'Available'
        return (
          <li key={slot.startUtc}>
            <button
              type="button"
              disabled={!available}
              aria-pressed={available ? selected : undefined}
              onClick={() => onSelect(slot)}
              className={cx(
                'flex min-h-14 w-full flex-col items-center justify-center rounded-md border px-2 py-1 text-base font-medium tabular-nums transition-colors',
                available && !selected && 'border-slate-300 bg-slate-100 text-slate-900 hover:border-slate-400 hover:bg-slate-200',
                selected && 'border-green-700 bg-green-700 text-white',
                slot.status === 'Booked' && 'cursor-not-allowed border-red-200 bg-red-50 text-red-800',
                slot.status === 'Past' && 'cursor-not-allowed border-slate-200 bg-white text-slate-400',
              )}
            >
              <span className={cx('flex items-center gap-1', slot.status === 'Booked' && 'line-through')}>
                {selected ? (
                  <Icon icon={Check} size={16} />
                ) : null}
                {formatTime(slot.startUtc)}
              </span>
              {slot.status === 'Booked' ? <span className="text-xs font-semibold">Dolu</span> : null}
              {slot.status === 'Past' ? <span className="text-xs">Geçti</span> : null}
              {selected ? <span className="sr-only">(seçili)</span> : null}
            </button>
          </li>
        )
      })}
    </ul>
  )
}

/** What the slot states look like: the same four looks as the buttons (Boş, Seçili, Dolu, Geçti), each with its text. */
export function SlotLegend() {
  const box = 'inline-flex h-6 min-w-10 items-center justify-center gap-1 rounded border px-1.5 text-xs font-medium tabular-nums'
  return (
    <ul aria-label="Saat durumları" className="flex flex-wrap gap-x-4 gap-y-2 text-sm text-slate-700">
      <li className="flex items-center gap-2">
        <span className={cx(box, 'border-slate-300 bg-slate-100 text-slate-900')}>09:00</span>
        Boş
      </li>
      <li className="flex items-center gap-2">
        <span className={cx(box, 'border-green-700 bg-green-700 text-white')}>
          <Icon icon={Check} size={12} />
          09:30
        </span>
        Seçili
      </li>
      <li className="flex items-center gap-2">
        <span className={cx(box, 'border-red-200 bg-red-50 text-red-800 line-through')}>10:00</span>
        Dolu
      </li>
      <li className="flex items-center gap-2">
        <span className={cx(box, 'border-slate-200 bg-white text-slate-400')}>08:30</span>
        Geçti
      </li>
    </ul>
  )
}

/** Placeholder boxes in the shape of the slot grid while the slots load (instead of a spinner). */
export function SlotSkeleton() {
  return (
    <div role="status" aria-label="Saatler yükleniyor" className="space-y-4">
      {['Sabah', 'Öğleden sonra'].map((name) => (
        <div key={name} className="space-y-2" aria-hidden="true">
          <div className="h-4 w-24 animate-pulse rounded bg-slate-200" />
          <div className="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-6">
            {Array.from({ length: 6 }, (_, index) => (
              <div key={index} className="h-14 animate-pulse rounded-md bg-slate-100" />
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
