import type { AppointmentStatus } from '../api/types'
import type { BadgeTone } from './Badge'

/** Turkish label and badge color of each appointment status; the same everywhere in the app. */
export const STATUS_PRESENTATION: Record<AppointmentStatus, { label: string; tone: BadgeTone }> = {
  Pending: { label: 'Onay bekliyor', tone: 'amber' },
  Confirmed: { label: 'Onaylandı', tone: 'green' },
  Rejected: { label: 'Reddedildi', tone: 'red' },
  Cancelled: { label: 'İptal edildi', tone: 'slate' },
}

/** The statuses as select options ("Onay bekliyor", "Onaylandı", ...), for the appointment filters. */
export const STATUS_OPTIONS: ReadonlyArray<{ value: AppointmentStatus; label: string }> = (Object.keys(STATUS_PRESENTATION) as AppointmentStatus[]).map((value) => ({
  value,
  label: STATUS_PRESENTATION[value].label,
}))
