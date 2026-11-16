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

    public override string RoleName => "Patient";

    public bool HasInsurance => !string.IsNullOrWhiteSpace(InsuranceNumber);
}
