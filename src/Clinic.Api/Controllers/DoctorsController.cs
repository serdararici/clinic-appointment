using Clinic.Application.Availability;
using Clinic.Application.Slots;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// A doctor's availability ranges and bookable examination slots. The project has no authentication
/// yet, so availability changes are not restricted to the doctor / clinic manager roles; that check
/// would be added with auth.
/// </summary>
[ApiController]
[Route("api/doctors/{doctorId:guid}")]
public class DoctorsController(IDoctorAvailabilityService availabilityService, ISlotService slotService) : ControllerBase
{
    [HttpGet("availability")]
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

    [HttpPost("availability")]
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

    [HttpDelete("availability/{slotId:guid}")]
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

    /// <summary>
    /// All of the doctor's examination slots on a date (YYYY-MM-DD, read as Europe/Istanbul local time),
    /// each Available, Booked or Past.
    /// </summary>
    [HttpGet("slots")]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> GetSlots(Guid doctorId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (date is null)
        {
            return BadRequest("Query parameter 'date' is required (YYYY-MM-DD).");
        }

        try
        {
            return Ok(await slotService.GetExaminationSlotsAsync(doctorId, date.Value, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
