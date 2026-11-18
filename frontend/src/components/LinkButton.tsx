import type { LucideIcon } from 'lucide-react'
import { Link, type LinkProps } from 'react-router-dom'
import { buttonClasses, type ButtonSize, type ButtonVariant } from './button-styles'
import { Icon } from './Icon'

interface LinkButtonProps extends LinkProps {
  variant?: ButtonVariant
  size?: ButtonSize
  /** Optional leading icon (lucide-react). */
  icon?: LucideIcon
}

/** A router link that looks like a Button (for navigation that should read as an action). */
export function LinkButton({ variant = 'primary', size = 'md', icon, className, children, ...rest }: LinkButtonProps) {
  return (
    <Link className={buttonClasses(variant, size, className)} {...rest}>
      {icon ? <Icon icon={icon} /> : null}
      {children}
    </Link>
  )
}
