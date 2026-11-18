import type { WeekdayName } from '../../api/types'
import { istanbulToUtcIso } from '../../utils/format'

/** The slot durations the API accepts. */
export const SLOT_DURATIONS = [10, 15, 20, 30, 60] as const

/** Monday first, as the week is read in Turkey. `index` is the JavaScript weekday (Sunday = 0). */
export const WEEKDAYS: ReadonlyArray<{ name: WeekdayName; label: string; index: number }> = [
  { name: 'Monday', label: 'Pazartesi', index: 1 },
  { name: 'Tuesday', label: 'Salı', index: 2 },
  { name: 'Wednesday', label: 'Çarşamba', index: 3 },
  { name: 'Thursday', label: 'Perşembe', index: 4 },
  { name: 'Friday', label: 'Cuma', index: 5 },
  { name: 'Saturday', label: 'Cumartesi', index: 6 },
  { name: 'Sunday', label: 'Pazar', index: 0 },
]

export const DEFAULT_WEEKDAYS: readonly WeekdayName[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']

/** Minutes since midnight of an "HH:mm" time. */
export function minutesOf(time: string): number {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}

/**
 * How many appointment slots a range of this length holds. The API cuts a range into whole slots of the doctor's
 * duration and drops a shorter piece at the end. (Slots outside the branch's working hours are dropped too, so
 * this is the most the range can offer.)
 */
export function slotCount(rangeMinutes: number, slotDurationMinutes: number): number {
  return Math.max(0, Math.floor(rangeMinutes / slotDurationMinutes))
}

export interface BulkPreview {
  days: number
  slots: number
}

/** A daily time window, "HH:mm" to "HH:mm" (Turkey time). */
export interface TimeWindow {
  start: string
  end: string
}

/**
 * What a bulk request would create, worked out in the browser for the preview line: the matching weekdays in the
 * date range, and for each of them the windows that have not started yet (the API skips started windows), times in
 * Turkey local time. Days that overlap an existing range are only known to the API, so the preview is an upper bound.
 */
export function previewBulk(
  fromDate: string,
  toDate: string,
  weekdays: readonly WeekdayName[],
  windows: readonly TimeWindow[],
  slotDurationMinutes: number,
  now: number,
): BulkPreview {
  const valid = windows.filter((window) => window.start && window.end && minutesOf(window.end) > minutesOf(window.start))
  if (!fromDate || !toDate || valid.length === 0 || valid.length < windows.length || weekdays.length === 0) return { days: 0, slots: 0 }

  const selected = new Set(WEEKDAYS.filter((day) => weekdays.includes(day.name)).map((day) => day.index))
  const [fy, fm, fd] = fromDate.split('-').map(Number)
  const [ty, tm, td] = toDate.split('-').map(Number)
  const last = Date.UTC(ty, tm - 1, td)

  let days = 0
  let slots = 0
  // Walk the calendar dates (UTC arithmetic on the date parts only, so no time-zone shifts).
  for (let cursor = Date.UTC(fy, fm - 1, fd), guard = 0; cursor <= last && guard < 400; cursor += 86_400_000, guard++) {
    const date = new Date(cursor)
    if (!selected.has(date.getUTCDay())) continue
    const iso = date.toISOString().slice(0, 10)

    let dayHasRange = false
    for (const window of valid) {
      if (new Date(istanbulToUtcIso(iso, window.start)).getTime() < now) continue
      dayHasRange = true
      slots += slotCount(minutesOf(window.end) - minutesOf(window.start), slotDurationMinutes)
    }
    if (dayHasRange) days++
  }

  return { days, slots }
}
