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

/**
 * A short summary of a branch's week: consecutive days with the same hours are joined, e.g.
 * "Pzt–Cum 08:00–17:00 · Cmt 09:00–13:00". A branch without any working hours is closed all week.
 */
export function summarizeHours(hours: readonly WorkingHour[]): string {
  const groups: { first: string; last: string; ranges: string }[] = []

  for (const day of DAY_ORDER) {
    const ranges = rangesOf(hours, day.name)
    if (!ranges) continue
    const previous = groups[groups.length - 1]
    const previousDayIndex = previous ? DAY_ORDER.findIndex((candidate) => candidate.short === previous.last) : -1
    if (previous && previous.ranges === ranges && previousDayIndex === DAY_ORDER.indexOf(day) - 1) previous.last = day.short
    else groups.push({ first: day.short, last: day.short, ranges })
  }

  if (groups.length === 0) return 'Çalışma saati tanımlı değil'
  return groups.map((group) => `${group.first === group.last ? group.first : `${group.first}–${group.last}`} ${group.ranges}`).join(' · ')
}
