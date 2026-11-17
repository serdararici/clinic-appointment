namespace Clinic.Application.Exceptions;

/// <summary>Thrown when an appointment is requested with a staff member who has been deactivated.</summary>
public class StaffInactiveException(Guid staffMemberId)
    : Exception($"Staff member {staffMemberId} is inactive and cannot be booked.")
{
}
