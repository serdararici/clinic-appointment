import { useId, type SelectHTMLAttributes } from 'react'
import { cx } from '../utils/cx'
import { Field } from './Field'

export interface SelectOption {
  value: string
  label: string
}

interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  label: string
  options: readonly SelectOption[]
  /** Text of the empty first choice ("Seçiniz"); leave out to start on a real option. */
  placeholder?: string
  hint?: string
  error?: string
  size?: 'md' | 'lg'
}

/** The browser's native select element, styled to match Input. */
export function Select({ label, options, placeholder, hint, error, size = 'md', id, className, required, ...rest }: SelectProps) {
  const generatedId = useId()
  const selectId = id ?? generatedId

  return (
    <Field id={selectId} label={label} required={required} hint={hint} error={error}>
      <select
        id={selectId}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${selectId}-error` : hint ? `${selectId}-hint` : undefined}
        className={cx(
          'block w-full rounded-md border bg-white px-3 text-slate-900',
          'focus:border-teal-700 disabled:bg-slate-100 disabled:text-slate-500',
          error ? 'border-red-600' : 'border-slate-300',
          size === 'lg' ? 'min-h-11 text-base' : 'min-h-11 text-base sm:h-10 sm:min-h-0 sm:text-sm',
          className,
        )}
        {...rest}
      >
        {placeholder !== undefined ? <option value="">{placeholder}</option> : null}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  )
}
