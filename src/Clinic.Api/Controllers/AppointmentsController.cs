using Clinic.Application.Appointments;
using Clinic.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await appointmentService.CreateAppointmentAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Create), new { id = appointment.Id }, appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ServiceNotOfferedException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (StaffInactiveException ex)
        {
            return Conflict(ex.Message);
        }
        catch (PastBookingException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (SlotNotAlignedException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (OutsideWorkingHoursException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ServiceSlotConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (NotAvailableException ex)
        {
            return Conflict(ex.Message);
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.ConfirmAppointmentAsync(id, cancellationToken);
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

    /// <summary>Cancels a pending or confirmed appointment; 409 if it is already rejected or cancelled.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await appointmentService.CancelAppointmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.RejectAppointmentAsync(id, cancellationToken);
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
