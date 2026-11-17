namespace Clinic.Application.Exceptions;

/// <summary>Thrown when an appointment is requested for a service the chosen doctor does not provide.</summary>
public class ServiceNotOfferedException(Guid doctorId, Guid serviceId)
    : Exception($"Doctor {doctorId} does not offer service {serviceId}.")
{
}
