namespace Clinic.Application.Exceptions;

/// <summary>Thrown when a requested appointment time overlaps an existing one for the same doctor.</summary>
public class AppointmentConflictException(Guid doctorId, DateTime start, DateTime end)
    : Exception($"Doctor {doctorId} already has an appointment overlapping {start:O}-{end:O}.")
{
}
