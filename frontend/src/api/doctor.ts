import { api } from './client'
import type {
  AddAvailabilityRequest,
  AppointmentDetail,
  Availability,
  BulkAvailabilityRequest,
  BulkAvailabilityResult,
  DoctorProfile,
} from './types'

// The doctor's own calls (Doctor role; the API refuses any other doctor's id). Cancelling an appointment
// uses cancelAppointment from ./booking: it is the same endpoint the patient uses.

export const getDoctorProfile = (doctorId: string, signal?: AbortSignal) =>
  api.get<DoctorProfile>(`/api/doctors/${doctorId}/profile`, undefined, signal)

export const listDoctorAppointments = (doctorId: string, signal?: AbortSignal) =>
  api.get<AppointmentDetail[]>(`/api/doctors/${doctorId}/appointments`, undefined, signal)

export const listAvailability = (doctorId: string, signal?: AbortSignal) =>
  api.get<Availability[]>(`/api/doctors/${doctorId}/availability`, undefined, signal)

export const addAvailability = (doctorId: string, request: AddAvailabilityRequest) =>
  api.post<Availability>(`/api/doctors/${doctorId}/availability`, request)

export const addBulkAvailability = (doctorId: string, request: BulkAvailabilityRequest) =>
  api.post<BulkAvailabilityResult>(`/api/doctors/${doctorId}/availability/bulk`, request)

export const deleteAvailability = (doctorId: string, availabilityId: string) =>
  api.delete(`/api/doctors/${doctorId}/availability/${availabilityId}`)

/** The doctor changes their own slot duration (10, 15, 20, 30 or 60 minutes). */
export const setSlotDuration = (doctorId: string, slotDurationMinutes: number) =>
  api.put<{ staffId: string; slotDurationMinutes: number }>(`/api/staff/${doctorId}/slot-duration`, { slotDurationMinutes })
