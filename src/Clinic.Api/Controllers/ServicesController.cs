using Clinic.Application.Management;
using Clinic.Application.Slots;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// One service (view, edit, delete) and its diagnostic slots. Services are created under a department
/// (<c>POST api/departments/{id}/services</c>). Errors are mapped centrally by <c>ApiExceptionHandler</c>.
/// </summary>
[ApiController]
[Route("api/services")]
public class ServicesController(IServiceCatalogService serviceCatalog, ISlotService slotService) : ControllerBase
{
    [HttpGet("{serviceId:guid}")]
    public async Task<ActionResult<ServiceDto>> Get(Guid serviceId, CancellationToken cancellationToken) =>
        Ok(await serviceCatalog.GetAsync(serviceId, cancellationToken));

    /// <summary>409 if the type is changed while appointments exist for the service.</summary>
    [HttpPut("{serviceId:guid}")]
    public async Task<ActionResult<ServiceDto>> Update(Guid serviceId, SaveServiceRequest request, CancellationToken cancellationToken) =>
        Ok(await serviceCatalog.UpdateAsync(serviceId, request, cancellationToken));

    /// <summary>204 on success; 409 while appointments refer to the service or staff offer it.</summary>
    [HttpDelete("{serviceId:guid}")]
    public async Task<IActionResult> Delete(Guid serviceId, CancellationToken cancellationToken)
    {
        await serviceCatalog.DeleteAsync(serviceId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// All slots of a Diagnostic service on a date (YYYY-MM-DD, read as Europe/Istanbul local time),
    /// each Available, Booked or Past. Examination services return 400: they are booked through a doctor.
    /// </summary>
    [HttpGet("{serviceId:guid}/slots")]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> GetSlots(Guid serviceId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (date is null)
        {
            return BadRequest("Query parameter 'date' is required (YYYY-MM-DD).");
        }

        return Ok(await slotService.GetDiagnosticSlotsAsync(serviceId, date.Value, cancellationToken));
    }
}
