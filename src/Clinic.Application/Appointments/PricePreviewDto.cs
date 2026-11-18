namespace Clinic.Application.Appointments;

/// <summary>
/// What a patient would pay for a service before booking: the service's <paramref name="BasePrice"/>, the
/// <paramref name="Price"/> the booking would store for this patient (same pricing strategy), and whether the
/// patient has insurance on file (so the screen can word the amount correctly).
/// </summary>
public record PricePreviewDto(Guid ServiceId, decimal BasePrice, decimal Price, bool HasInsurance);
