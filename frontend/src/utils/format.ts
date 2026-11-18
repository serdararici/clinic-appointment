// Display formats. The API speaks UTC; people see Turkey time (Europe/Istanbul) in tr-TR style:
// "18.11.2026", 24-hour "09:30", and prices like "₺1.234,50".

const TIME_ZONE = 'Europe/Istanbul'
const LOCALE = 'tr-TR'

const dateFormat = new Intl.DateTimeFormat(LOCALE, { timeZone: TIME_ZONE, day: '2-digit', month: '2-digit', year: 'numeric' })
const longDateFormat = new Intl.DateTimeFormat(LOCALE, { timeZone: TIME_ZONE, day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' })
const timeFormat = new Intl.DateTimeFormat(LOCALE, { timeZone: TIME_ZONE, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })
const currencyFormat = new Intl.NumberFormat(LOCALE, { style: 'currency', currency: 'TRY' })

/** "18.11.2026" from an ISO date-time such as "2026-11-18T06:00:00Z". */
export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso))
}

/** "18 Kasım 2026 Çarşamba". */
export function formatLongDate(iso: string): string {
  return longDateFormat.format(new Date(iso))
}

/** "09:00" (24-hour). */
export function formatTime(iso: string): string {
  return timeFormat.format(new Date(iso))
}

/** "18.11.2026 09:00". */
export function formatDateTime(iso: string): string {
  return `${formatDate(iso)} ${formatTime(iso)}`
}

/** "₺450,00". */
export function formatCurrency(amount: number): string {
  return currencyFormat.format(amount)
}

const isoDateFormat = new Intl.DateTimeFormat('en-CA', { timeZone: TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' })

/** The Turkey calendar date of an ISO date-time as "YYYY-MM-DD" (to compare with todayIsoDate()). */
export function isoDateOf(iso: string): string {
  return isoDateFormat.format(new Date(iso))
}

/** Today's date in Turkey as "YYYY-MM-DD", the form of a native date input and of the slot endpoints. */
export function todayIsoDate(): string {
  return isoDateFormat.format(new Date())
}

/** "Sakarya Şehir Hastanesi · Merkez": where an appointment is (organization and branch). */
export function formatPlace(place: { organizationName: string; branchName: string }): string {
  return `${place.organizationName} · ${place.branchName}`
}

/**
 * Turkey wall-clock date ("YYYY-MM-DD") and time ("HH:mm") to a UTC ISO date-time for the API.
 * Turkey has been on a fixed UTC+3 since 2016 (no daylight saving), so the offset is constant.
 */
export function istanbulToUtcIso(date: string, time: string): string {
  return new Date(`${date}T${time}:00+03:00`).toISOString()
}
