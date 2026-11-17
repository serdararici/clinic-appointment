namespace Clinic.Application.Appointments;

/// <summary>
/// Input DTO for booking a new appointment. <paramref name="StartUtc"/> must be UTC ("...Z")
/// or carry an explicit offset; a date-time with no zone information is rejected.
/// </summary>
public record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateTime StartUtc);
