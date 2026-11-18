import { useCallback } from 'react'
import { listPatientAppointments } from '../../api/booking'
import { useAuth } from '../../auth/useAuth'
import { useAsync } from '../../utils/useAsync'

/** The logged-in patient's appointments (every status), one call per page; shared by the home page and "Randevularım". */
export function usePatientAppointments() {
  const { user } = useAuth()
  const patientId = user?.id ?? ''
  return useAsync(useCallback((signal: AbortSignal) => listPatientAppointments(patientId, signal), [patientId]))
}
