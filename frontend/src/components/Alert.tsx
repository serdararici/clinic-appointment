import type { ReactNode } from 'react'
import { cx } from '../utils/cx'

type AlertTone = 'error' | 'success' | 'info'

const tones: Record<AlertTone, string> = {
  error: 'border-red-200 bg-red-50 text-red-900',
  success: 'border-green-200 bg-green-50 text-green-900',
  info: 'border-slate-200 bg-slate-100 text-slate-800',
}

/** A short message box for form errors, notices and confirmations. */
export function Alert({ tone = 'info', children }: { tone?: AlertTone; children: ReactNode }) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={cx('rounded-md border px-4 py-3 text-sm', tones[tone])}>
      {children}
    </div>
  )
}
