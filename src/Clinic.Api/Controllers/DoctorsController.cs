using Clinic.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Clinic.Application.Appointments;
using Clinic.Application.Availability;
using Clinic.Application.Slots;
using Clinic.Application.Staff;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// A doctor's availability ranges, appointments and bookable examination slots. Slots are public; everything
/// else needs the Doctor role and is limited to the doctor's own data.
/// </summary>
[ApiController]
[Route("api/doctors/{doctorId:guid}")]
public class DoctorsController(
    IDoctorAvailabilityService availabilityService,
    ISlotService slotService,
    IAppointmentService appointmentService,
    IStaffService staffService,
    IAutoScheduleService autoScheduleService) : ControllerBase
{
    /// <summary>
    /// The doctor switches the approval of their appointments (own id only). On: new appointments are confirmed at once.
    /// Off: they wait as Pending for the doctor. Appointments that are already Pending are not touched.
    /// </summary>
    [HttpPut("auto-confirm")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<DoctorProfileDto>> SetAutoConfirm(Guid doctorId, SetAutoConfirmRequest request, CancellationToken cancellationToken) =>
        Ok(await staffService.SetAutoConfirmAsync(doctorId, request.Enabled, cancellationToken));

    /// <summary>Whether the doctor's automatic work schedule is on.</summary>
    [HttpGet("auto-schedule")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<AutoScheduleDto>> GetAutoSchedule(Guid doctorId, CancellationToken cancellationToken) =>
        Ok(await autoScheduleService.GetAsync(doctorId, cancellationToken));

    /// <summary>Switches the doctor's automatic work schedule on or off (on fills the booking window right away).</summary>
    [HttpPut("auto-schedule")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<AutoScheduleDto>> SetAutoSchedule(Guid doctorId, SetAutoScheduleRequest request, CancellationToken cancellationToken) =>
        Ok(await autoScheduleService.SetAsync(doctorId, request.Enabled, cancellationToken));

    /// <summary>The doctor's own name and department (for the doctor's home page).</summary>
    [HttpGet("profile")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<DoctorProfileDto>> GetProfile(Guid doctorId, CancellationToken cancellationToken) =>
        Ok(await staffService.GetDoctorProfileAsync(doctorId, cancellationToken));

    [HttpGet("availability")]
    [Authorize(Roles = AppRoles.Doctor)]
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
    [Authorize(Roles = AppRoles.Doctor)]
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

    /// <summary>
    /// Creates availability over a date range in Turkey local time: one range per matching weekday, skipping
    /// days that overlap an existing range or have already started. 200 with the created ranges, the skipped
    /// dates and the past dates; 400 if the request is invalid.
    /// </summary>
    [HttpPost("availability/bulk")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<BulkAvailabilityResultDto>> AddBulk(Guid doctorId, BulkAvailabilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await availabilityService.AddBulkAvailabilityAsync(doctorId, request, cancellationToken));
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

    /// <summary>The doctor's appointments in every status, ordered by start time.</summary>
    [HttpGet("appointments")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDetailDto>>> GetAppointments(Guid doctorId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await appointmentService.GetByDoctorAsync(doctorId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("availability/{slotId:guid}")]
    [Authorize(Roles = AppRoles.Doctor)]
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
    [AllowAnonymous]
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
