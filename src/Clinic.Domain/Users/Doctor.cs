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
