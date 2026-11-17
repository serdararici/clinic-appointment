using Clinic.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Clinic.Application.Management;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Organizations and their branches. Errors (404 unknown id, 400 invalid input, 409 rule or dependent
/// data) are mapped centrally by <c>ApiExceptionHandler</c>. Reads are public; changes need the ClinicManager role.
/// </summary>
[ApiController]
[Route("api/organizations")]
public class OrganizationsController(IOrganizationService organizationService, IBranchService branchService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> List(CancellationToken cancellationToken) =>
        Ok(await organizationService.ListAsync(cancellationToken));

    [HttpGet("{organizationId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrganizationDto>> Get(Guid organizationId, CancellationToken cancellationToken) =>
        Ok(await organizationService.GetAsync(organizationId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.ClinicManager)]
    public async Task<ActionResult<OrganizationDto>> Create(SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await organizationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { organizationId = organization.Id }, organization);
    }

    [HttpPut("{organizationId:guid}")]
    [Authorize(Roles = AppRoles.ClinicManager)]
    public async Task<ActionResult<OrganizationDto>> Update(Guid organizationId, SaveOrganizationRequest request, CancellationToken cancellationToken) =>
        Ok(await organizationService.UpdateAsync(organizationId, request, cancellationToken));

    /// <summary>204 on success; 409 while the organization still has branches.</summary>
    [HttpDelete("{organizationId:guid}")]
    [Authorize(Roles = AppRoles.ClinicManager)]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken cancellationToken)
    {
        await organizationService.DeleteAsync(organizationId, cancellationToken);
        return NoContent();
    }

    // ---- Branches of an organization ----

    [HttpGet("{organizationId:guid}/branches")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> ListBranches(Guid organizationId, CancellationToken cancellationToken) =>
        Ok(await branchService.ListByOrganizationAsync(organizationId, cancellationToken));

    /// <summary>Creates a branch; 404 if the organization does not exist (it must be created first).</summary>
    [HttpPost("{organizationId:guid}/branches")]
    [Authorize(Roles = AppRoles.ClinicManager)]
    public async Task<ActionResult<BranchDto>> CreateBranch(Guid organizationId, SaveBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await branchService.CreateAsync(organizationId, request, cancellationToken);
        return CreatedAtAction(nameof(BranchesController.Get), "Branches", new { branchId = branch.Id }, branch);
    }
}
