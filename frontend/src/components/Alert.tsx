import type { ReactNode } from 'react'
import { CircleAlert, CircleCheck, Info, type LucideIcon } from 'lucide-react'
import { cx } from '../utils/cx'
import { Icon } from './Icon'

type AlertTone = 'error' | 'success' | 'info'

const tones: Record<AlertTone, { box: string; icon: LucideIcon; iconColor: string }> = {
  error: { box: 'border-red-200 bg-red-50 text-red-900', icon: CircleAlert, iconColor: 'text-red-700' },
  success: { box: 'border-green-200 bg-green-50 text-green-900', icon: CircleCheck, iconColor: 'text-green-700' },
  info: { box: 'border-slate-200 bg-slate-50 text-slate-800', icon: Info, iconColor: 'text-slate-500' },
}

/** A short message box for form errors, notices and confirmations; the icon repeats what the text already says. */
export function Alert({ tone = 'info', children }: { tone?: AlertTone; children: ReactNode }) {
  const style = tones[tone]
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={cx('flex gap-3 rounded-lg border px-4 py-3 text-sm', style.box)}>
      <Icon icon={style.icon} className={cx('mt-px', style.iconColor)} />
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  )
}
