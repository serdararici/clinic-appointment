using Clinic.Application.Management;
using Clinic.Application.Staff;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

/// <summary>
/// A department, the services it offers, and registering staff under it. Errors are mapped centrally
/// by <c>ApiExceptionHandler</c>. No authorization yet.
/// </summary>
[ApiController]
[Route("api/departments")]
public class DepartmentsController(
    IDepartmentService departmentService,
    IServiceCatalogService serviceCatalog,
    IStaffService staffService) : ControllerBase
{
    [HttpGet("{departmentId:guid}")]
    public async Task<ActionResult<DepartmentDto>> Get(Guid departmentId, CancellationToken cancellationToken) =>
        Ok(await departmentService.GetAsync(departmentId, cancellationToken));

    [HttpPut("{departmentId:guid}")]
    public async Task<ActionResult<DepartmentDto>> Update(Guid departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken) =>
        Ok(await departmentService.UpdateAsync(departmentId, request, cancellationToken));

    /// <summary>204 on success; 409 while the department still has services or staff.</summary>
    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> Delete(Guid departmentId, CancellationToken cancellationToken)
    {
        await departmentService.DeleteAsync(departmentId, cancellationToken);
        return NoContent();
    }

    // ---- Services of a department ----

    [HttpGet("{departmentId:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> ListServices(Guid departmentId, CancellationToken cancellationToken) =>
        Ok(await serviceCatalog.ListByDepartmentAsync(departmentId, cancellationToken));

    [HttpPost("{departmentId:guid}/services")]
    public async Task<ActionResult<ServiceDto>> CreateService(Guid departmentId, SaveServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await serviceCatalog.CreateAsync(departmentId, request, cancellationToken);
        return CreatedAtAction(nameof(ServicesController.Get), "Services", new { serviceId = service.Id }, service);
    }

    // ---- Staff registered under the department ----

    [HttpPost("{departmentId:guid}/doctors")]
    public async Task<ActionResult<StaffDto>> CreateDoctor(Guid departmentId, CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        var doctor = await staffService.CreateDoctorAsync(departmentId, request, cancellationToken);
        return CreatedAtAction(nameof(StaffController.Get), "Staff", new { staffId = doctor.Id }, doctor);
    }

    [HttpPost("{departmentId:guid}/technicians")]
    public async Task<ActionResult<StaffDto>> CreateTechnician(Guid departmentId, CreateTechnicianRequest request, CancellationToken cancellationToken)
    {
        var technician = await staffService.CreateTechnicianAsync(departmentId, request, cancellationToken);
        return CreatedAtAction(nameof(StaffController.Get), "Staff", new { staffId = technician.Id }, technician);
    }
}
