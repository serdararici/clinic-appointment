import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { listDepartments, listDoctors, listDoctorSlots, listServices, listServiceSlots } from '../../api/booking'
import type { BranchSummary, Department, DoctorSummary, Service, Slot } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { ChoiceCard } from '../../components/ChoiceCard'
import { DoctorCard } from './DoctorCard'
import { EmptyState } from '../../components/EmptyState'
import { Input } from '../../components/Input'
import { Spinner } from '../../components/Spinner'
import { BOOKING_WINDOW_DAYS, addDays, bookingWindow } from '../../utils/booking-window'
import { formatCurrency, formatLongDate, formatTime } from '../../utils/format'
import { useAsync, type AsyncState } from '../../utils/useAsync'
import { SERVICE_TYPE_LABELS } from './labels'
import { SlotGrid, SlotLegend, SlotSkeleton } from './SlotGrid'
import { Building2, Hospital, MapPin, Microscope, Stethoscope } from 'lucide-react'

// One component per step of the booking flow. Each loads its own list (with loading, error and empty states)
// and reports the patient's choice upward; BookingPage decides what comes next.

function Loading() {
  return (
    <div className="flex justify-center py-10">
      <Spinner label="Yükleniyor..." />
    </div>
  )
}

/** Loading, error and empty handling shared by the list steps. */
function ListState<T>({
  state,
  emptyTitle,
  emptyDescription,
  loading,
  children,
}: {
  state: AsyncState<T[]>
  emptyTitle: string
  emptyDescription?: string
  /** Shown while loading instead of the spinner (the slot grid uses a skeleton). */
  loading?: ReactNode
  children: (items: T[]) => ReactNode
}) {
  if (state.loading) return loading ?? <Loading />
  if (state.error) {
    return (
      <div className="space-y-3">
        <Alert tone="error">{state.error}</Alert>
        <Button variant="secondary" size="lg" onClick={state.reload}>
          Yeniden dene
        </Button>
      </div>
    )
  }
  if (!state.data || state.data.length === 0) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
        <EmptyState title={emptyTitle} description={emptyDescription} />
      </div>
    )
  }
  return <>{children(state.data)}</>
}

export function OrganizationStep({ branches, onSelect }: { branches: AsyncState<BranchSummary[]>; onSelect: (organizationId: string) => void }) {
  // One entry per organization, in the order the API lists them.
  const organizations = new Map<string, { name: string; branchCount: number }>()
  for (const branch of branches.data ?? []) {
    const entry = organizations.get(branch.organizationId)
    organizations.set(branch.organizationId, { name: branch.organizationName, branchCount: (entry?.branchCount ?? 0) + 1 })
  }

  return (
    <ListState
      state={{ ...branches, data: [...organizations.entries()] }}
      emptyTitle="Henüz kurum bulunmuyor"
      emptyDescription="Randevu alınabilecek bir kurum tanımlanmamış."
    >
      {(list) => (
        <div className="space-y-3">
          {list.map(([id, organization]) => (
            <ChoiceCard
              key={id}
              icon={Building2}
              title={organization.name}
              description={organization.branchCount === 1 ? '1 şube' : `${organization.branchCount} şube`}
              onSelect={() => onSelect(id)}
            />
          ))}
        </div>
      )}
    </ListState>
  )
}

export function BranchStep({ branches, organizationId, onSelect }: { branches: BranchSummary[]; organizationId: string; onSelect: (branch: BranchSummary) => void }) {
  return (
    <div className="space-y-3">
      {branches
        .filter((branch) => branch.organizationId === organizationId)
        .map((branch) => (
          <ChoiceCard key={branch.id} icon={MapPin} title={branch.name} description={branch.address} onSelect={() => onSelect(branch)} />
        ))}
    </div>
  )
}

export function DepartmentStep({ branchId, onSelect }: { branchId: string; onSelect: (department: Department) => void }) {
  const state = useAsync(useCallback((signal: AbortSignal) => listDepartments(branchId, signal), [branchId]))
  return (
    <ListState state={state} emptyTitle="Bu şubede bölüm bulunmuyor" emptyDescription="Başka bir şube seçmeyi deneyin.">
      {(departments) => (
        <div className="space-y-3">
          {departments.map((department) => (
            <ChoiceCard key={department.id} icon={Hospital} title={department.name} onSelect={() => onSelect(department)} />
          ))}
        </div>
      )}
    </ListState>
  )
}

interface ServiceStepProps {
  departmentId: string
  onSelect: (service: Service) => void
  /** Called instead of onSelect when the department has a single service: this step is then skipped. */
  onAutoSelect: (service: Service) => void
}

export function ServiceStep({ departmentId, onSelect, onAutoSelect }: ServiceStepProps) {
  const state = useAsync(useCallback((signal: AbortSignal) => listServices(departmentId, signal), [departmentId]))
  const only = state.data?.length === 1 ? state.data[0] : null

  // A department with exactly one service needs no choice.
  useEffect(() => {
    if (only) onAutoSelect(only)
  }, [only, onAutoSelect])

  if (only) return <Loading />

  return (
    <ListState state={state} emptyTitle="Bu bölümde hizmet bulunmuyor" emptyDescription="Başka bir bölüm seçmeyi deneyin.">
      {(services) => (
        <div className="space-y-3">
          {services.map((service) => (
            <ChoiceCard
              key={service.id}
              icon={service.serviceType === 'Examination' ? Stethoscope : Microscope}
              title={service.name}
              description={`${SERVICE_TYPE_LABELS[service.serviceType]} · ${service.durationMinutes} dk`}
              meta={formatCurrency(service.basePrice)}
              onSelect={() => onSelect(service)}
            />
          ))}
        </div>
      )}
    </ListState>
  )
}

export function DoctorStep({ departmentId, serviceId, selectedId, onSelect }: { departmentId: string; serviceId: string; selectedId: string | null; onSelect: (doctor: DoctorSummary) => void }) {
  // Only doctors who offer the chosen service.
  const state = useAsync(
    useCallback(
      // The API returns only the doctors who offer the chosen service.
      (signal: AbortSignal) => listDoctors(departmentId, serviceId, signal),
      [departmentId, serviceId],
    ),
  )
  return (
    <ListState state={state} emptyTitle="Bu hizmeti veren doktor bulunmuyor" emptyDescription="Başka bir hizmet veya bölüm seçmeyi deneyin.">
      {(doctors) => (
        <div className="grid gap-3 sm:grid-cols-2">
          {doctors.map((doctor) => (
            <DoctorCard key={doctor.id} doctor={doctor} selected={doctor.id === selectedId} onSelect={() => onSelect(doctor)} />
          ))}
        </div>
      )}
    </ListState>
  )
}

interface SlotStepProps {
  service: Service
  /** Null for Diagnostic services, which are booked without choosing a doctor. */
  doctor: DoctorSummary | null
  selected: Slot | null
  /** An error left over from a failed booking attempt (the slots are loaded fresh because of it). */
  notice: string | null
  onSelect: (slot: Slot) => void
  onContinue: () => void
}

/** A slot's hour in Turkey time (the grid is grouped by it). */
const hourOf = (slot: Slot) => Number(formatTime(slot.startUtc).slice(0, 2))

export function SlotStep({ service, doctor, selected, notice, onSelect, onContinue }: SlotStepProps) {
  // Patients can book from today up to BOOKING_WINDOW_DAYS days ahead (the API enforces the same window).
  const { min, max } = bookingWindow()
  const tomorrow = addDays(min, 1)
  const [date, setDate] = useState(min)

  const serviceId = service.id
  const doctorId = doctor?.id ?? null
  const loadSlots = useCallback(
    (day: string) => (signal: AbortSignal) => (doctorId ? listDoctorSlots(doctorId, day, signal) : listServiceSlots(serviceId, day, signal)),
    [doctorId, serviceId],
  )
  const loadDay = useCallback((signal: AbortSignal) => loadSlots(date)(signal), [loadSlots, date])
  const loadToday = useCallback((signal: AbortSignal) => loadSlots(min)(signal), [loadSlots, min])
  const state = useAsync(loadDay)
  // Today's slots are checked on their own: "Bugün" is disabled once nothing is left today.
  const today = useAsync(loadToday)
  const todayFull = today.data !== undefined && !today.data.some((slot) => slot.status === 'Available')

  const hasAvailable = state.data?.some((slot) => slot.status === 'Available') ?? false

  const chip = (label: string, day: string, disabled: boolean) => (
    <Button variant={date === day ? 'primary' : 'secondary'} size="lg" aria-pressed={date === day} disabled={disabled} onClick={() => setDate(day)}>
      {label}
    </Button>
  )

  return (
    <div className="space-y-5">
      {notice ? <Alert tone="error">{notice}</Alert> : null}

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-full sm:w-auto sm:min-w-56">
          <Input
            label="Tarih"
            type="date"
            size="lg"
            min={min}
            max={max}
            hint={`En fazla ${BOOKING_WINDOW_DAYS} gün sonrasına randevu alabilirsiniz.`}
            value={date}
            onChange={(event) => {
              // An empty value (the field was cleared) or a date outside the window is not queried.
              if (event.target.value >= min && event.target.value <= max) setDate(event.target.value)
            }}
          />
        </div>
        <div className="flex gap-2 sm:pb-6">
          {chip('Bugün', min, todayFull)}
          {tomorrow <= max ? chip('Yarın', tomorrow, false) : null}
        </div>
      </div>

      <div className="space-y-3">
        <h2 className="text-base font-semibold text-slate-900">{formatLongDate(`${date}T12:00:00+03:00`)}</h2>
        <ListState state={state} loading={<SlotSkeleton />} emptyTitle="Bu tarihte uygun saat yok" emptyDescription="Başka bir tarih seçmeyi deneyin.">
          {(slots) => (
            <div className="space-y-4">
              {[
                { title: 'Sabah', items: slots.filter((slot) => hourOf(slot) < 12) },
                { title: 'Öğleden sonra', items: slots.filter((slot) => hourOf(slot) >= 12) },
              ]
                .filter((group) => group.items.length > 0)
                .map((group) => (
                  <section key={group.title} aria-label={group.title} className="space-y-2">
                    <h3 className="text-sm font-semibold text-slate-700">{group.title}</h3>
                    <SlotGrid slots={group.items} selectedStartUtc={selected?.startUtc ?? null} onSelect={onSelect} />
                  </section>
                ))}
              <SlotLegend />
              {!hasAvailable ? <Alert tone="info">Bu tarihte boş saat kalmadı. Başka bir tarih seçmeyi deneyin.</Alert> : null}
            </div>
          )}
        </ListState>
      </div>

      <Button size="lg" disabled={!selected} onClick={onContinue}>
        Devam et
      </Button>
    </div>
  )
}
