namespace Clinic.Domain.Users;

/// <summary>
/// Manages a clinic/hospital facility: configures departments, services, doctors,
/// and approves or rejects appointments that require manager confirmation.
/// </summary>
public class ClinicManager : User
{
    public Guid ClinicFacilityId { get; private set; }

    /// <summary>A deactivated manager cannot log in (the account and its history stay).</summary>
    public bool IsActive { get; private set; } = true;

    public override bool CanSignIn => IsActive;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public ClinicManager(string fullName, string email, string phoneNumber, Guid clinicFacilityId)
        : base(fullName, email, phoneNumber)
    {
        ClinicFacilityId = clinicFacilityId;
    }

    private ClinicManager()
    {
    }

    public override string RoleName => "ClinicManager";
}
