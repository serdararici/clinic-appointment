import { Navigate, Route, Routes } from 'react-router-dom'
import { GuestOnly } from './auth/GuestOnly'
import { RequireRole } from './auth/RequireRole'
import { homeFor } from './auth/roles'
import { useAuth } from './auth/useAuth'
import { Layout } from './layout/Layout'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'
import { BookingPage } from './pages/patient/BookingPage'
import { MyAppointmentsPage } from './pages/patient/MyAppointmentsPage'
import { PatientHome } from './pages/patient/PatientHome'
import { AvailabilityPage } from './pages/doctor/AvailabilityPage'
import { DoctorAppointmentsPage } from './pages/doctor/DoctorAppointmentsPage'
import { DoctorHome } from './pages/doctor/DoctorHome'
import { DepartmentsPage } from './pages/manager/DepartmentsPage'
import { DoctorsPage } from './pages/manager/DoctorsPage'
import { ManagerHome } from './pages/manager/ManagerHome'
import { AdminHome, TechnicianHome } from './pages/RoleHomes'

// "/" has no page of its own: it sends people to where they belong.
function RootRedirect() {
  const { user } = useAuth()
  return <Navigate to={user ? homeFor(user.role) : '/giris'} replace />
}

/**
 * The routes. Paths are Turkish because they show in the address bar:
 * /giris (login), /kayit (register), /hasta, /doktor, /personel, /yonetici, /sistem (home of each role).
 * <RequireRole> guards each role's pages, <GuestOnly> keeps logged-in users off the login and register pages.
 */
export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<RootRedirect />} />

        <Route element={<GuestOnly />}>
          <Route path="giris" element={<LoginPage />} />
          <Route path="kayit" element={<RegisterPage />} />
        </Route>

        <Route element={<RequireRole roles={['Patient']} />}>
          <Route path="hasta" element={<PatientHome />} />
          <Route path="hasta/randevu-al" element={<BookingPage />} />
          <Route path="hasta/randevularim" element={<MyAppointmentsPage />} />
        </Route>
        <Route element={<RequireRole roles={['Doctor']} />}>
          <Route path="doktor" element={<DoctorHome />} />
          <Route path="doktor/randevular" element={<DoctorAppointmentsPage />} />
          <Route path="doktor/uygunluk" element={<AvailabilityPage />} />
        </Route>
        <Route element={<RequireRole roles={['HealthTechnician']} />}>
          <Route path="personel" element={<TechnicianHome />} />
        </Route>
        <Route element={<RequireRole roles={['ClinicManager']} />}>
          <Route path="yonetici" element={<ManagerHome />} />
          <Route path="yonetici/bolumler" element={<DepartmentsPage />} />
          <Route path="yonetici/doktorlar" element={<DoctorsPage />} />
        </Route>
        <Route element={<RequireRole roles={['SystemAdmin']} />}>
          <Route path="sistem" element={<AdminHome />} />
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
