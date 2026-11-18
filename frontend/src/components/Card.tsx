import type { ReactNode } from 'react'
import { cx } from '../utils/cx'

interface CardProps {
  /** Optional heading row. */
  title?: string
  /** Optional controls on the right of the heading (buttons, filters). */
  actions?: ReactNode
  /** Remove the inner padding, e.g. when the content is a table that should reach the edges. */
  flush?: boolean
  className?: string
  children: ReactNode
}

/** A white panel with a crisp slate-200 border: the basic container for detail panels and forms. */
export function Card({ title, actions, flush = false, className, children }: CardProps) {
  return (
    <section className={cx('rounded-lg border border-slate-200 bg-white', className)}>
      {title || actions ? (
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-4 py-3 sm:px-5">
          {title ? <h2 className="text-base font-semibold text-slate-900">{title}</h2> : <span />}
          {actions}
        </header>
      ) : null}
      <div className={cx(!flush && 'p-4 sm:p-5')}>{children}</div>
    </section>
  )
}
