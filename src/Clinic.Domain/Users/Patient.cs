namespace Clinic.Domain.Users;

/// <summary>
/// A patient who can book appointments. Concrete subclass of <see cref="User"/>.
/// </summary>
public class Patient : User
{
    public DateOnly DateOfBirth { get; private set; }
    public string? InsuranceNumber { get; private set; }

    public Patient(string fullName, string email, string phoneNumber, DateOnly dateOfBirth, string? insuranceNumber = null)
        : base(fullName, email, phoneNumber)
    {
        DateOfBirth = dateOfBirth;
        InsuranceNumber = insuranceNumber;
    }

    private Patient()
    {
    }

    public const int MaxInsuranceNumberLength = 50;

    /// <summary>Sets the insurance number; blank removes it (the patient then pays the full price).</summary>
    public void ChangeInsuranceNumber(string? insuranceNumber)
    {
        var trimmed = insuranceNumber?.Trim();
        if (trimmed is { Length: > MaxInsuranceNumberLength })
        {
            throw new ArgumentException($"The insurance number cannot be longer than {MaxInsuranceNumberLength} characters.", nameof(insuranceNumber));
        }

        InsuranceNumber = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public override string RoleName => "Patient";

    public bool HasInsurance => !string.IsNullOrWhiteSpace(InsuranceNumber);
}
