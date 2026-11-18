import type { AppointmentStatus } from '../api/types'
import type { BadgeTone } from './Badge'

/** Turkish label and badge color of each appointment status; the same everywhere in the app. */
export const STATUS_PRESENTATION: Record<AppointmentStatus, { label: string; tone: BadgeTone }> = {
  Pending: { label: 'Onay bekliyor', tone: 'amber' },
  Confirmed: { label: 'Onaylandı', tone: 'green' },
  Rejected: { label: 'Reddedildi', tone: 'red' },
  Cancelled: { label: 'İptal edildi', tone: 'slate' },
}
