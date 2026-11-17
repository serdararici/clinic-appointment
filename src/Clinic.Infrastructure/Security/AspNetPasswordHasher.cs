using Clinic.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace Clinic.Infrastructure.Security;

/// <summary>
/// Password hashing with ASP.NET's <see cref="PasswordHasher{TUser}"/> (salted PBKDF2). Only the hasher is
/// used, not the Identity user/role tables. The hasher does not look at the user object, so a plain object is passed.
/// </summary>
public class AspNetPasswordHasher : IPasswordHashing
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string password)
    {
        // A malformed stored hash must not throw out of a login; it simply does not match.
        try
        {
            return _hasher.VerifyHashedPassword(new object(), passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
