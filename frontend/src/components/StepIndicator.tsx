import { cx } from '../utils/cx'

interface StepIndicatorProps {
  labels: readonly string[]
  /** Zero-based index of the current step. */
  current: number
}

/**
 * Shows where the patient is in the flow: numbered steps, done ones with a check mark.
 * On a phone only the current label is spelled out ("Adım 2 / 5"), the others stay as numbers.
 */
export function StepIndicator({ labels, current }: StepIndicatorProps) {
  return (
    <nav aria-label="Randevu adımları">
      <p className="mb-2 text-sm font-medium text-slate-700 sm:hidden">
        Adım {current + 1} / {labels.length}: {labels[current]}
      </p>
      <ol className="flex items-center gap-2">
        {labels.map((label, index) => {
          const done = index < current
          const active = index === current
          return (
            <li key={label} className="flex items-center gap-2" aria-current={active ? 'step' : undefined}>
              <span
                className={cx(
                  'flex h-8 w-8 items-center justify-center rounded-full border text-sm font-semibold tabular-nums',
                  active && 'border-teal-700 bg-teal-700 text-white',
                  done && 'border-teal-700 bg-white text-teal-700',
                  !active && !done && 'border-slate-300 bg-white text-slate-600',
                )}
              >
                {done ? (
                  <svg viewBox="0 0 20 20" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
                    <path d="M4 10.5 8 14.5 16 6" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
                ) : (
                  index + 1
                )}
                <span className="sr-only">{label}{done ? ' (tamamlandı)' : ''}</span>
              </span>
              <span className={cx('hidden text-sm sm:inline', active ? 'font-semibold text-slate-900' : 'text-slate-600')}>{label}</span>
              {index < labels.length - 1 ? <span className="hidden h-px w-4 bg-slate-300 sm:block" aria-hidden="true" /> : null}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
