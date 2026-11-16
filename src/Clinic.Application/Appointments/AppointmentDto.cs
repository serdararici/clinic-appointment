using Clinic.Domain.Enums;

namespace Clinic.Application.Appointments;

/// <summary>Output DTO describing a persisted appointment.</summary>
public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    decimal Price,
    AppointmentStatus Status);
