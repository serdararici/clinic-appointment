using Clinic.Domain.Enums;

namespace Clinic.Domain.Strategies;

/// <summary>Appointments are confirmed immediately, with no manager review.</summary>
public class AutoConfirmationPolicy : IConfirmationPolicy
{
    public AppointmentStatus DetermineInitialStatus() => AppointmentStatus.Confirmed;
}
