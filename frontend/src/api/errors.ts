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
  [/availability range overlaps an existing/i, () => 'Bu aralık mevcut bir uygunluk aralığıyla çakışıyor.'],
  [/end time must be after start time/i, () => 'Bitiş saati başlangıç saatinden sonra olmalıdır.'],
  [/todate must not be before fromdate/i, () => 'Bitiş tarihi başlangıç tarihinden önce olamaz.'],
  [/date range cannot be longer than (\d+)/i, (m) => `Tarih aralığı en fazla ${m[1]} gün olabilir.`],
  [/at least one weekday is required/i, () => 'En az bir gün seçin.'],
  [/availability range that still has active appointments/i, () => 'Bu aralıkta aktif randevular olduğu için silinemez. Önce randevuları iptal edin.'],
  [/slot duration must be one of/i, () => 'Randevu süresi 10, 15, 20, 30 veya 60 dakika olmalıdır.'],
  [/department still has services/i, () => 'Bölümde hâlâ hizmet var. Önce hizmetleri silin.'],
  [/department still has staff/i, () => 'Bölümde hâlâ personel var. Önce personeli silin veya taşıyın.'],
  [/service type cannot be changed/i, () => 'Bu hizmet için randevu olduğundan hizmet türü değiştirilemez.'],
  [/appointments refer to this service/i, () => 'Bu hizmete ait randevular olduğundan hizmet silinemez.'],
  [/staff are registered as offering this service/i, () => 'Bu hizmeti veren doktorlar var. Önce hizmeti doktorlardan kaldırın.'],
  [/staff member has upcoming appointments/i, () => 'Yaklaşan randevuları olduğundan bu işlem yapılamıyor. Önce randevuları iptal edin.'],
  [/service name is required/i, () => 'Hizmet adı zorunludur.'],
  [/department name is required/i, () => 'Bölüm adı zorunludur.'],
  [/duration must be positive/i, () => 'Süre sıfırdan büyük olmalıdır.'],
  [/duration cannot exceed/i, () => 'Süre en fazla 24 saat olabilir.'],
  [/price cannot be negative/i, () => 'Ücret negatif olamaz.'],
  [/price cannot exceed/i, () => 'Ücret çok yüksek.'],
  [/price can have at most two decimal/i, () => 'Ücret en fazla iki ondalık basamak içerebilir.'],
  [/organization still has branches/i, () => 'Kurumun şubeleri var. Önce şubeleri silin.'],
  [/branch still has departments/i, () => 'Şubede bölümler var. Önce bölümleri silin.'],
  [/branch still has clinic managers/i, () => 'Şubeye bağlı yöneticiler var. Önce yöneticileri başka şubeye taşıyın veya pasifleştirin.'],
  [/working hours overlap an existing range/i, () => 'Bu saat aralığı aynı günün mevcut bir aralığıyla çakışıyor.'],
  [/closing time must be after opening time/i, () => 'Kapanış saati açılış saatinden sonra olmalıdır.'],
  [/organization name is required/i, () => 'Kurum adı zorunludur.'],
  [/branch name is required/i, () => 'Şube adı zorunludur.'],
  [/branch address is required/i, () => 'Şube adresi zorunludur.'],
  [/current password is incorrect/i, () => 'Mevcut şifreniz hatalı.'],
  [/phone number cannot be longer than (\d+)/i, (m) => `Telefon numarası en fazla ${m[1]} karakter olabilir.`],
  [/insurance number cannot be longer than (\d+)/i, (m) => `Sigorta numarası en fazla ${m[1]} karakter olabilir.`],
  // Booking: the reasons a slot cannot be booked (409 and 422).
  [/already has an appointment overlapping/i, () => 'Bu saat az önce doldu. Lütfen başka bir saat seçin.'],
  [/is already booked for a time overlapping/i, () => 'Bu saat az önce doldu. Lütfen başka bir saat seçin.'],
  [/has no availability slot covering/i, () => 'Doktor bu saatte çalışmıyor. Lütfen başka bir saat seçin.'],
  [/is not open during/i, () => 'Şube bu saatte kapalı. Lütfen başka bir saat seçin.'],
  [/is in the past; appointments can only be booked/i, () => 'Geçmiş bir saat için randevu alınamaz.'],
  [/is not the start of a bookable slot/i, () => 'Seçtiğiniz saat geçerli bir randevu saati değil. Lütfen listeden bir saat seçin.'],
  [/does not offer service/i, () => 'Bu doktor seçtiğiniz hizmeti vermiyor.'],
  [/is inactive and cannot be booked/i, () => 'Bu doktor şu anda randevu almıyor.'],
  [/doctor is required to book an examination/i, () => 'Muayene için bir doktor seçmelisiniz.'],
  [/only a patient can book/i, () => 'Randevuyu yalnızca hasta hesabı alabilir.'],
  [/only a system administrator can do this/i, () => 'Bu işlemi yalnızca sistem yöneticisi yapabilir.'],
  [/belongs to another organization/i, () => 'Bu kayıt başka bir kuruma ait.'],
  [/endtime must be after starttime/i, () => 'Bitiş saati başlangıç saatinden sonra olmalıdır.'],
  [/^email is required/i, () => 'E-posta zorunludur.'],
  // The booking-window message is already Turkish (written for the patient), so it is shown as it is.
  [/^Randevu yalnızca bugünden itibaren.*$/, (m) => m[0]],
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
