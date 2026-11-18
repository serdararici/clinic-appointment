import type { ButtonHTMLAttributes } from 'react'
import type { LucideIcon } from 'lucide-react'
import { buttonClasses, type ButtonSize, type ButtonVariant } from './button-styles'
import { Icon } from './Icon'
import { Spinner } from './Spinner'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  /** "md" is the dense size for staff screens; "lg" has a touch target of at least 44px for the patient flow. */
  size?: ButtonSize
  /** Shows a spinner and blocks clicks while a request is running. */
  loading?: boolean
  /** Optional leading icon (lucide-react); the spinner takes its place while loading. */
  icon?: LucideIcon
}

export function Button({ variant = 'primary', size = 'md', loading = false, icon, disabled, className, children, type = 'button', ...rest }: ButtonProps) {
  return (
    <button type={type} disabled={disabled || loading} className={buttonClasses(variant, size, className)} {...rest}>
      {loading ? <Spinner label="" className="text-current" /> : icon ? <Icon icon={icon} /> : null}
      {children}
    </button>
  )
}
