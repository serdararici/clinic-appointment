import { useCallback, useState } from 'react'
import { getWorkingHours } from '../../api/admin'
import { getDoctorProfile, listAvailability } from '../../api/doctor'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { BOOKING_WINDOW_DAYS } from '../../utils/booking-window'
import { useAsync } from '../../utils/useAsync'
import { AutoScheduleCard } from './AutoScheduleCard'
import { AvailabilityList } from './AvailabilityList'
import { BulkAvailabilityCard } from './BulkAvailabilityCard'
import { SingleAvailabilityCard } from './SingleAvailabilityCard'
import { SlotDurationCard } from './SlotDurationCard'
import { usePageTitle } from '../../utils/usePageTitle'
import { hourGroups } from '../shared/working-hours-utils'

/** The doctor's schedule: appointment length, adding availability (single or bulk) and the upcoming ranges. */
export function AvailabilityPage() {
  usePageTitle('Uygunluk')
  const { user } = useAuth()
  const doctorId = user?.id ?? ''
  const profile = useAsync(useCallback((signal: AbortSignal) => getDoctorProfile(doctorId, signal), [doctorId]))
  const availability = useAsync(useCallback((signal: AbortSignal) => listAvailability(doctorId, signal), [doctorId]))
  const [now] = useState(() => Date.now())
  // The branch's working hours are public (GET /api/branches/{id}/working-hours); the doctor's token carries the branch.
  const branchId = user?.branchId ?? ''
  const branchHours = useAsync(useCallback((signal: AbortSignal) => getWorkingHours(branchId, signal), [branchId]))
  const hourRows = branchHours.data ? hourGroups(branchHours.data, true) : null
  // After a save the new duration is used right away, without reloading the profile (which would blank the cards).
  const [savedDuration, setSavedDuration] = useState<number | null>(null)

  const slotDuration = savedDuration ?? profile.data?.slotDurationMinutes

  // Only upcoming ranges: a range that has not ended yet.
  const upcoming = availability.data
    ?.filter((range) => new Date(range.endUtc).getTime() > now)
    .sort((a, b) => new Date(a.startUtc).getTime() - new Date(b.startUtc).getTime())

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Uygunluk</h1>
      {hourRows ? (
        <p className="text-sm text-slate-500">
          Şube çalışma saatleri:{' '}
          <span className="tabular-nums">{hourRows.map((group) => `${group.days} ${group.ranges}`).join(' · ')}</span>
        </p>
      ) : null}
      <Alert tone="info">Hastalar yalnızca önümüzdeki {BOOKING_WINDOW_DAYS} gün için randevu alabilir ve uygun saatleri görebilir.</Alert>

      {slotDuration === undefined ? (
        profile.error ? (
          <div className="space-y-3">
            <Alert tone="error">{profile.error}</Alert>
            <Button variant="secondary" onClick={profile.reload}>
              Yeniden dene
            </Button>
          </div>
        ) : (
          <div className="flex justify-center py-10">
            <Spinner label="Yükleniyor..." />
          </div>
        )
      ) : (
        <>
          <SlotDurationCard doctorId={doctorId} current={slotDuration} onSaved={setSavedDuration} />
          <AutoScheduleCard doctorId={doctorId} onChanged={availability.reload} />
          <div className="grid gap-5 lg:grid-cols-2">
            <SingleAvailabilityCard doctorId={doctorId} branchHours={branchHours.data} onAdded={availability.reload} />
            <BulkAvailabilityCard doctorId={doctorId} branchHours={branchHours.data} slotDurationMinutes={slotDuration} onAdded={availability.reload} />
          </div>
          <section aria-labelledby="existing-heading" className="space-y-3">
            <h2 id="existing-heading" className="text-base font-semibold text-slate-900">
              Mevcut uygunluklar
            </h2>
            <AvailabilityList
              doctorId={doctorId}
              rows={upcoming}
              loading={availability.loading}
              error={availability.error}
              slotDurationMinutes={slotDuration}
              onReload={availability.reload}
            />
          </section>
        </>
      )}
    </div>
  )
}
