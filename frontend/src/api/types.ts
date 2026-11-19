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
  /** The doctor's specialty is the name of their department. */
  departmentName: string
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
  organizationName: string
  branchName: string
  departmentName: string
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

/** What the logged-in patient would pay for a service (GET /api/services/{id}/price-preview). */
export interface PricePreview {
  serviceId: string
  basePrice: number
  /** The amount after the patient's insurance discount, if any. */
  price: number
  hasInsurance: boolean
}

export interface CreateAppointmentRequest {
  serviceId: string
  /** UTC ISO date-time ("...Z"). */
  startUtc: string
  /** Required for Examination services. */
  doctorId?: string
}

/** The logged-in doctor's own name and department (GET /api/doctors/{id}/profile). */
export interface DoctorProfile {
  id: string
  fullName: string
  departmentId: string
  departmentName: string
  slotDurationMinutes: number
  /** True: new appointments are confirmed at once; false: they wait for the doctor's approval. */
  autoConfirmAppointments: boolean
}

// ---- Availability ----

/** The doctor's automatic work schedule switch (GET/PUT /api/doctors/{id}/auto-schedule). */
export interface AutoSchedule {
  doctorId: string
  enabled: boolean
}

/** One availability range of a doctor (UTC). */
export interface Availability {
  id: string
  doctorId: string
  startUtc: string
  endUtc: string
}

export interface AddAvailabilityRequest {
  startUtc: string
  endUtc: string
}

export type WeekdayName = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'

/** Dates and times are Istanbul local time ("YYYY-MM-DD" and "HH:mm"). */
export interface BulkAvailabilityRequest {
  fromDate: string
  toDate: string
  weekdays: WeekdayName[]
  startTime: string
  endTime: string
}

export interface BulkAvailabilityResult {
  created: Availability[]
  /** Dates skipped because the range would overlap an existing one. */
  skippedDates: string[]
  /** Dates skipped because the range had already started. */
  pastDates: string[]
}

// ---- Management (clinic manager) ----

/** Create or update a service (POST /api/departments/{id}/services, PUT /api/services/{id}). */
export interface SaveServiceRequest {
  name: string
  serviceType: ServiceType
  durationMinutes: number
  basePrice: number
}

/** A staff member as the manager sees them (GET /api/staff). */
export interface StaffMember {
  id: string
  /** "Doctor" or "HealthTechnician". */
  role: string
  fullName: string
  email: string
  phoneNumber: string
  branchId: string
  departmentId: string
  slotDurationMinutes: number
  isActive: boolean
  title: string | null
  serviceIds: string[]
}

export interface CreateDoctorRequest {
  fullName: string
  email: string
  phoneNumber?: string
  /** The temporary password the doctor first logs in with (at least 8 characters). */
  password: string
  slotDurationMinutes?: number
  /** The examination services the doctor offers (at least one); all of the department's when left out. */
  serviceIds?: string[]
}

export interface CreateTechnicianRequest {
  fullName: string
  email: string
  phoneNumber?: string
  /** The diagnostic services the technician is linked to (may be empty); all of the department's when left out. */
  serviceIds?: string[]
}

export interface UpdateStaffRequest {
  fullName: string
  email: string
  phoneNumber?: string
}

// ---- Administration (system administrator) ----

export interface Organization {
  id: string
  name: string
}

export interface Branch {
  id: string
  organizationId: string
  name: string
  address: string
}

/** An opening range of a branch on one weekday, in Turkey time ("HH:mm:ss"). */
export interface WorkingHour {
  id: string
  branchId: string
  dayOfWeek: WeekdayName
  opensAt: string
  closesAt: string
}

export interface AddWorkingHourRequest {
  dayOfWeek: WeekdayName
  /** "HH:mm". */
  opensAt: string
  closesAt: string
}

/** A clinic manager account with the branch and organization they work for (GET /api/managers). */
export interface ClinicManagerAccount {
  id: string
  fullName: string
  email: string
  phoneNumber: string
  branchId: string
  branchName: string
  organizationId: string
  organizationName: string
  isActive: boolean
}

export interface CreateManagerRequest {
  fullName: string
  email: string
  /** The temporary password the manager first logs in with (at least 8 characters). */
  password: string
  phoneNumber?: string
}

export interface UpdateManagerRequest {
  fullName: string
  email: string
  phoneNumber?: string
}

// ---- My account ----

/** The logged-in user's own account (GET /api/account). The insurance number is set for patients only. */
export interface Account {
  id: string
  fullName: string
  email: string
  phoneNumber: string
  role: Role
  insuranceNumber: string | null
}

/** The email is read-only. The insurance number only applies to patients; blank removes it. */
export interface UpdateAccountRequest {
  fullName: string
  phoneNumber: string
  insuranceNumber?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}
