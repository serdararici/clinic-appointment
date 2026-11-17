using Clinic.Application.Staff;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Staff settings. The project has no authentication yet, so this is not restricted to
/// clinic managers; that check would be added with auth.
/// </summary>
[ApiController]
[Route("api/staff")]
public class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpPut("{staffId:guid}/slot-duration")]
    public async Task<ActionResult<StaffSlotDurationDto>> SetSlotDuration(Guid staffId, SetSlotDurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await staffService.SetSlotDurationAsync(staffId, request.SlotDurationMinutes, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
