import type { ReactNode } from 'react'
import { Inbox, type LucideIcon } from 'lucide-react'
import { Icon } from './Icon'

interface EmptyStateProps {
  title: string
  description?: string
  /** Optional next step, e.g. a button that creates the first record. */
  action?: ReactNode
  /** A small muted icon above the title (default: an empty tray). Kept small on purpose, no tile behind it. */
  icon?: LucideIcon
}

/** Shown when a list has nothing in it: plain words and an optional next step, no illustration. */
export function EmptyState({ title, description, action, icon = Inbox }: EmptyStateProps) {
  return (
    <div className="px-4 py-10 text-center">
      <Icon icon={icon} size={20} className="mx-auto mb-3 text-slate-400" />
      <p className="text-base font-semibold text-slate-900">{title}</p>
      {description ? <p className="mx-auto mt-1 max-w-md text-sm text-slate-600">{description}</p> : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  )
}
