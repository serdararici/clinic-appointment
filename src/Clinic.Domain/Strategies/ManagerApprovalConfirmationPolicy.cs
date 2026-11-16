using Clinic.Domain.Enums;

namespace Clinic.Domain.Strategies;

/// <summary>Appointments start as pending and require explicit manager approval.</summary>
public class ManagerApprovalConfirmationPolicy : IConfirmationPolicy
{
    public AppointmentStatus DetermineInitialStatus() => AppointmentStatus.Pending;
}
