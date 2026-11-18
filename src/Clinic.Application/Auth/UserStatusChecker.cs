using Clinic.Domain.Repositories;

namespace Clinic.Application.Auth;

/// <summary>
/// Answers whether the account behind a (correctly signed) token still exists and may be used. A token is valid for
/// 8 hours, but the account can be deleted (a reset database) or deactivated in that time.
/// </summary>
public interface IUserStatusChecker
{
    Task<bool> IsUsableAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>One lookup of the user by id (a primary-key query) per check.</summary>
public class UserStatusChecker(IUserRepository users) : IUserStatusChecker
{
    public async Task<bool> IsUsableAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is { CanSignIn: true };
    }
}
