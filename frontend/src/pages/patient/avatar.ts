/** The initials of a person: first letters of the first and the last word, without a leading "Dr." ("Dr. Hakan Yıldız" gives "HY"). */
export function initialsOf(fullName: string): string {
  const words = fullName
    .replace(/^\s*dr\.?\s+/i, '')
    .split(/\s+/)
    .filter(Boolean)
  if (words.length === 0) return '?'
  const first = words[0][0]
  const last = words.length > 1 ? words[words.length - 1][0] : ''
  return (first + last).toLocaleUpperCase('tr-TR')
}

// Tonal pairs of the existing palette (teal and slate): calm, readable, and none of them is a status color.
const AVATAR_TONES = [
  'bg-teal-100 text-teal-800',
  'bg-teal-200 text-teal-900',
  'bg-slate-200 text-slate-800',
  'bg-slate-300 text-slate-900',
  'bg-teal-50 text-teal-800 ring-1 ring-inset ring-teal-200',
  'bg-slate-100 text-slate-800 ring-1 ring-inset ring-slate-300',
]

/** The same name always gets the same color (a small hash of its characters). */
export function avatarToneOf(fullName: string): string {
  let hash = 0
  for (const char of fullName) hash = (hash * 31 + char.codePointAt(0)!) >>> 0
  return AVATAR_TONES[hash % AVATAR_TONES.length]
}
