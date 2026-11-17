namespace Clinic.Domain.Users;

/// <summary>
/// A platform-level administrator who is not tied to one branch or organization. Creates and deletes
/// organizations, creates clinic manager accounts, and can use every management endpoint across organizations.
/// </summary>
public class SystemAdmin : User
{
    public SystemAdmin(string fullName, string email, string phoneNumber)
        : base(fullName, email, phoneNumber)
    {
    }

    private SystemAdmin()
    {
    }

    public override string RoleName => "SystemAdmin";
}
