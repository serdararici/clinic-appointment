import { useId, type InputHTMLAttributes } from 'react'
import { cx } from '../utils/cx'
import { Field } from './Field'

interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label: string
  hint?: string
  error?: string
  /** "lg" gives a 44px-high control for the patient flow. Native types (date, tel, email) are used as they are. */
  size?: 'md' | 'lg'
}

export function Input({ label, hint, error, size = 'md', id, className, required, ...rest }: InputProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId

  return (
    <Field id={inputId} label={label} required={required} hint={hint} error={error}>
      <input
        id={inputId}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
        className={cx(
          'block w-full rounded-md border bg-white px-3 text-slate-900 placeholder:text-slate-400',
          'focus:border-teal-700 disabled:bg-slate-100 disabled:text-slate-500',
          error ? 'border-red-600' : 'border-slate-300',
          size === 'lg' ? 'min-h-11 text-base' : 'min-h-11 text-base sm:h-10 sm:min-h-0 sm:text-sm',
          className,
        )}
        {...rest}
      />
    </Field>
  )
}
