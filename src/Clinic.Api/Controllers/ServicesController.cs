using Clinic.Application.Slots;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

[ApiController]
[Route("api/services/{serviceId:guid}")]
public class ServicesController(ISlotService slotService) : ControllerBase
{
    /// <summary>
    /// All slots of a Diagnostic service on a date (YYYY-MM-DD, read as Europe/Istanbul local time),
    /// each Available, Booked or Past. Examination services return 400: they are booked through a doctor.
    /// </summary>
    [HttpGet("slots")]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> GetSlots(Guid serviceId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (date is null)
        {
            return BadRequest("Query parameter 'date' is required (YYYY-MM-DD).");
        }

        try
        {
            return Ok(await slotService.GetDiagnosticSlotsAsync(serviceId, date.Value, cancellationToken));
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
