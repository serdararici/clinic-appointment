import type { Role } from '../api/types'
import { homeFor } from '../auth/roles'

export interface NavItem {
  to: string
  label: string
}

/**
 * The navbar links of each role. For now every role has just its home page; as screens are added
 * (appointments, schedule, management lists...) they get listed here and nowhere else.
 */
export function navItemsFor(role: Role): NavItem[] {
  // "Hesabım" is in the account menu (and the phone menu), not among the page links.
  return roleItems(role)
}

function roleItems(role: Role): NavItem[] {
  const home = { to: homeFor(role), label: 'Ana sayfa' }
  if (role === 'Patient') {
    return [home, { to: '/hasta/randevu-al', label: 'Randevu al' }, { to: '/hasta/randevularim', label: 'Randevularım' }]
  }
  if (role === 'Doctor') {
    return [home, { to: '/doktor/randevular', label: 'Randevular' }, { to: '/doktor/uygunluk', label: 'Uygunluk' }]
  }
  if (role === 'ClinicManager') {
    return [home, { to: '/yonetici/bolumler', label: 'Bölümler' }, { to: '/yonetici/doktorlar', label: 'Doktorlar' }]
  }
  if (role === 'SystemAdmin') {
    return [home, { to: '/sistem/kurumlar', label: 'Kurumlar' }, { to: '/sistem/yoneticiler', label: 'Yöneticiler' }]
  }
  return [home]
}
