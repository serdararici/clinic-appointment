import type { ReactNode } from 'react'
import { cx } from '../utils/cx'

/**
 * The fixed status colors of the app. Use the same tone for the same meaning everywhere:
 *   amber  = pending ("Beklemede")
 *   green  = confirmed ("Onaylandı")
 *   red    = rejected ("Reddedildi") or a failure
 *   slate  = cancelled ("İptal edildi") or neutral information
 *   teal   = informational highlight (e.g. a role)
 * The text always says what the status is, so color is never the only signal.
 */
export type BadgeTone = 'amber' | 'green' | 'red' | 'slate' | 'teal'

const tones: Record<BadgeTone, string> = {
  amber: 'bg-amber-50 text-amber-800 ring-amber-200',
  green: 'bg-green-50 text-green-800 ring-green-200',
  red: 'bg-red-50 text-red-800 ring-red-200',
  slate: 'bg-slate-100 text-slate-700 ring-slate-200',
  teal: 'bg-teal-50 text-teal-800 ring-teal-200',
}

export function Badge({ tone = 'slate', children }: { tone?: BadgeTone; children: ReactNode }) {
  return (
    <span className={cx('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset', tones[tone])}>
      {children}
    </span>
  )
}
