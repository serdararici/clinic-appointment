using System.Net.Mail;
using Clinic.Application.Exceptions;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Auth;

/// <summary>
/// Registration (patients only) and login. Passwords are stored only as hashes; a login answers the same
/// way for an unknown email and a wrong password.
/// </summary>
public class AuthService(
    IUserRepository users,
    IClinicFacilityRepository clinics,
    IPasswordHashing passwordHashing,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAuthService
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    // Verified against when the email is unknown, so an unknown email costs about as much time as a wrong
    // password and the response time does not reveal which emails exist.
    private readonly Lazy<string> _dummyHash = new(() => passwordHashing.Hash("not-a-real-password"));

    public async Task<AuthUserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = ValidateEmail(request.Email);

        ValidatePassword(request.Password);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (request.DateOfBirth < new DateOnly(1900, 1, 1) || request.DateOfBirth > today)
        {
            throw new ArgumentException("A valid date of birth is required.", nameof(request));
        }

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var patient = new Patient(
            request.FullName,
            email,
            request.PhoneNumber ?? string.Empty,
            request.DateOfBirth,
            string.IsNullOrWhiteSpace(request.InsuranceNumber) ? null : request.InsuranceNumber.Trim());
        patient.SetPasswordHash(passwordHashing.Hash(request.Password));

        await users.AddAsync(patient, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthUserDto(patient.Id, patient.FullName, patient.Email, patient.RoleName);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : await users.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        if (user?.PasswordHash is null)
        {
            // Unknown email, or an account that has no password yet: same time cost and same answer as a wrong password.
            passwordHashing.Verify(_dummyHash.Value, request.Password ?? string.Empty);
            throw new InvalidCredentialsException();
        }

        if (!passwordHashing.Verify(user.PasswordHash, request.Password ?? string.Empty))
        {
            throw new InvalidCredentialsException();
        }

        // A deactivated staff member or clinic manager has left: refuse with the same answer as a wrong password.
        if (user is StaffMember { IsActive: false } or ClinicManager { IsActive: false })
        {
            throw new InvalidCredentialsException();
        }

        // Staff and managers work in one branch; the token also carries that branch's organization.
        Guid? branchId = user switch
        {
            StaffMember staff => staff.ClinicFacilityId,
            ClinicManager manager => manager.ClinicFacilityId,
            _ => null
        };

        Guid? organizationId = null;
        if (branchId is { } id)
        {
            organizationId = (await clinics.GetByIdAsync(id, cancellationToken))?.OrganizationId;
        }

        var issued = tokenGenerator.Create(new TokenSubject(user.Id, user.FullName, user.Email, user.RoleName, branchId, organizationId));

        return new LoginResponse(
            issued.Token, issued.ExpiresAtUtc, user.Id, user.FullName, user.Email, user.RoleName, branchId, organizationId);
    }

    /// <summary>Shared by registration and by creating manager accounts.</summary>
    internal static void ValidatePassword(string? password)
    {
        if (password is null || password.Length < MinPasswordLength)
        {
            throw new ArgumentException($"The password must be at least {MinPasswordLength} characters long.", nameof(password));
        }

        if (password.Length > MaxPasswordLength)
        {
            throw new ArgumentException($"The password cannot be longer than {MaxPasswordLength} characters.", nameof(password));
        }
    }

    internal static string ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out var address) || address.Address != email.Trim())
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        return address.Address;
    }
}
