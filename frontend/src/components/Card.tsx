import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { cx } from '../utils/cx'
import { Icon } from './Icon'

interface CardProps {
  /** Optional heading row. */
  title?: string
  /** A small icon before the title (lucide-react). */
  icon?: LucideIcon
  /** Optional controls on the right of the heading (buttons, filters). */
  actions?: ReactNode
  /** Remove the inner padding, e.g. when the content is a table that should reach the edges. */
  flush?: boolean
  className?: string
  children: ReactNode
}

/**
 * The basic panel of every page: white, rounded-xl, a slate-200 border and a light shadow. The heading row sits on a
 * very light slate tint so a page of cards reads as clear sections; the padding is the same as before, so the dense
 * staff pages keep their density.
 */
export function Card({ title, icon, actions, flush = false, className, children }: CardProps) {
  return (
    <section className={cx('rounded-xl border border-slate-200 bg-white shadow-sm', className)}>
      {title || actions ? (
        <header className="flex flex-wrap items-center justify-between gap-3 rounded-t-xl border-b border-slate-200 bg-slate-50/70 px-4 py-3 sm:px-5">
          {title ? (
            <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
              {icon ? <Icon icon={icon} className="text-teal-700" /> : null}
              {title}
            </h2>
          ) : (
            <span />
          )}
          {actions}
        </header>
      ) : null}
      <div className={cx(!flush && 'p-4 sm:p-5')}>{children}</div>
    </section>
  )
}
