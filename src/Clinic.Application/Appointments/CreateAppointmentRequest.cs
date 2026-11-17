namespace Clinic.Application.Appointments;

/// <summary>
/// Input DTO for booking a new appointment. The patient is the logged-in patient (from the token); a
/// <c>patientId</c> in the request body is ignored. <paramref name="StartUtc"/> must be UTC ("...Z")
/// or carry an explicit offset; a date-time with no zone information is rejected.
/// <paramref name="DoctorId"/> is required for Examination services and ignored for Diagnostic
/// services, which are booked without choosing staff.
/// </summary>
public record CreateAppointmentRequest(
    Guid ServiceId,
    DateTime StartUtc,
    Guid? DoctorId = null);
