import type { ReactNode } from 'react'

interface FieldProps {
  /** The id of the control inside, so the label is attached to it. */
  id: string
  label: string
  required?: boolean
  hint?: string
  error?: string
  children: ReactNode
}

/** Label, control, hint and error message: the shared frame of Input and Select. */
export function Field({ id, label, required, hint, error, children }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-medium text-slate-900">
        {label}
        {required ? (
          <span aria-hidden="true" className="text-red-700">
            {' *'}
          </span>
        ) : null}
      </label>
      {children}
      {hint && !error ? (
        <p id={`${id}-hint`} className="text-sm text-slate-600">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={`${id}-error`} className="text-sm font-medium text-red-700">
          {error}
        </p>
      ) : null}
    </div>
  )
}
