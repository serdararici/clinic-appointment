import type { Role } from '../api/types'

/** Turkish names of the roles, for display. */
export const ROLE_LABELS: Record<Role, string> = {
  Patient: 'Hasta',
  Doctor: 'Doktor',
  HealthTechnician: 'Sağlık teknikeri',
  ClinicManager: 'Klinik yöneticisi',
  SystemAdmin: 'Sistem yöneticisi',
}

/** Each role's home page; users are sent here after login and when they open a page their role may not use. */
export const ROLE_HOME: Record<Role, string> = {
  Patient: '/hasta',
  Doctor: '/doktor',
  HealthTechnician: '/personel',
  ClinicManager: '/yonetici',
  SystemAdmin: '/sistem',
}

export function homeFor(role: Role): string {
  return ROLE_HOME[role]
}
