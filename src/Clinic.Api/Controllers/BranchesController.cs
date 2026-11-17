using Clinic.Application.Auth;
using Clinic.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Clinic.Application.Management;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// A branch, its weekly working hours and its departments. Errors are mapped centrally by
/// <c>ApiExceptionHandler</c>. Reads are public; changes need the ClinicManager role (own organization only) or SystemAdmin.
/// </summary>
[ApiController]
[Route("api/branches")]
public class BranchesController(IBranchService branchService, IDepartmentService departmentService, IManagerService managerService) : ControllerBase
{
    /// <summary>Every branch with its organization's name.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<BranchSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await branchService.ListAllAsync(cancellationToken));

    [HttpGet("{branchId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BranchDto>> Get(Guid branchId, CancellationToken cancellationToken) =>
        Ok(await branchService.GetAsync(branchId, cancellationToken));

    [HttpPut("{branchId:guid}")]
    [Authorize(Roles = AppRoles.ClinicManagerOrAdmin)]
    public async Task<ActionResult<BranchDto>> Update(Guid branchId, SaveBranchRequest request, CancellationToken cancellationToken) =>
        Ok(await branchService.UpdateAsync(branchId, request, cancellationToken));

    /// <summary>204 on success; 409 while the branch still has departments or managers.</summary>
    [HttpDelete("{branchId:guid}")]
    [Authorize(Roles = AppRoles.ClinicManagerOrAdmin)]
    public async Task<IActionResult> Delete(Guid branchId, CancellationToken cancellationToken)
    {
        await branchService.DeleteAsync(branchId, cancellationToken);
        return NoContent();
    }

    // ---- Working hours (clinic local time) ----

    /// <summary>Working hours ordered Monday to Sunday; add <c>?dayOfWeek=Monday</c> to list one weekday.</summary>
    [HttpGet("{branchId:guid}/working-hours")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<WorkingHourDto>>> GetWorkingHours(Guid branchId, [FromQuery] DayOfWeek? dayOfWeek, CancellationToken cancellationToken) =>
        Ok(await branchService.GetWorkingHoursAsync(branchId, dayOfWeek, cancellationToken));

    /// <summary>201 on success; 400 if closing is not after opening; 409 if it overlaps another range that weekday.</summary>
    [HttpPost("{branchId:guid}/working-hours")]
    [Authorize(Roles = AppRoles.ClinicManagerOrAdmin)]
    public async Task<ActionResult<WorkingHourDto>> AddWorkingHour(Guid branchId, AddWorkingHourRequest request, CancellationToken cancellationToken)
    {
        var workingHour = await branchService.AddWorkingHourAsync(branchId, request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkingHours), new { branchId, dayOfWeek = workingHour.DayOfWeek }, workingHour);
    }

    [HttpDelete("{branchId:guid}/working-hours/{workingHourId:guid}")]
    [Authorize(Roles = AppRoles.ClinicManagerOrAdmin)]
    public async Task<IActionResult> RemoveWorkingHour(Guid branchId, Guid workingHourId, CancellationToken cancellationToken)
    {
        await branchService.RemoveWorkingHourAsync(branchId, workingHourId, cancellationToken);
        return NoContent();
    }

    // ---- Manager accounts ----

    /// <summary>
    /// Creates a ClinicManager account for the branch with an initial password. SystemAdmin only; 404 unknown
    /// branch, 400 invalid input or short password, 409 email already used.
    /// </summary>
    [HttpPost("{branchId:guid}/managers")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    public async Task<ActionResult<AuthUserDto>> CreateManager(Guid branchId, CreateManagerRequest request, CancellationToken cancellationToken)
    {
        var manager = await managerService.CreateAsync(branchId, request, cancellationToken);
        return Created($"api/branches/{branchId}/managers/{manager.Id}", manager);
    }

    // ---- Departments of a branch ----

    [HttpGet("{branchId:guid}/departments")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> ListDepartments(Guid branchId, CancellationToken cancellationToken) =>
        Ok(await departmentService.ListByBranchAsync(branchId, cancellationToken));

    [HttpPost("{branchId:guid}/departments")]
    [Authorize(Roles = AppRoles.ClinicManagerOrAdmin)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(Guid branchId, SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await departmentService.CreateAsync(branchId, request, cancellationToken);
        return CreatedAtAction(nameof(DepartmentsController.Get), "Departments", new { departmentId = department.Id }, department);
    }
}
