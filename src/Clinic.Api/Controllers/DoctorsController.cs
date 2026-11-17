using Clinic.Application.Availability;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// Manages a doctor's availability ranges. The project has no authentication yet, so this is
/// not restricted to the doctor / clinic manager roles; that check would be added with auth.
/// </summary>
[ApiController]
[Route("api/doctors/{doctorId:guid}/availability")]
public class DoctorsController(IDoctorAvailabilityService availabilityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AvailabilityDto>>> GetAll(Guid doctorId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await availabilityService.GetAvailabilityAsync(doctorId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<AvailabilityDto>> Add(Guid doctorId, AddAvailabilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var slot = await availabilityService.AddAvailabilityAsync(doctorId, request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), new { doctorId }, slot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{slotId:guid}")]
    public async Task<IActionResult> Remove(Guid doctorId, Guid slotId, CancellationToken cancellationToken)
    {
        try
        {
            await availabilityService.RemoveAvailabilityAsync(doctorId, slotId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
