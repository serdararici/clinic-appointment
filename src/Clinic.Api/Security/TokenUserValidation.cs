using System.Security.Claims;
using Clinic.Application.Auth;

namespace Clinic.Api.Security;

/// <summary>
/// The extra check run after a token's signature, issuer, audience and expiry are valid: the user in the token must
/// still exist and be active. It does not look at the role: the roles come from the token, as before.
/// </summary>
public static class TokenUserValidation
{
    /// <summary>False for a token without a readable user id, for a deleted user and for a deactivated one.</summary>
    public static async Task<bool> IsAccountUsableAsync(ClaimsPrincipal? principal, IUserStatusChecker checker, CancellationToken cancellationToken = default)
    {
        var subject = principal?.FindFirst("sub")?.Value;
        return Guid.TryParse(subject, out var userId) && await checker.IsUsableAsync(userId, cancellationToken);
    }
}
