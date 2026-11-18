import type { AppointmentDetail } from '../../api/types'
import { isoDateOf, todayIsoDate } from '../../utils/format'

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

/** Active appointments that start today (Turkey date), earliest first. */
export function sortToday(list: readonly AppointmentDetail[]): AppointmentDetail[] {
  const today = todayIsoDate()
  return list.filter((appointment) => isActive(appointment) && isoDateOf(appointment.scheduledStartUtc) === today).sort(byStart)
}

/** Pending appointments that have not finished yet (waiting for the doctor's decision), nearest first. */
export function sortPending(list: readonly AppointmentDetail[], now: number): AppointmentDetail[] {
  return list.filter((appointment) => appointment.status === 'Pending' && isUpcoming(appointment, now)).sort(byStart)
}

/** Confirmed appointments that have not finished yet, nearest first (the doctor's "Yaklaşan" tab). */
export function sortConfirmedUpcoming(list: readonly AppointmentDetail[], now: number): AppointmentDetail[] {
  return list.filter((appointment) => appointment.status === 'Confirmed' && isUpcoming(appointment, now)).sort(byStart)
}
