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

    /// <summary>
    /// Salted hash of the password (null for an account that cannot log in yet). The domain only stores
    /// it; hashing and checking happen in the Application/Infrastructure layers.
    /// </summary>
    public string? PasswordHash { get; private set; }

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

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
    }

    public const int MaxPhoneNumberLength = 30;

    /// <summary>Changes only the phone number (blank clears it); at most <see cref="MaxPhoneNumberLength"/> characters.</summary>
    public void ChangePhoneNumber(string? phoneNumber)
    {
        var trimmed = phoneNumber?.Trim() ?? string.Empty;
        if (trimmed.Length > MaxPhoneNumberLength)
        {
            throw new ArgumentException($"The phone number cannot be longer than {MaxPhoneNumberLength} characters.", nameof(phoneNumber));
        }

        PhoneNumber = trimmed;
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

    /// <summary>
    /// Whether the account may sign in and keep using the API. Always true, except for staff and clinic managers who
    /// have been deactivated (they override this). One definition for the login and for every later request.
    /// </summary>
    public virtual bool CanSignIn => true;

    /// <summary>Human-readable role name, overridden by each concrete role (polymorphism).</summary>
    public abstract string RoleName { get; }
}
