using Clinic.Domain.Common;

namespace Clinic.Domain.Users;

/// <summary>
/// Abstract base type for every actor in the system (Patient, Doctor, ClinicManager).
/// Holds the identity/contact fields shared by all roles and encapsulates the
/// invariants around them; role-specific state and behavior live in subclasses.
/// </summary>
public abstract class User : Entity
{
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    protected User(string fullName, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>EF Core requires a parameterless constructor for materialization.</summary>
    protected User()
    {
        FullName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
    }

    public void UpdateContactInfo(string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = email;
        PhoneNumber = phoneNumber;
    }

    /// <summary>Human-readable role name, overridden by each concrete role (polymorphism).</summary>
    public abstract string RoleName { get; }
}
