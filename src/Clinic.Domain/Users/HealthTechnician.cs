using Clinic.Domain.Clinics;

namespace Clinic.Domain.Users;

/// <summary>
/// A health technician (e.g. radiology technician, laboratory technician). Only a registered
/// staff member for now: there is no scheduling or assignment logic for technicians.
/// </summary>
public class HealthTechnician : StaffMember
{
    public string Title { get; private set; }

    public HealthTechnician(
        string fullName,
        string email,
        string phoneNumber,
        Department department,
        string title)
        : base(fullName, email, phoneNumber, department)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title;
    }

    private HealthTechnician()
    {
        Title = string.Empty;
    }

    public override string RoleName => "HealthTechnician";
}
