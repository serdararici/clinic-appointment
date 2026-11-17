using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinic.Application.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure.Security;

/// <summary>
/// Creates signed JWTs (HS256). The token carries the user id (sub), email, name, role and, for staff and
/// managers, the branch and organization ids. "Now" comes from <see cref="TimeProvider"/> so expiry is testable.
/// </summary>
public class JwtTokenGenerator(JwtOptions options, TimeProvider timeProvider) : ITokenGenerator
{
    public IssuedToken Create(TokenSubject subject)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddHours(options.ExpiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, subject.Email),
            new(JwtRegisteredClaimNames.Name, subject.FullName),
            new(AuthClaims.Role, subject.Role)
        };

        if (subject.BranchId is { } branchId)
        {
            claims.Add(new Claim(AuthClaims.BranchId, branchId.ToString()));
        }

        if (subject.OrganizationId is { } organizationId)
        {
            claims.Add(new Claim(AuthClaims.OrganizationId, organizationId.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, notBefore: now, expires: expires, signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
