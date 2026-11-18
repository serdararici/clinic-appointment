// Calendar maths on plain calendar dates ("YYYY-MM-DD"). Dates are built and formatted in UTC on purpose: a calendar
// day has no time zone, so this never shifts a day. Appointment times are turned into Turkey days elsewhere (isoDateOf).

export interface Month {
  year: number
  /** 0 = January. */
  month: number
}

export interface CalendarDay {
  iso: string
  day: number
  inMonth: boolean
}

const utc = (iso: string) => {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day))
}

const toIso = (date: Date) => date.toISOString().slice(0, 10)

export const monthOf = (iso: string): Month => {
  const date = utc(iso)
  return { year: date.getUTCFullYear(), month: date.getUTCMonth() }
}

export const addMonths = ({ year, month }: Month, delta: number): Month => {
  const date = new Date(Date.UTC(year, month + delta, 1))
  return { year: date.getUTCFullYear(), month: date.getUTCMonth() }
}

export const sameMonth = (a: Month, b: Month) => a.year === b.year && a.month === b.month

export const addDaysTo = (iso: string, days: number) => {
  const date = utc(iso)
  date.setUTCDate(date.getUTCDate() + days)
  return toIso(date)
}

/** The weeks of a month, Monday first, filled with the neighbouring months' days (whole weeks only). */
export function monthWeeks({ year, month }: Month): CalendarDay[][] {
  const first = new Date(Date.UTC(year, month, 1))
  const offset = (first.getUTCDay() + 6) % 7 // Monday = 0
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate()
  const weekCount = Math.ceil((offset + daysInMonth) / 7)

  const weeks: CalendarDay[][] = []
  for (let week = 0; week < weekCount; week++) {
    const days: CalendarDay[] = []
    for (let weekday = 0; weekday < 7; weekday++) {
      const date = new Date(Date.UTC(year, month, 1 - offset + week * 7 + weekday))
      days.push({ iso: toIso(date), day: date.getUTCDate(), inMonth: date.getUTCMonth() === month })
    }
    weeks.push(days)
  }
  return weeks
}

const monthTitleFormat = new Intl.DateTimeFormat('tr-TR', { timeZone: 'UTC', month: 'long', year: 'numeric' })
const dayMonthFormat = new Intl.DateTimeFormat('tr-TR', { timeZone: 'UTC', day: 'numeric', month: 'long' })
const weekdayShort = new Intl.DateTimeFormat('tr-TR', { timeZone: 'UTC', weekday: 'short' })
const weekdayLong = new Intl.DateTimeFormat('tr-TR', { timeZone: 'UTC', weekday: 'long' })

/** "Kasım 2026". */
export const monthTitle = ({ year, month }: Month) => monthTitleFormat.format(new Date(Date.UTC(year, month, 1)))

/** "19 Kasım". */
export const dayAndMonth = (iso: string) => dayMonthFormat.format(utc(iso))

/** Monday to Sunday: short ("Pzt") and long ("Pazartesi") names. 2024-01-01 was a Monday. */
export const WEEKDAYS = Array.from({ length: 7 }, (_, index) => {
  const date = new Date(Date.UTC(2024, 0, 1 + index))
  return { short: weekdayShort.format(date), long: weekdayLong.format(date) }
})
