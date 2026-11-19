using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;

namespace Clinic.Domain.Users;

/// <summary>
/// A health technician (e.g. radiology technician, laboratory technician). Only a registered
/// staff member: there is no scheduling or assignment logic for technicians, and technicians cannot sign in.
/// </summary>
public class HealthTechnician : StaffMember
{
    /// <summary>The title a technician gets when none is given ("Sağlık Teknisyeni").</summary>
    public const string DefaultTitle = "Sağlık Teknisyeni";

    public string Title { get; private set; }

    /// <summary>Technicians are staff records only: they never sign in (and have no password).</summary>
    public override bool CanSignIn => false;

    public HealthTechnician(
        string fullName,
        string email,
        string phoneNumber,
        Department department,
        string title)
        : base(fullName, email, phoneNumber, department)
    {
        Title = ValidateTitle(title);
    }

    public void ChangeTitle(string title) => Title = ValidateTitle(title);

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return title.Trim();
    }

    private HealthTechnician()
    {
        Title = string.Empty;
    }

    public override string RoleName => "HealthTechnician";

    public override ServiceType OfferedServiceType => ServiceType.Diagnostic;
}
