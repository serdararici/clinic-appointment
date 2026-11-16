namespace Clinic.Application.Appointments;

/// <summary>Input DTO for booking a new appointment.</summary>
public record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateTime StartUtc);
