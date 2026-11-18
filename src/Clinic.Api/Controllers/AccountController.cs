using Clinic.Api.Security;
using Clinic.Application.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// "My account" for every logged-in role. The user is always the one in the token; there is no id in the route.
/// Errors are mapped centrally by <c>ApiExceptionHandler</c>: 400 invalid input or a wrong current password.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    /// <summary>The own profile: name, email, phone, role and, for patients, the insurance number.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AnyRole)]
    public async Task<ActionResult<AccountDto>> Get(CancellationToken cancellationToken) =>
        Ok(await accountService.GetAsync(cancellationToken));

    /// <summary>Changes the full name and phone (and the insurance number of a patient). The email cannot be changed.</summary>
    [HttpPut]
    [Authorize(Roles = AppRoles.AnyRole)]
    public async Task<ActionResult<AccountDto>> Update(UpdateAccountRequest request, CancellationToken cancellationToken) =>
        Ok(await accountService.UpdateAsync(request, cancellationToken));

    /// <summary>204 on success; 400 if the current password is wrong or the new password is too short or long.</summary>
    [HttpPut("password")]
    [Authorize(Roles = AppRoles.AnyRole)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await accountService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
