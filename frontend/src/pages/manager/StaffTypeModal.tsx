import { Microscope, Stethoscope, type LucideIcon } from 'lucide-react'
import { Icon } from '../../components/Icon'
import { Modal } from '../../components/Modal'

export type StaffKind = 'doctor' | 'technician'

const OPTIONS: ReadonlyArray<{ kind: StaffKind; title: string; description: string; icon: LucideIcon }> = [
  { kind: 'doctor', title: 'Doktor', description: 'Randevu alır; geçici şifreyle giriş yapar.', icon: Stethoscope },
  { kind: 'technician', title: 'Sağlık teknisyeni', description: 'Yalnızca personel kaydı; giriş yapmaz.', icon: Microscope },
]

/** The first step of "Personel ekle": which kind of staff member. The matching form opens next. */
export function StaffTypeModal({ open, onClose, onPick }: { open: boolean; onClose: () => void; onPick: (kind: StaffKind) => void }) {
  return (
    <Modal open={open} onClose={onClose} title="Personel ekle">
      <div className="space-y-3">
        <p className="text-slate-700">Eklemek istediğiniz personel türünü seçin.</p>
        <ul className="grid gap-2">
          {OPTIONS.map((option) => (
            <li key={option.kind}>
              <button
                type="button"
                onClick={() => onPick(option.kind)}
                className="flex min-h-14 w-full items-start gap-3 rounded-xl border border-slate-200 px-4 py-3 text-left transition-colors duration-150 hover:border-teal-700 hover:bg-teal-50/60"
              >
                <Icon icon={option.icon} size={20} className="mt-0.5 text-teal-700" />
                <span>
                  <span className="block font-semibold text-slate-900">{option.title}</span>
                  <span className="block text-sm text-slate-600">{option.description}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      </div>
    </Modal>
  )
}
