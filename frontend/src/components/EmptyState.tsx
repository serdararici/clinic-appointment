import type { ReactNode } from 'react'

interface EmptyStateProps {
  title: string
  description?: string
  /** Optional next step, e.g. a button that creates the first record. */
  action?: ReactNode
}

/** Shown when a list has nothing in it: plain words and an optional next step, no illustration. */
export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className="px-4 py-10 text-center">
      <p className="text-base font-semibold text-slate-900">{title}</p>
      {description ? <p className="mx-auto mt-1 max-w-md text-sm text-slate-600">{description}</p> : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  )
}
