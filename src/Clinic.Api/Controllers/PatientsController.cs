using Clinic.Application.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>A patient's appointments. Errors are mapped centrally by <c>ApiExceptionHandler</c>. No authorization yet.</summary>
[ApiController]
[Route("api/patients/{patientId:guid}")]
public class PatientsController(IAppointmentService appointmentService) : ControllerBase
{
    /// <summary>The patient's appointments in every status, ordered by start time.</summary>
    [HttpGet("appointments")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDetailDto>>> GetAppointments(Guid patientId, CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetByPatientAsync(patientId, cancellationToken));
}
