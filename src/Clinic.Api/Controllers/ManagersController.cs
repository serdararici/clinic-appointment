using Clinic.Api.Security;
using Clinic.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Clinic manager accounts, for the system administrator only (they are created with
/// <c>POST api/branches/{id}/managers</c>). Errors are mapped centrally by <c>ApiExceptionHandler</c>.
/// </summary>
[ApiController]
[Route("api/managers")]
public class ManagersController(IManagerService managerService) : ControllerBase
{
    /// <summary>Every clinic manager with their branch and organization, ordered by name (inactive ones included).</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<IReadOnlyList<ManagerDto>>> List(CancellationToken cancellationToken) =>
        Ok(await managerService.ListAsync(cancellationToken));

    [HttpPut("{managerId:guid}")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<ManagerDto>> Update(Guid managerId, UpdateManagerRequest request, CancellationToken cancellationToken) =>
        Ok(await managerService.UpdateAsync(managerId, request, cancellationToken));

    /// <summary>The manager can no longer log in; their account stays.</summary>
    [HttpPost("{managerId:guid}/deactivate")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<ManagerDto>> Deactivate(Guid managerId, CancellationToken cancellationToken) =>
        Ok(await managerService.DeactivateAsync(managerId, cancellationToken));

    [HttpPost("{managerId:guid}/activate")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<ManagerDto>> Activate(Guid managerId, CancellationToken cancellationToken) =>
        Ok(await managerService.ActivateAsync(managerId, cancellationToken));
}
