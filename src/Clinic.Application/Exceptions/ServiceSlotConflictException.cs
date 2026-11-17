namespace Clinic.Application.Exceptions;

/// <summary>Thrown when a diagnostic slot overlaps another active appointment for the same service in the same branch.</summary>
public class ServiceSlotConflictException(Guid serviceId, DateTime start, DateTime end)
    : Exception($"Service {serviceId} is already booked for a time overlapping {start:O}-{end:O}.")
{
}
