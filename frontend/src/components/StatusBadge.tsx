import type { AppointmentStatus } from '../api/types'
import { STATUS_PRESENTATION } from './appointment-status'
import { Badge } from './Badge'

/** The badge of an appointment status (pending amber, confirmed green, rejected red, cancelled slate). */
export function StatusBadge({ status }: { status: AppointmentStatus }) {
  const { label, tone } = STATUS_PRESENTATION[status]
  return <Badge tone={tone}>{label}</Badge>
}
