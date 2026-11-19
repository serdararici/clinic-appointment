import { Alert } from '../../components/Alert'

/** A non-blocking amber hint: saving works as always, the part outside the branch hours is just not offered to patients. */
export function OutsideHoursNotice() {
  return <Alert tone="warning">Şubenin çalışma saati dışında kalan kısım randevuya açılmaz.</Alert>
}
