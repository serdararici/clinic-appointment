import { Link, type LinkProps } from 'react-router-dom'
import { buttonClasses, type ButtonSize, type ButtonVariant } from './button-styles'

interface LinkButtonProps extends LinkProps {
  variant?: ButtonVariant
  size?: ButtonSize
}

/** A router link that looks like a Button (for navigation that should read as an action). */
export function LinkButton({ variant = 'primary', size = 'md', className, ...rest }: LinkButtonProps) {
  return <Link className={buttonClasses(variant, size, className)} {...rest} />
}
