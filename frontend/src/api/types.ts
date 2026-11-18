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
