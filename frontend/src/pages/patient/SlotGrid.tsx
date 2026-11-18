import type { Slot } from '../../api/types'
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
                  <svg viewBox="0 0 20 20" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
                    <path d="M4 10.5 8 14.5 16 6" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
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
