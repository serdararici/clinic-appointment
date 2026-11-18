/**
 * API errors. The API answers errors with a short plain-text message in English (for example
 * "An account with this email address already exists."), or with a JSON problem document when the request
 * body itself is malformed. The UI is Turkish, so the raw server text is kept for debugging but what we show
 * is a Turkish message: a translation of the messages we know, otherwise a generic message for the status code.
 */
export class ApiError extends Error {
  readonly status: number
  /** The server's own message (English); useful for logs, not shown to users. */
  readonly serverMessage: string

  constructor(status: number, message: string, serverMessage: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.serverMessage = serverMessage
  }
}

// Server messages we know, with their Turkish text. Add to this list as screens start depending on a message.
const KNOWN_MESSAGES: ReadonlyArray<readonly [RegExp, (match: RegExpMatchArray) => string]> = [
  [/password must be at least (\d+)/i, (m) => `Şifre en az ${m[1]} karakter olmalıdır.`],
  [/password cannot be longer than (\d+)/i, (m) => `Şifre en fazla ${m[1]} karakter olabilir.`],
  [/account with this email address already exists/i, () => 'Bu e-posta adresiyle kayıtlı bir hesap zaten var.'],
  [/valid email address is required/i, () => 'Geçerli bir e-posta adresi girin.'],
  [/valid date of birth is required/i, () => 'Geçerli bir doğum tarihi girin.'],
  [/full name is required/i, () => 'Ad soyad zorunludur.'],
  [/invalid email or password/i, () => 'E-posta veya şifre hatalı.'],
  [/token is invalid or has expired/i, () => 'Oturumunuz sona erdi. Lütfen yeniden giriş yapın.'],
  [/authentication is required/i, () => 'Devam etmek için giriş yapmalısınız.'],
]

// Used when the server message is not in the list above.
function genericMessage(status: number): string {
  switch (status) {
    case 400:
      return 'Gönderilen bilgiler geçersiz. Lütfen alanları kontrol edin.'
    case 401:
      return 'Bu işlem için giriş yapmanız gerekiyor.'
    case 403:
      return 'Bu işlem için yetkiniz yok.'
    case 404:
      return 'İstenen kayıt bulunamadı.'
    case 409:
      return 'Bu işlem mevcut kayıtlarla çakıştığı için yapılamadı.'
    case 422:
      return 'İşlem şu anda yapılamıyor.'
    default:
      return status >= 500
        ? 'Sunucuda bir hata oluştu. Lütfen daha sonra tekrar deneyin.'
        : 'Beklenmeyen bir hata oluştu.'
  }
}

/** Turns an error response body (plain text or JSON) into one readable string. */
export function parseErrorBody(text: string, contentType: string | null): string {
  const trimmed = text.trim()
  if (!trimmed) return ''

  if (contentType?.includes('json')) {
    try {
      const body = JSON.parse(trimmed) as { errors?: Record<string, string[]>; detail?: string; title?: string }
      // ASP.NET model-binding failures: { errors: { field: ["message", ...] } }
      if (body.errors) return Object.values(body.errors).flat().join(' ')
      return body.detail ?? body.title ?? trimmed
    } catch {
      return trimmed
    }
  }

  return trimmed
}

/** Builds the ApiError for a failed response. */
export function createApiError(status: number, serverMessage: string): ApiError {
  const known = KNOWN_MESSAGES.map(([pattern, translate]) => {
    const match = serverMessage.match(pattern)
    return match ? translate(match) : null
  }).find((text) => text !== null)

  return new ApiError(status, known ?? genericMessage(status), serverMessage)
}

/** A message that is safe to show for any caught value (ApiError, network failure, or anything else). */
export function readableMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}
