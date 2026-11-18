using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>Appointments are confirmed immediately, with no review.</summary>
public class AutoConfirmationPolicy : IConfirmationPolicy
{
    public AppointmentStatus DetermineInitialStatus(Doctor? doctor) => AppointmentStatus.Confirmed;
}
