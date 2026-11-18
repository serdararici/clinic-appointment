import { cx } from '../utils/cx'

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'
export type ButtonSize = 'md' | 'lg'

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-teal-700 text-white hover:bg-teal-800',
  secondary: 'border border-slate-300 bg-white text-slate-900 hover:bg-slate-50',
  danger: 'bg-red-700 text-white hover:bg-red-800',
  ghost: 'text-slate-700 hover:bg-slate-100',
}

const sizes: Record<ButtonSize, string> = {
  md: 'min-h-11 px-4 text-sm sm:h-10 sm:min-h-0',
  lg: 'min-h-11 px-6 text-base', // min-h-11 = 44px: the touch target of the patient flow
}

/** The classes shared by <Button> and <LinkButton>, so a link can look exactly like a button. */
export function buttonClasses(variant: ButtonVariant, size: ButtonSize, extra?: string): string {
  return cx(
    'inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors',
    'disabled:cursor-not-allowed disabled:opacity-50',
    variants[variant],
    sizes[size],
    extra,
  )
}
