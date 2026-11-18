import type { DoctorSummary } from '../../api/types'
import { Badge } from '../../components/Badge'
import { cx } from '../../utils/cx'
import { avatarToneOf, initialsOf } from '../../utils/avatar'

interface DoctorCardProps {
  doctor: DoctorSummary
  /** True for the doctor chosen earlier (when the patient comes back to this step). */
  selected: boolean
  onSelect: () => void
}

/**
 * One doctor as a large card button: initials avatar, name, department (their specialty) and appointment length.
 * It is a real button, so it works with keyboard and screen readers; the chosen one is marked with a check mark
 * and a heavier border, not by color alone.
 */
export function DoctorCard({ doctor, selected, onSelect }: DoctorCardProps) {
  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={selected}
      className={cx(
        'flex min-h-20 w-full items-center gap-4 rounded-lg border bg-white px-4 py-3 text-left transition-colors',
        selected ? 'border-2 border-teal-700 bg-teal-50' : 'border-slate-200 hover:border-teal-700 hover:bg-teal-50',
      )}
    >
      <span className={cx('flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-base font-semibold', avatarToneOf(doctor.fullName))} aria-hidden="true">
        {initialsOf(doctor.fullName)}
      </span>
      <span className="min-w-0 flex-1 space-y-1">
        <span className="block text-base font-semibold text-slate-900">{doctor.fullName}</span>
        <span className="flex flex-wrap items-center gap-x-3 gap-y-1">
          <Badge tone="teal">{doctor.departmentName}</Badge>
          <span className="text-sm tabular-nums text-slate-600">{doctor.slotDurationMinutes} dk randevu</span>
        </span>
      </span>
      {selected ? (
        <svg viewBox="0 0 20 20" className="h-6 w-6 shrink-0 text-teal-700" fill="none" stroke="currentColor" strokeWidth="2.4" aria-hidden="true">
          <path d="M4 10.5 8 14.5 16 6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ) : null}
      {selected ? <span className="sr-only">(seçili)</span> : null}
    </button>
  )
}
