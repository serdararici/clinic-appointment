import type { ButtonHTMLAttributes } from 'react'
import { buttonClasses, type ButtonSize, type ButtonVariant } from './button-styles'
import { Spinner } from './Spinner'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  /** "md" is the dense size for staff screens; "lg" has a touch target of at least 44px for the patient flow. */
  size?: ButtonSize
  /** Shows a spinner and blocks clicks while a request is running. */
  loading?: boolean
}

export function Button({ variant = 'primary', size = 'md', loading = false, disabled, className, children, type = 'button', ...rest }: ButtonProps) {
  return (
    <button type={type} disabled={disabled || loading} className={buttonClasses(variant, size, className)} {...rest}>
      {loading && <Spinner label="" className="text-current" />}
      {children}
    </button>
  )
}
