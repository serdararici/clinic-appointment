import type { LucideIcon } from 'lucide-react'
import { cx } from '../utils/cx'

interface IconProps {
  /** A lucide-react icon, for example `CalendarDays`. */
  icon: LucideIcon
  /** Pixels; 16 to 20 next to labels (default 18). */
  size?: number
  className?: string
}

/**
 * The one way icons are drawn in the app, so they all look alike: lucide-react, 18px by default, stroke 1.75,
 * aria-hidden (an icon is always next to text that says the same thing, never the only signal).
 */
export function Icon({ icon: Glyph, size = 18, className }: IconProps) {
  return <Glyph size={size} strokeWidth={1.75} aria-hidden="true" focusable="false" className={cx('shrink-0', className)} />
}
