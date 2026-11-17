using Clinic.Application.Staff;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Staff (doctors and technicians): view, list, edit, offered services, slot duration, deactivate and
/// delete. Staff are created under a department (<c>POST api/departments/{id}/doctors</c> or
/// <c>/technicians</c>). Errors are mapped centrally by <c>ApiExceptionHandler</c>. No authorization yet.
/// </summary>
[ApiController]
[Route("api/staff")]
public class StaffController(IStaffService staffService) : ControllerBase
{
    /// <summary>Staff ordered by name; filter with <c>?branchId=</c> and/or <c>?departmentId=</c> (inactive staff included).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffDto>>> List([FromQuery] Guid? branchId, [FromQuery] Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await staffService.ListAsync(branchId, departmentId, cancellationToken));

    [HttpGet("{staffId:guid}")]
    public async Task<ActionResult<StaffDto>> Get(Guid staffId, CancellationToken cancellationToken) =>
        Ok(await staffService.GetAsync(staffId, cancellationToken));

    [HttpPut("{staffId:guid}")]
    public async Task<ActionResult<StaffDto>> Update(Guid staffId, UpdateStaffRequest request, CancellationToken cancellationToken) =>
        Ok(await staffService.UpdateAsync(staffId, request, cancellationToken));

    /// <summary>Replaces the offered services; 400 if any service belongs to another department, 404 if one does not exist.</summary>
    [HttpPut("{staffId:guid}/services")]
    public async Task<ActionResult<StaffDto>> SetServices(Guid staffId, SetStaffServicesRequest request, CancellationToken cancellationToken) =>
        Ok(await staffService.SetServicesAsync(staffId, request.ServiceIds, cancellationToken));

    [HttpPut("{staffId:guid}/slot-duration")]
    public async Task<ActionResult<StaffSlotDurationDto>> SetSlotDuration(Guid staffId, SetSlotDurationRequest request, CancellationToken cancellationToken) =>
        Ok(await staffService.SetSlotDurationAsync(staffId, request.SlotDurationMinutes, cancellationToken));

    /// <summary>409 while the staff member has upcoming non-cancelled appointments.</summary>
    [HttpPost("{staffId:guid}/deactivate")]
    public async Task<ActionResult<StaffDto>> Deactivate(Guid staffId, CancellationToken cancellationToken) =>
        Ok(await staffService.DeactivateAsync(staffId, cancellationToken));

    [HttpPost("{staffId:guid}/activate")]
    public async Task<ActionResult<StaffDto>> Activate(Guid staffId, CancellationToken cancellationToken) =>
        Ok(await staffService.ActivateAsync(staffId, cancellationToken));

    /// <summary>204 on success; 409 while the staff member has upcoming non-cancelled appointments.</summary>
    [HttpDelete("{staffId:guid}")]
    public async Task<IActionResult> Delete(Guid staffId, CancellationToken cancellationToken)
    {
        await staffService.DeleteAsync(staffId, cancellationToken);
        return NoContent();
    }
}
