using Clinic.Domain.Clinics;

namespace Clinic.Domain.Users;

/// <summary>
/// A doctor. Staff data (department, services, availability, slot duration) comes from
/// <see cref="StaffMember"/>; a doctor adds a medical specialty. Examination appointments
/// are booked with a doctor. A doctor has no specialty of their own: it is the name of their
/// department, so the two can never disagree.
/// </summary>
public class Doctor : StaffMember
{
    /// <summary>
    /// Whether the default work schedule (weekdays 09:00-12:00 and 13:00-16:00) is kept filled in automatically
    /// for the booking window. On by default; a doctor turns it off for a custom schedule or a long leave.
    /// </summary>
    public bool AutoScheduleEnabled { get; private set; } = true;

    public void SetAutoSchedule(bool enabled) => AutoScheduleEnabled = enabled;

    public Doctor(
        string fullName,
        string email,
        string phoneNumber,
        Department department,
        int slotDurationMinutes = DefaultSlotDurationMinutes)
        : base(fullName, email, phoneNumber, department, slotDurationMinutes)
    {
    }

    private Doctor()
    {
    }

    public override string RoleName => "Doctor";
}
