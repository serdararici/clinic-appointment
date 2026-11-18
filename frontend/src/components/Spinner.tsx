import { cx } from '../utils/cx'

interface SpinnerProps {
  /** Text shown next to the spinner and read by screen readers. Pass '' for an icon-only spinner. */
  label?: string
  /** The icon takes the text color of this element (teal by default; "text-current" inside a button). */
  className?: string
}

/** Loading indicator. Functional feedback, not decoration: it only appears while something is loading. */
export function Spinner({ label = 'Yükleniyor...', className }: SpinnerProps) {
  return (
    <span role="status" aria-label={label || 'Yükleniyor'} className={cx('inline-flex items-center gap-2 text-sm text-teal-700', className)}>
      <svg className="h-5 w-5 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="3" className="opacity-25" />
        <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
      </svg>
      {label ? <span className="text-slate-600">{label}</span> : null}
    </span>
  )
}
