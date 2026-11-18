// Shapes of the API's auth endpoints (see CLAUDE.md "Authentication and Authorization").

/** The role names exactly as the API writes them into the token's "role" claim. */
export type Role = 'Patient' | 'Doctor' | 'HealthTechnician' | 'ClinicManager' | 'SystemAdmin'

export const ROLES: readonly Role[] = ['Patient', 'Doctor', 'HealthTechnician', 'ClinicManager', 'SystemAdmin']

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAtUtc: string
  userId: string
  fullName: string
  email: string
  role: Role
  /** Set for staff and clinic managers only. */
  branchId: string | null
  organizationId: string | null
}

/** Self-registration always creates a Patient. */
export interface RegisterRequest {
  fullName: string
  email: string
  password: string
  phoneNumber: string
  /** ISO date, "YYYY-MM-DD". */
  dateOfBirth: string
  insuranceNumber?: string
}

export interface RegisteredUser {
  id: string
  fullName: string
  email: string
  role: Role
}

// ---- Booking and appointments -------------------------------------------------------------------------

export type ServiceType = 'Examination' | 'Diagnostic'

/** A branch with the name of its organization (GET /api/branches). */
export interface BranchSummary {
  id: string
  organizationId: string
  organizationName: string
  name: string
  address: string
}

export interface Department {
  id: string
  branchId: string
  name: string
}

export interface Service {
  id: string
  departmentId: string
  name: string
  serviceType: ServiceType
  durationMinutes: number
  basePrice: number
}

/** A doctor of a department with the services they offer (GET /api/departments/{id}/doctors). */
export interface DoctorSummary {
  id: string
  fullName: string
  specialty: string
  slotDurationMinutes: number
  services: Service[]
}

export type SlotStatus = 'Available' | 'Booked' | 'Past'

export interface Slot {
  startUtc: string
  endUtc: string
  status: SlotStatus
}

export type AppointmentStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Cancelled'

/** One row of the patient's list (GET /api/patients/{id}/appointments). */
export interface AppointmentDetail {
  id: string
  patientId: string
  patientName: string
  staffMemberId: string | null
  staffMemberName: string | null
  branchName: string
  serviceId: string
  serviceName: string
  serviceType: ServiceType
  scheduledStartUtc: string
  scheduledEndUtc: string
  price: number
  status: AppointmentStatus
}

/** The result of booking (POST /api/appointments). The status is already normalized to its name. */
export interface BookedAppointment {
  id: string
  staffMemberId: string | null
  serviceId: string
  scheduledStartUtc: string
  scheduledEndUtc: string
  price: number
  status: AppointmentStatus
}

export interface CreateAppointmentRequest {
  serviceId: string
  /** UTC ISO date-time ("...Z"). */
  startUtc: string
  /** Required for Examination services. */
  doctorId?: string
}
