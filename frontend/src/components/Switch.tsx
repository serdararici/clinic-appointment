import { cx } from '../utils/cx'

interface SwitchProps {
  checked: boolean
  onChange: () => void
  /** The accessible name of the switch (the visible text next to it should say the same). */
  label: string
  disabled?: boolean
}

/** An on/off switch (a button with role="switch"); the state is also written next to it by the caller, not only shown by color. */
export function Switch({ checked, onChange, label, disabled }: SwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={onChange}
      className={cx('relative h-7 w-12 shrink-0 rounded-full transition-colors duration-150 disabled:opacity-50', checked ? 'bg-teal-700' : 'bg-slate-300')}
    >
      <span className={cx('absolute top-0.5 h-6 w-6 rounded-full bg-white transition-all duration-150', checked ? 'left-[1.375rem]' : 'left-0.5')} />
    </button>
  )
}
