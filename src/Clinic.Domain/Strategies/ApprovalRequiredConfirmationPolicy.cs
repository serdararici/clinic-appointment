using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>Appointments start as pending and wait for explicit approval (by the assigned doctor, or a manager).</summary>
public class ApprovalRequiredConfirmationPolicy : IConfirmationPolicy
{
    public AppointmentStatus DetermineInitialStatus(Doctor? doctor) => AppointmentStatus.Pending;
}
