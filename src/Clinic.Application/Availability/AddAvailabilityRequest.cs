namespace Clinic.Application.Availability;

/// <summary>
/// Input DTO for adding an availability range. Values must be UTC ("...Z") or carry an
/// explicit offset; a date-time with no zone information is rejected as ambiguous.
/// </summary>
public record AddAvailabilityRequest(DateTime StartUtc, DateTime EndUtc);
