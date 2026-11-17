namespace Clinic.Application.Exceptions;

/// <summary>Thrown when a requested appointment does not fall within the clinic's working hours.</summary>
public class OutsideWorkingHoursException(Guid clinicFacilityId, DateTime startUtc, DateTime endUtc)
    : Exception($"Clinic {clinicFacilityId} is not open during {startUtc:O}-{endUtc:O} (working hours are in clinic local time).")
{
}
