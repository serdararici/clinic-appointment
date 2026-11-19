import type { WeekdayName, WorkingHour } from '../../api/types'

/** Monday first, with the short names used in the working-hours summary. */
export const DAY_ORDER: ReadonlyArray<{ name: WeekdayName; label: string; short: string }> = [
  { name: 'Monday', label: 'Pazartesi', short: 'Pzt' },
  { name: 'Tuesday', label: 'Salı', short: 'Sal' },
  { name: 'Wednesday', label: 'Çarşamba', short: 'Çar' },
  { name: 'Thursday', label: 'Perşembe', short: 'Per' },
  { name: 'Friday', label: 'Cuma', short: 'Cum' },
  { name: 'Saturday', label: 'Cumartesi', short: 'Cmt' },
  { name: 'Sunday', label: 'Pazar', short: 'Paz' },
]

/** "08:00:00" from the API to "08:00". */
export const shortTime = (time: string) => time.slice(0, 5)

/** The ranges of one weekday as text, e.g. "08:00–12:00, 13:00–17:00" (empty for a closed day). */
function rangesOf(hours: readonly WorkingHour[], day: WeekdayName): string {
  return hours
    .filter((hour) => hour.dayOfWeek === day)
    .sort((a, b) => a.opensAt.localeCompare(b.opensAt))
    .map((hour) => `${shortTime(hour.opensAt)}–${shortTime(hour.closesAt)}`)
    .join(', ')
}

export interface HourGroup {
  /** "Pzt–Cum", "Cmt" ... */
  days: string
  /** "08:00–17:00" or "08:00–12:00, 13:00–17:00"; "Kapalı" for the days without hours. */
  ranges: string
}

/**
 * The week as groups of consecutive days with the same hours, Monday first. With `includeClosed` the days without any
 * hours are grouped too ("Cmt–Paz Kapalı"); without it they are left out.
 */
export function hourGroups(hours: readonly WorkingHour[], includeClosed = false): HourGroup[] {
  const groups: { first: number; last: number; ranges: string }[] = []

  DAY_ORDER.forEach((day, index) => {
    const ranges = rangesOf(hours, day.name) || (includeClosed ? 'Kapalı' : '')
    if (!ranges) return
    const previous = groups[groups.length - 1]
    if (previous && previous.ranges === ranges && previous.last === index - 1) previous.last = index
    else groups.push({ first: index, last: index, ranges })
  })

  return groups.map((group) => ({
    days: group.first === group.last ? DAY_ORDER[group.first].short : `${DAY_ORDER[group.first].short}–${DAY_ORDER[group.last].short}`,
    ranges: group.ranges,
  }))
}

/**
 * A short summary of a branch's week: consecutive days with the same hours are joined, e.g.
 * "Pzt–Cum 08:00–17:00 · Cmt 09:00–13:00". A branch without any working hours is closed all week.
 */
export function summarizeHours(hours: readonly WorkingHour[]): string {
  const groups = hourGroups(hours)
  if (groups.length === 0) return 'Çalışma saati tanımlı değil'
  return groups.map((group) => `${group.days} ${group.ranges}`).join(' · ')
}

const minutes = (time: string) => {
  const [hours, mins] = time.split(':').map(Number)
  return hours * 60 + mins
}

/** The weekday name of a calendar date ("YYYY-MM-DD"). */
export function weekdayNameOf(isoDate: string): WeekdayName {
  const [year, month, day] = isoDate.split('-').map(Number)
  const index = (new Date(Date.UTC(year, month - 1, day)).getUTCDay() + 6) % 7 // Monday = 0
  return DAY_ORDER[index].name
}

/**
 * Whether a time range ("HH:mm" to "HH:mm") on a weekday reaches outside the branch's working hours: it is not inside one
 * of that day's opening ranges (a closed day has none). Only used for a hint: the range is saved either way, and the
 * API simply does not offer the part outside the hours.
 */
export function leavesBranchHours(hours: readonly WorkingHour[], day: WeekdayName, start: string, end: string): boolean {
  const from = minutes(start)
  const to = minutes(end)
  return !hours.some((hour) => hour.dayOfWeek === day && minutes(shortTime(hour.opensAt)) <= from && to <= minutes(shortTime(hour.closesAt)))
}
