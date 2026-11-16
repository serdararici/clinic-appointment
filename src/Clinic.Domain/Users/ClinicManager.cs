namespace Clinic.Domain.Users;

/// <summary>
/// Manages a clinic/hospital facility: configures departments, services, doctors,
/// and approves or rejects appointments that require manager confirmation.
/// </summary>
public class ClinicManager : User
{
    public Guid ClinicFacilityId { get; private set; }

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
