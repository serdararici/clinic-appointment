namespace Clinic.Application.Exceptions;

/// <summary>Thrown when an appointment is requested for a start time that has already passed.</summary>
public class PastBookingException(DateTime startUtc)
    : Exception($"{startUtc:O} is in the past; appointments can only be booked for the current time or later.")
{
}
