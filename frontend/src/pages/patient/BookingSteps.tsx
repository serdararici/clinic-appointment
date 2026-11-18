import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { listDepartments, listDoctors, listDoctorSlots, listServices, listServiceSlots } from '../../api/booking'
import type { BranchSummary, Department, DoctorSummary, Service, Slot } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { ChoiceCard } from '../../components/ChoiceCard'
import { EmptyState } from '../../components/EmptyState'
import { Input } from '../../components/Input'
import { Spinner } from '../../components/Spinner'
import { formatCurrency, formatLongDate, todayIsoDate } from '../../utils/format'
import { useAsync, type AsyncState } from '../../utils/useAsync'
import { SERVICE_TYPE_LABELS } from './labels'
import { SlotGrid } from './SlotGrid'

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
  children,
}: {
  state: AsyncState<T[]>
  emptyTitle: string
  emptyDescription?: string
  children: (items: T[]) => ReactNode
}) {
  if (state.loading) return <Loading />
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
      <div className="rounded-lg border border-slate-200 bg-white">
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
          <ChoiceCard key={branch.id} title={branch.name} description={branch.address} onSelect={() => onSelect(branch)} />
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
            <ChoiceCard key={department.id} title={department.name} onSelect={() => onSelect(department)} />
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

export function DoctorStep({ departmentId, serviceId, onSelect }: { departmentId: string; serviceId: string; onSelect: (doctor: DoctorSummary) => void }) {
  // Only doctors who offer the chosen service.
  const state = useAsync(
    useCallback(
      async (signal: AbortSignal) => (await listDoctors(departmentId, signal)).filter((doctor) => doctor.services.some((service) => service.id === serviceId)),
      [departmentId, serviceId],
    ),
  )
  return (
    <ListState state={state} emptyTitle="Bu hizmeti veren doktor bulunmuyor" emptyDescription="Başka bir hizmet veya bölüm seçmeyi deneyin.">
      {(doctors) => (
        <div className="space-y-3">
          {doctors.map((doctor) => (
            <ChoiceCard
              key={doctor.id}
              title={doctor.fullName}
              description={doctor.departmentName}
              meta={`${doctor.slotDurationMinutes} dk randevu`}
              onSelect={() => onSelect(doctor)}
            />
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

export function SlotStep({ service, doctor, selected, notice, onSelect, onContinue }: SlotStepProps) {
  const today = todayIsoDate()
  const [date, setDate] = useState(today)

  const serviceId = service.id
  const doctorId = doctor?.id ?? null
  const state = useAsync(
    useCallback(
      (signal: AbortSignal) => (doctorId ? listDoctorSlots(doctorId, date, signal) : listServiceSlots(serviceId, date, signal)),
      [doctorId, serviceId, date],
    ),
  )

  const hasAvailable = state.data?.some((slot) => slot.status === 'Available') ?? false

  return (
    <div className="space-y-5">
      {notice ? <Alert tone="error">{notice}</Alert> : null}

      <div className="max-w-xs">
        <Input
          label="Tarih"
          type="date"
          size="lg"
          min={today}
          value={date}
          onChange={(event) => {
            // An empty value (the field was cleared) or a date before today is not queried.
            if (event.target.value >= today) setDate(event.target.value)
          }}
        />
      </div>

      <div className="space-y-3">
        <h2 className="text-base font-semibold text-slate-900">{formatLongDate(`${date}T12:00:00+03:00`)}</h2>
        <ListState state={state} emptyTitle="Bu tarihte uygun saat yok" emptyDescription="Başka bir tarih seçmeyi deneyin.">
          {(slots) => (
            <div className="space-y-3">
              <SlotGrid slots={slots} selectedStartUtc={selected?.startUtc ?? null} onSelect={onSelect} />
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
