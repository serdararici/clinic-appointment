using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>
/// Lets each doctor choose: a booking without a doctor (a Diagnostic service) or with a doctor who has
/// <see cref="Doctor.AutoConfirmAppointments"/> switched on gets the auto policy's answer (Confirmed); a booking with
/// a doctor who wants to approve appointments gets the approval policy's answer (Pending).
/// </summary>
public class DoctorPreferenceConfirmationPolicy(AutoConfirmationPolicy auto, ApprovalRequiredConfirmationPolicy approval) : IConfirmationPolicy
{
    public AppointmentStatus DetermineInitialStatus(Doctor? doctor) =>
        doctor is null || doctor.AutoConfirmAppointments
            ? auto.DetermineInitialStatus(doctor)
            : approval.DetermineInitialStatus(doctor);
}
