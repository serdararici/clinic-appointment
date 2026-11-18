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

// The first path segment decides which role owns a page: /hasta, /doktor, /personel, /yonetici, /sistem.
// "/hesabim" (my account) belongs to everyone. This mirrors the route guards in App.tsx.
const firstSegment = (path: string) => path.split(/[?#]/)[0].split('/')[1] ?? ''

/** Whether a role may open a page (by its path). */
export function canOpen(role: Role, path: string): boolean {
  const segment = firstSegment(path)
  return segment === 'hesabim' || segment === firstSegment(ROLE_HOME[role])
}

/**
 * Where to send a user who has just logged in. `from` is the page they were on when the session ended or the
 * deep link they opened; it is only used if it is a normal in-app path and this role may open it. Anything else
 * (nothing, another role's page, an odd value) goes to the role's own home page.
 */
export function returnPathFor(role: Role, from: string | undefined): string {
  if (from && from.startsWith('/') && !from.startsWith('//') && canOpen(role, from)) return from
  return homeFor(role)
}
