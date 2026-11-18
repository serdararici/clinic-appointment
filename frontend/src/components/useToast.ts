import { useContext } from 'react'
import { ToastContext, type ToastApi } from './toast-context'

/** `const toast = useToast(); toast.success('Kaydedildi.')`. Must be used under <ToastProvider>. */
export function useToast(): ToastApi {
  const context = useContext(ToastContext)
  if (!context) throw new Error('useToast must be used inside <ToastProvider>.')
  return context
}
