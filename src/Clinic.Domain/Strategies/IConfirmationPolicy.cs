using Clinic.Domain.Enums;

namespace Clinic.Domain.Strategies;

/// <summary>
/// Strategy pattern: decides the initial <see cref="AppointmentStatus"/> a newly
/// created appointment should have (e.g. auto-confirm vs. require manager approval).
/// </summary>
public interface IConfirmationPolicy
{
    AppointmentStatus DetermineInitialStatus();
}
