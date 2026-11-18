import type { BookedAppointment, BranchSummary, Department, DoctorSummary, Service, Slot } from '../../api/types'

/** The screens of the booking flow, in the order they can appear. */
export type Step = 'organization' | 'branch' | 'department' | 'service' | 'doctor' | 'slot' | 'summary' | 'done'

/** What the patient has chosen so far. */
export interface Selection {
  organizationId: string | null
  branch: BranchSummary | null
  department: Department | null
  service: Service | null
  doctor: DoctorSummary | null
  slot: Slot | null
}

export const EMPTY_SELECTION: Selection = {
  organizationId: null,
  branch: null,
  department: null,
  service: null,
  doctor: null,
  slot: null,
}

/** The choice each step makes, in flow order: going back to a step clears everything chosen after it. */
export const CHOICE_ORDER: ReadonlyArray<[Step, keyof Selection]> = [
  ['organization', 'organizationId'],
  ['branch', 'branch'],
  ['department', 'department'],
  ['service', 'service'],
  ['doctor', 'doctor'],
  ['slot', 'slot'],
]

export interface BookingResult {
  appointment: BookedAppointment
  selection: Selection
}
