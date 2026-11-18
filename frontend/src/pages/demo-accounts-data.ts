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
 * The accounts the API seeds for demos (see CLAUDE.md "Demo accounts"): real data, not invented.
 * They are shown on the login page as a hint for trying out each role.
 */
export const DEMO_ACCOUNTS: readonly DemoAccount[] = [
  { name: 'Sistem Yöneticisi', email: 'admin@clinic.example', role: 'SystemAdmin' },
  { name: 'Ayşe Yılmaz', email: 'ayse.yilmaz@sakaryasehir.example', role: 'ClinicManager', workplace: 'Sakarya Şehir Hastanesi - Merkez' },
  { name: 'Mehmet Kara', email: 'mehmet.kara@sakaryasehir.example', role: 'ClinicManager', workplace: 'Sakarya Şehir Hastanesi - Adapazarı Şubesi' },
  { name: 'Ali Rıza Erdoğan', email: 'ali.erdogan@marmarapoliklinik.example', role: 'ClinicManager', workplace: 'Marmara Poliklinik - Serdivan Kliniği' },
  { name: 'Dr. Hakan Yıldız', email: 'hakan.yildiz@sakaryasehir.example', role: 'Doctor', workplace: 'Merkez' },
  { name: 'Dr. Elif Şahin', email: 'elif.sahin@sakaryasehir.example', role: 'Doctor', workplace: 'Merkez' },
  { name: 'Dr. Zeynep Çelik', email: 'zeynep.celik@sakaryasehir.example', role: 'Doctor', workplace: 'Adapazarı Şubesi' },
  { name: 'Dr. Burak Öztürk', email: 'burak.ozturk@sakaryasehir.example', role: 'Doctor', workplace: 'Adapazarı Şubesi' },
  { name: 'Dr. Can Aksoy', email: 'can.aksoy@marmarapoliklinik.example', role: 'Doctor', workplace: 'Serdivan Kliniği' },
  { name: 'Ahmet Çelik', email: 'ahmet.celik@example.com', role: 'Patient' },
  { name: 'Fatma Yıldırım', email: 'fatma.yildirim@example.com', role: 'Patient' },
]
