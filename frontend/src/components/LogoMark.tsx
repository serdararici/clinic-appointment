import { cx } from '../utils/cx'

/**
 * The app's mark: the same teal rounded square with a white cross as public/favicon.svg, inline so it scales with
 * the text. A plain geometric sign, not modeled on any real hospital or public-service logo.
 */
export function LogoMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 64 64" className={cx('h-7 w-7 shrink-0', className)} aria-hidden="true">
      <rect width="64" height="64" rx="14" fill="#0f766e" />
      <path d="M32 16v32M16 32h32" stroke="#ffffff" strokeWidth="7" strokeLinecap="round" />
    </svg>
  )
}
