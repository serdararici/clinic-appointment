using Clinic.Application.Auth;

namespace Clinic.Api.Security;

/// <summary>The caller of the current request, read from the claims of the validated JWT.</summary>
public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => GuidClaim("sub");

    public string? Role => Principal?.FindFirst(AuthClaims.Role)?.Value;

    public Guid? BranchId => GuidClaim(AuthClaims.BranchId);

    public Guid? OrganizationId => GuidClaim(AuthClaims.OrganizationId);

    private Guid? GuidClaim(string type) =>
        Guid.TryParse(Principal?.FindFirst(type)?.Value, out var id) ? id : null;
}
