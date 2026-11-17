using Clinic.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Clinic.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Registration and login: the only write endpoints open to anyone.
/// Errors are mapped centrally by <c>ApiExceptionHandler</c>: 400 invalid input, 409 email taken, 401 failed login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Creates a patient account (password of at least 8 characters, unique email).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthUserDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        return Created($"api/patients/{user.Id}", user);
    }

    /// <summary>Returns a JWT valid for 8 hours; 401 for a wrong password or unknown email.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));
}
