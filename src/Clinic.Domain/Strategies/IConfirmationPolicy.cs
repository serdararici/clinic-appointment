using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>
/// Strategy pattern: decides the initial <see cref="AppointmentStatus"/> a newly
/// created appointment should have (e.g. auto-confirm vs. require approval).
/// </summary>
public interface IConfirmationPolicy
{
    /// <param name="doctor">The doctor the appointment is booked with; null for a booking without a doctor (a Diagnostic service).</param>
    AppointmentStatus DetermineInitialStatus(Doctor? doctor);
}
