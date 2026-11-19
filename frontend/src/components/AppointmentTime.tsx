import { formatDateTime, formatDuration, formatTime, minutesBetween } from '../utils/format'

interface AppointmentTimeProps {
  startUtc: string
  endUtc: string
  /** "dateTime": "19.11.2026 10:00 · 30 dk" (lists); "time": "10:00 · 30 dk" (the day is shown elsewhere). */
  show?: 'dateTime' | 'time'
}

/**
 * The time of an appointment with its length, compact: "10:00 · 30 dk" with the length muted. The length is the stored
 * end minus the stored start, exactly what the booking decided (the doctor's slot or the service's duration).
 */
export function AppointmentTime({ startUtc, endUtc, show = 'dateTime' }: AppointmentTimeProps) {
  return (
    <span className="tabular-nums">
      {show === 'dateTime' ? formatDateTime(startUtc) : formatTime(startUtc)}
      <span className="text-slate-500"> · {formatDuration(minutesBetween(startUtc, endUtc))}</span>
    </span>
  )
}
