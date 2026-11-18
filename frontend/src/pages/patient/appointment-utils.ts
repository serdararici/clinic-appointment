import type { AppointmentDetail } from '../../api/types'

/** Pending or confirmed: the appointment still holds its time and can be cancelled. */
export function isActive(appointment: AppointmentDetail): boolean {
  return appointment.status === 'Pending' || appointment.status === 'Confirmed'
}

/** Upcoming = still active and not finished yet. Cancelled and rejected ones belong to the history. */
export function isUpcoming(appointment: AppointmentDetail, now: number): boolean {
  return isActive(appointment) && new Date(appointment.scheduledEndUtc).getTime() > now
}

const byStart = (a: AppointmentDetail, b: AppointmentDetail) => new Date(a.scheduledStartUtc).getTime() - new Date(b.scheduledStartUtc).getTime()

/** Nearest first. */
export function sortUpcoming(list: readonly AppointmentDetail[], now: number): AppointmentDetail[] {
  return list.filter((appointment) => isUpcoming(appointment, now)).sort(byStart)
}

/** Most recent first. */
export function sortPast(list: readonly AppointmentDetail[], now: number): AppointmentDetail[] {
  return list.filter((appointment) => !isUpcoming(appointment, now)).sort((a, b) => byStart(b, a))
}
