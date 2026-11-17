namespace Clinic.Application.Auth;

/// <summary>Names of the custom claims in the JWT (the standard ones, sub / email / name, are used as they are).</summary>
public static class AuthClaims
{
    public const string Role = "role";
    public const string BranchId = "branch_id";
    public const string OrganizationId = "organization_id";
}

/// <summary>Hashes and checks passwords. The implementation uses ASP.NET's PasswordHasher (salted PBKDF2).</summary>
public interface IPasswordHashing
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

/// <summary>What goes into a token: who the user is and, for staff and managers, where they work.</summary>
public record TokenSubject(Guid UserId, string FullName, string Email, string Role, Guid? BranchId, Guid? OrganizationId);

public record IssuedToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenGenerator
{
    IssuedToken Create(TokenSubject subject);
}

/// <summary>Input DTO for self-registration; a registration always creates a Patient.</summary>
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string? InsuranceNumber = null);

public record LoginRequest(string Email, string Password);

public record AuthUserDto(Guid Id, string FullName, string Email, string Role);

/// <summary>
/// Result of a successful login. <paramref name="BranchId"/> and <paramref name="OrganizationId"/> are set for
/// staff and clinic managers and null for patients.
/// </summary>
public record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    Guid? BranchId,
    Guid? OrganizationId);

public interface IAuthService
{
    /// <summary>Creates a Patient account. 400 for a short password or invalid input, 409 if the email is taken.</summary>
    Task<AuthUserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Checks the credentials and issues a token. Wrong password and unknown email look the same (401).</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
