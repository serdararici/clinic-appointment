namespace Clinic.Domain.Enums;

/// <summary>
/// Lifecycle states of an appointment, from creation to final resolution.
/// </summary>
public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled
}
