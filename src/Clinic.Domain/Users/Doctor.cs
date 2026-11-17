using Clinic.Domain.Clinics;

namespace Clinic.Domain.Users;

/// <summary>
/// A doctor. Staff data (department, services, availability, slot duration) comes from
/// <see cref="StaffMember"/>; a doctor adds a medical specialty. Examination appointments
/// are booked with a doctor.
/// </summary>
public class Doctor : StaffMember
{
    public string Specialty { get; private set; }

    public Doctor(
        string fullName,
        string email,
        string phoneNumber,
        Department department,
        string specialty,
        int slotDurationMinutes = DefaultSlotDurationMinutes)
        : base(fullName, email, phoneNumber, department, slotDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(specialty))
        {
            throw new ArgumentException("Specialty is required.", nameof(specialty));
        }

        Specialty = specialty;
    }

    private Doctor()
    {
        Specialty = string.Empty;
    }

    public override string RoleName => "Doctor";
}
