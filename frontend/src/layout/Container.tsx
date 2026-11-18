import type { ReactNode } from 'react'
import { cx } from '../utils/cx'

/**
 * The one page width of the app (max-w-6xl with the same side padding). The navbar, the page area and the footer all
 * use it, so the brand, the page titles and the footer text start on the same line.
 */
export function Container({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={cx('mx-auto w-full max-w-6xl px-4 sm:px-6', className)}>{children}</div>
}
