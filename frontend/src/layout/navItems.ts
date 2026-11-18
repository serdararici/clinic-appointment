import { CalendarCheck, CalendarPlus, CalendarRange, ClipboardList, Hospital, House, Layers, UserCog, Users, type LucideIcon } from 'lucide-react'
import type { Role } from '../api/types'
import { homeFor } from '../auth/roles'

export interface NavItem {
  to: string
  label: string
  /** Shown in the staff sidebar; the patient's top navbar shows text only. */
  icon: LucideIcon
}

/**
 * The page links of each role, in one list for both shells: the patient navbar and the staff sidebar.
 * "Hesabım" is not here: it lives in the account menu (and in the phone menus).
 */
export function navItemsFor(role: Role): NavItem[] {
  const home: NavItem = { to: homeFor(role), label: 'Ana sayfa', icon: House }
  switch (role) {
    case 'Patient':
      return [home, { to: '/hasta/randevu-al', label: 'Randevu al', icon: CalendarPlus }, { to: '/hasta/randevularim', label: 'Randevularım', icon: ClipboardList }]
    case 'Doctor':
      return [home, { to: '/doktor/randevular', label: 'Randevular', icon: CalendarCheck }, { to: '/doktor/uygunluk', label: 'Uygunluk', icon: CalendarRange }]
    case 'ClinicManager':
      return [home, { to: '/yonetici/bolumler', label: 'Bölümler', icon: Layers }, { to: '/yonetici/doktorlar', label: 'Doktorlar', icon: Users }]
    case 'SystemAdmin':
      return [home, { to: '/sistem/kurumlar', label: 'Kurumlar', icon: Hospital }, { to: '/sistem/yoneticiler', label: 'Yöneticiler', icon: UserCog }]
    default:
      return [home]
  }
}

/** The roles that work in the staff shell (left sidebar); everyone else uses the top-navbar layout. */
export const STAFF_ROLES: readonly Role[] = ['Doctor', 'ClinicManager', 'SystemAdmin']
