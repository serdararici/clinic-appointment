using Clinic.Domain.Enums;

namespace Clinic.Application.Appointments;

/// <summary>
/// Output DTO describing a persisted appointment. <paramref name="StaffMemberId"/> is null
/// when the appointment is unassigned (Diagnostic services).
/// </summary>
public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid? StaffMemberId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    decimal Price,
    AppointmentStatus Status);
