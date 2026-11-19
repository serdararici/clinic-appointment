import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Icon } from '../../components/Icon'
import type { AppointmentDetail } from '../../api/types'
import { StatusBadge } from '../../components/StatusBadge'
import { cx } from '../../utils/cx'
import { formatDuration, formatLongDate, formatPlace, formatTime, isoDateOf, minutesBetween } from '../../utils/format'
import { isActive } from './appointment-utils'
import { addDaysTo, addMonths, dayAndMonth, monthOf, monthTitle, monthWeeks, sameMonth, WEEKDAYS, type Month } from './calendar-utils'
import { ServiceLabel } from './ServiceLabel'

interface AppointmentCalendarProps {
  appointments: readonly AppointmentDetail[]
  /** Today in Turkey, "YYYY-MM-DD". */
  today: string
  /** The day selected first (the day of the next appointment, or today). */
  initialDay: string
}

function Chevron({ direction }: { direction: 'left' | 'right' }) {
  return <Icon icon={direction === 'left' ? ChevronLeft : ChevronRight} size={20} />
}

/**
 * The patient's month calendar. Each day is a real button; arrow keys move by a day or a week, Home/End go to the
 * start or end of the week and Page Up/Down change the month (one focusable day at a time, like a native picker).
 * A dot marks days with appointments: teal when one is still pending or confirmed, muted when all are cancelled or
 * rejected. The selected day's appointments are listed below.
 */
export function AppointmentCalendar({ appointments, today, initialDay }: AppointmentCalendarProps) {
  const [selected, setSelected] = useState(initialDay)
  const [view, setView] = useState<Month>(() => monthOf(initialDay))
  const dayRefs = useRef(new Map<string, HTMLButtonElement>())
  const [focusDay, setFocusDay] = useState<string | null>(null)

  // Appointments grouped by their Turkey calendar day, each day in time order.
  const byDay = useMemo(() => {
    const map = new Map<string, AppointmentDetail[]>()
    for (const appointment of appointments) {
      const day = isoDateOf(appointment.scheduledStartUtc)
      map.set(day, [...(map.get(day) ?? []), appointment])
    }
    for (const list of map.values()) list.sort((a, b) => a.scheduledStartUtc.localeCompare(b.scheduledStartUtc))
    return map
  }, [appointments])

  // After a keyboard move, put the focus on the new day (it may be in a month that was just rendered).
  useEffect(() => {
    if (!focusDay) return
    dayRefs.current.get(focusDay)?.focus()
    setFocusDay(null)
  }, [focusDay, view])

  const select = (iso: string, moveFocus = false) => {
    setSelected(iso)
    const month = monthOf(iso)
    if (!sameMonth(month, view)) setView(month)
    if (moveFocus) setFocusDay(iso)
  }

  const onDayKey = (event: KeyboardEvent<HTMLButtonElement>, iso: string) => {
    const weekday = (new Date(`${iso}T00:00:00Z`).getUTCDay() + 6) % 7
    const moves: Record<string, () => string> = {
      ArrowLeft: () => addDaysTo(iso, -1),
      ArrowRight: () => addDaysTo(iso, 1),
      ArrowUp: () => addDaysTo(iso, -7),
      ArrowDown: () => addDaysTo(iso, 7),
      Home: () => addDaysTo(iso, -weekday),
      End: () => addDaysTo(iso, 6 - weekday),
      PageUp: () => addDaysTo(iso, -28),
      PageDown: () => addDaysTo(iso, 28),
    }
    const move = moves[event.key]
    if (!move) return
    event.preventDefault()
    select(move(), true)
  }

  // Only one day is in the Tab order: the selected one if it is shown, otherwise the first day of the month.
  const weeks = monthWeeks(view)
  const tabbable = sameMonth(monthOf(selected), view) ? selected : weeks.flat().find((day) => day.inMonth)!.iso
  const dayList = byDay.get(selected) ?? []

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-lg font-semibold text-slate-900" aria-live="polite">
          {monthTitle(view)}
        </h3>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setView(addMonths(view, -1))}
            aria-label="Önceki ay"
            className="flex h-11 w-11 items-center justify-center rounded-md text-slate-700 hover:bg-slate-100 sm:h-9 sm:w-9"
          >
            <Chevron direction="left" />
          </button>
          <button
            type="button"
            onClick={() => select(today)}
            className="min-h-11 rounded-md border border-slate-300 px-3 text-sm font-medium text-slate-900 hover:bg-slate-50 sm:min-h-9"
          >
            Bugün
          </button>
          <button
            type="button"
            onClick={() => setView(addMonths(view, 1))}
            aria-label="Sonraki ay"
            className="flex h-11 w-11 items-center justify-center rounded-md text-slate-700 hover:bg-slate-100 sm:h-9 sm:w-9"
          >
            <Chevron direction="right" />
          </button>
        </div>
      </div>

      <table className="w-full table-fixed border-collapse" aria-label={`Takvim, ${monthTitle(view)}`}>
        <thead>
          <tr>
            {WEEKDAYS.map((weekday) => (
              <th key={weekday.long} scope="col" className="pb-2 text-center text-xs font-medium text-slate-500">
                <abbr title={weekday.long} className="no-underline">
                  {weekday.short}
                </abbr>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {weeks.map((week) => (
            <tr key={week[0].iso}>
              {week.map((day) => {
                const list = byDay.get(day.iso) ?? []
                const hasActive = list.some(isActive)
                const isToday = day.iso === today
                const isSelected = day.iso === selected
                const count = list.length
                const label = `${dayAndMonth(day.iso)}, ${count === 0 ? 'randevu yok' : `${count} randevu`}${isToday ? ', bugün' : ''}`
                return (
                  <td key={day.iso} className="p-0.5 text-center">
                    <button
                      type="button"
                      ref={(element) => {
                        if (element) dayRefs.current.set(day.iso, element)
                        else dayRefs.current.delete(day.iso)
                      }}
                      tabIndex={day.iso === tabbable ? 0 : -1}
                      aria-label={label}
                      aria-pressed={isSelected}
                      aria-current={isToday ? 'date' : undefined}
                      onClick={() => select(day.iso)}
                      onKeyDown={(event) => onDayKey(event, day.iso)}
                      className={cx(
                        'relative mx-auto flex h-11 w-full max-w-12 flex-col items-center justify-center rounded-lg text-sm tabular-nums transition-colors',
                        isSelected
                          ? 'bg-teal-700 font-semibold text-white'
                          : isToday
                            ? 'font-semibold text-teal-800 ring-1 ring-inset ring-teal-700 hover:bg-teal-50'
                            : day.inMonth
                              ? 'text-slate-900 hover:bg-slate-100'
                              : 'text-slate-400 hover:bg-slate-50',
                      )}
                    >
                      {day.day}
                      {count > 0 ? (
                        <span
                          aria-hidden="true"
                          className={cx(
                            'absolute bottom-1.5 h-1.5 w-1.5 rounded-full',
                            isSelected ? 'bg-white' : hasActive ? 'bg-teal-600' : 'bg-slate-300',
                          )}
                        />
                      ) : null}
                    </button>
                  </td>
                )
              })}
            </tr>
          ))}
        </tbody>
      </table>

      <div className="flex flex-wrap gap-x-5 gap-y-1 text-xs text-slate-600" aria-hidden="true">
        <span className="flex items-center gap-1.5">
          <span className="h-1.5 w-1.5 rounded-full bg-teal-600" /> Randevu var
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-1.5 w-1.5 rounded-full bg-slate-300" /> Yalnızca iptal veya reddedilmiş
        </span>
      </div>

      <section aria-labelledby="day-heading" className="border-t border-slate-200 pt-4">
        <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
          <h3 id="day-heading" className="text-base font-semibold text-slate-900">
            {formatLongDate(`${selected}T12:00:00+03:00`)}
          </h3>
          {dayList.length > 0 ? (
            <Link to="/hasta/randevularim" className="inline-flex min-h-11 items-center text-sm font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800 sm:min-h-0">
              Randevularım’da gör
            </Link>
          ) : null}
        </div>
        {dayList.length === 0 ? (
          <p className="text-sm text-slate-600">Bu gün için randevunuz yok.</p>
        ) : (
          <ul className="space-y-3">
            {dayList.map((appointment) => (
              <li key={appointment.id} className="flex gap-4 rounded-lg border border-slate-200 p-3">
                <span className="w-16 shrink-0 pt-0.5 tabular-nums">
                  <span className="block text-base font-semibold text-slate-900">{formatTime(appointment.scheduledStartUtc)}</span>
                  <span className="block text-xs text-slate-500">{formatDuration(minutesBetween(appointment.scheduledStartUtc, appointment.scheduledEndUtc))}</span>
                </span>
                <div className="min-w-0 flex-1 space-y-1">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <p className="text-sm font-medium text-slate-900">
                      <ServiceLabel serviceName={appointment.serviceName} departmentName={appointment.departmentName} />
                    </p>
                    <StatusBadge status={appointment.status} />
                  </div>
                  <p className="text-sm text-slate-700">{appointment.staffMemberName ?? 'Doktor atanmadı'}</p>
                  <p className="text-sm text-slate-600">{formatPlace(appointment)}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
