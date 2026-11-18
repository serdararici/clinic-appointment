import { Ban, CircleCheck, CircleX, Clock, type LucideIcon } from 'lucide-react'
import type { AppointmentStatus } from '../api/types'
import { STATUS_PRESENTATION } from './appointment-status'
import { Badge } from './Badge'
import { Icon } from './Icon'

const STATUS_ICONS: Record<AppointmentStatus, LucideIcon> = {
  Pending: Clock,
  Confirmed: CircleCheck,
  Rejected: CircleX,
  Cancelled: Ban,
}

/** The badge of an appointment status: icon plus text (pending amber, confirmed green, rejected red, cancelled slate). */
export function StatusBadge({ status }: { status: AppointmentStatus }) {
  const { label, tone } = STATUS_PRESENTATION[status]
  return (
    <Badge tone={tone}>
      <Icon icon={STATUS_ICONS[status]} size={14} />
      {label}
    </Badge>
  )
}
