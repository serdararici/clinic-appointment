namespace Clinic.Application.Exceptions;

/// <summary>Thrown when a requested appointment time falls outside any of the doctor's availability slots.</summary>
public class NotAvailableException(Guid doctorId, DateTime start, DateTime end)
    : Exception($"Doctor {doctorId} has no availability slot covering {start:O}-{end:O}.")
{
}
