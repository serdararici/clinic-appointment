/** Joins class names, skipping the ones that are false/empty: cx('a', isOn && 'b') -> 'a b' or 'a'. */
export function cx(...parts: Array<string | false | null | undefined>): string {
  return parts.filter(Boolean).join(' ')
}
