/**
 * Text prepared for comparing: Turkish lower-casing ("İ" becomes "i", "I" becomes "ı") and a canonical Unicode form,
 * so a name typed in capitals, in lower case or with decomposed characters still matches. Both sides of a search go
 * through this.
 */
export function normalizeText(text: string): string {
  return text.toLocaleLowerCase('tr-TR').normalize('NFC').trim()
}

/** Whether the query occurs in any of the texts (an empty query matches everything). */
export function matchesQuery(query: string, texts: ReadonlyArray<string | null | undefined>): boolean {
  const needle = normalizeText(query)
  if (!needle) return true
  return texts.some((text) => text != null && normalizeText(text).includes(needle))
}
