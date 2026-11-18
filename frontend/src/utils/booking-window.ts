import { todayIsoDate } from './format'

/**
 * How many days ahead a patient can book, counted from today (Turkey calendar days). It mirrors
 * BookingRules.WindowDays in the API, which enforces it; keep the two numbers equal.
 */
export const BOOKING_WINDOW_DAYS = 14

/** Adds days to an ISO date ("YYYY-MM-DD") on the calendar, with no time-zone shifts. */
export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10)
}

/** The first and last date a patient can pick: today and today + BOOKING_WINDOW_DAYS. */
export function bookingWindow(): { min: string; max: string } {
  const min = todayIsoDate()
  return { min, max: addDays(min, BOOKING_WINDOW_DAYS) }
}
