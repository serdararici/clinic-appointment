namespace Clinic.Domain.Enums;

/// <summary>
/// How a service is booked. The type, not the service name, decides the booking flow.
/// </summary>
public enum ServiceType
{
    /// <summary>Seen by a chosen doctor; the appointment length is that doctor's slot duration.</summary>
    Examination,

    /// <summary>A procedure (X-ray, MRI, blood test) with a fixed duration; no staff member is chosen.</summary>
    Diagnostic
}
