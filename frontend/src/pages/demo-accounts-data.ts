import type { Role } from '../api/types'

export interface DemoAccount {
  name: string
  email: string
  role: Role
  /** Where this person works, when it applies. */
  workplace?: string
}

/** The password of every demo account. */
export const DEMO_PASSWORD = 'Demo123!'

/**
 * A short selection of the accounts the API seeds for demos (see CLAUDE.md "Demo accounts"): real data, not invented.
 * They are shown on the login page as a hint for trying out each role; the others are in the README.
 */
export const DEMO_ACCOUNTS: readonly DemoAccount[] = [
  { name: 'Sistem Yöneticisi', email: 'admin@clinic.example', role: 'SystemAdmin' },
  { name: 'Ayşe Yılmaz', email: 'ayse.yilmaz@sakaryasehir.example', role: 'ClinicManager', workplace: 'Sakarya Şehir Hastanesi - Merkez' },
  { name: 'Dr. Hakan Yıldız', email: 'hakan.yildiz@sakaryasehir.example', role: 'Doctor', workplace: 'Dahiliye, Merkez (otomatik onay)' },
  { name: 'Dr. Kemal Aslan', email: 'kemal.aslan@sakaryasehir.example', role: 'Doctor', workplace: 'Kardiyoloji, Merkez (onay gerekir)' },
  { name: 'Ahmet Çelik', email: 'ahmet.celik@example.com', role: 'Patient', workplace: 'sigortalı hasta' },
]
