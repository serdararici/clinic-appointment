using Clinic.Application.Management;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// A branch, its weekly working hours and its departments. Errors are mapped centrally by
/// <c>ApiExceptionHandler</c>. No authorization yet.
/// </summary>
[ApiController]
[Route("api/branches")]
public class BranchesController(IBranchService branchService, IDepartmentService departmentService) : ControllerBase
{
    [HttpGet("{branchId:guid}")]
    public async Task<ActionResult<BranchDto>> Get(Guid branchId, CancellationToken cancellationToken) =>
        Ok(await branchService.GetAsync(branchId, cancellationToken));

    [HttpPut("{branchId:guid}")]
    public async Task<ActionResult<BranchDto>> Update(Guid branchId, SaveBranchRequest request, CancellationToken cancellationToken) =>
        Ok(await branchService.UpdateAsync(branchId, request, cancellationToken));

    /// <summary>204 on success; 409 while the branch still has departments or managers.</summary>
    [HttpDelete("{branchId:guid}")]
    public async Task<IActionResult> Delete(Guid branchId, CancellationToken cancellationToken)
    {
        await branchService.DeleteAsync(branchId, cancellationToken);
        return NoContent();
    }

    // ---- Working hours (clinic local time) ----

    /// <summary>Working hours ordered Monday to Sunday; add <c>?dayOfWeek=Monday</c> to list one weekday.</summary>
    [HttpGet("{branchId:guid}/working-hours")]
    public async Task<ActionResult<IReadOnlyList<WorkingHourDto>>> GetWorkingHours(Guid branchId, [FromQuery] DayOfWeek? dayOfWeek, CancellationToken cancellationToken) =>
        Ok(await branchService.GetWorkingHoursAsync(branchId, dayOfWeek, cancellationToken));

    /// <summary>201 on success; 400 if closing is not after opening; 409 if it overlaps another range that weekday.</summary>
    [HttpPost("{branchId:guid}/working-hours")]
    public async Task<ActionResult<WorkingHourDto>> AddWorkingHour(Guid branchId, AddWorkingHourRequest request, CancellationToken cancellationToken)
    {
        var workingHour = await branchService.AddWorkingHourAsync(branchId, request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkingHours), new { branchId, dayOfWeek = workingHour.DayOfWeek }, workingHour);
    }

    [HttpDelete("{branchId:guid}/working-hours/{workingHourId:guid}")]
    public async Task<IActionResult> RemoveWorkingHour(Guid branchId, Guid workingHourId, CancellationToken cancellationToken)
    {
        await branchService.RemoveWorkingHourAsync(branchId, workingHourId, cancellationToken);
        return NoContent();
    }

    // ---- Departments of a branch ----

    [HttpGet("{branchId:guid}/departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> ListDepartments(Guid branchId, CancellationToken cancellationToken) =>
        Ok(await departmentService.ListByBranchAsync(branchId, cancellationToken));

    [HttpPost("{branchId:guid}/departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(Guid branchId, SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await departmentService.CreateAsync(branchId, request, cancellationToken);
        return CreatedAtAction(nameof(DepartmentsController.Get), "Departments", new { departmentId = department.Id }, department);
    }
}
