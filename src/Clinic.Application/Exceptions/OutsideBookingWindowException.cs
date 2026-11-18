namespace Clinic.Application.Exceptions;

/// <summary>
/// Thrown when an appointment is requested for a date outside the rolling booking window. The message is shown to
/// the patient as it is, so it is written in Turkish (the API's other messages are English and translated by the UI).
/// </summary>
public class OutsideBookingWindowException()
    : Exception($"Randevu yalnızca bugünden itibaren {Clinic.Domain.Scheduling.BookingRules.WindowDays} gün içindeki tarihler için alınabilir.")
{
}
