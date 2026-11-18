import { LoaderCircle } from 'lucide-react'
import { cx } from '../utils/cx'
import { Icon } from './Icon'

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
      <Icon icon={LoaderCircle} size={18} className="animate-spin" />
      {label ? <span className="text-slate-600">{label}</span> : null}
    </span>
  )
}
