import { createContext } from 'react'

export type ToastVariant = 'success' | 'error'

export interface ToastApi {
  /** Shows a short confirmation that an action went through ("Randevunuz iptal edildi."). */
  success: (message: string) => void
  /** Shows a short error that is not tied to a form (a form keeps its own inline Alert). */
  error: (message: string) => void
}

// Kept apart from the provider component so each file exports either a component or plain values
// (the react-refresh lint rule requires that).
export const ToastContext = createContext<ToastApi | null>(null)
