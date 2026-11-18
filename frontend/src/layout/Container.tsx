import type { ReactNode } from 'react'
import { cx } from '../utils/cx'

/**
 * The page width of the app with the same side padding everywhere: max-w-6xl in the patient layout, max-w-7xl
 * (`wide`) in the staff shell, whose tables need more room. Header, page area and footer of a shell use the same one,
 * so the brand, the page titles and the footer text start on the same line.
 */
export function Container({ className, wide = false, children }: { className?: string; wide?: boolean; children: ReactNode }) {
  return <div className={cx('mx-auto w-full px-4 sm:px-6', wide ? 'max-w-7xl' : 'max-w-6xl', className)}>{children}</div>
}
