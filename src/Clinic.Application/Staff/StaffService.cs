using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Staff;

/// <summary>
/// Manages staff (doctors and health technicians) under a department. Field validation, the
/// "services of the same department" rule and the slot-duration rule live in the domain entities;
/// this service loads data and enforces the rules that need other aggregates (upcoming appointments).
/// </summary>
public class StaffService(
    IStaffMemberRepository staffRepository,
    IDepartmentRepository departmentRepository,
    IServiceRepository serviceRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IStaffService
{
    public async Task<StaffDto> CreateDoctorAsync(Guid departmentId, CreateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);

        var doctor = new Doctor(
            request.FullName,
            request.Email,
            request.PhoneNumber ?? string.Empty,
            department,
            request.Specialty,
            request.SlotDurationMinutes ?? StaffMember.DefaultSlotDurationMinutes);

        await staffRepository.AddAsync(doctor, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(doctor);
    }

    public async Task<StaffDto> CreateTechnicianAsync(Guid departmentId, CreateTechnicianRequest request, CancellationToken cancellationToken = default)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);

        var technician = new HealthTechnician(
            request.FullName,
            request.Email,
            request.PhoneNumber ?? string.Empty,
            department,
            request.Title);

        await staffRepository.AddAsync(technician, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(technician);
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> ListActiveDoctorsAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        _ = await LoadDepartmentAsync(departmentId, cancellationToken);

        return (await staffRepository.ListAsync(null, departmentId, cancellationToken))
            .OfType<Doctor>()
            .Where(d => d.IsActive)
            .Select(d => new DoctorSummaryDto(
                d.Id,
                d.FullName,
                d.Specialty,
                d.SlotDurationMinutes,
                d.ServicesProvided.OrderBy(s => s.Name).Select(Management.ServiceDto.From).ToList()))
            .ToList();
    }

    public async Task<StaffDto> GetAsync(Guid staffId, CancellationToken cancellationToken = default) =>
        ToDto(await LoadAsync(staffId, cancellationToken));

    public async Task<IReadOnlyList<StaffDto>> ListAsync(Guid? branchId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        (await staffRepository.ListAsync(branchId, departmentId, cancellationToken)).Select(ToDto).ToList();

    public async Task<StaffDto> UpdateAsync(Guid staffId, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        staff.Rename(request.FullName);
        staff.UpdateContactInfo(request.Email, request.PhoneNumber ?? string.Empty);

        // The role-specific field depends on the concrete type.
        switch (staff)
        {
            case Doctor doctor:
                doctor.ChangeSpecialty(request.Specialty ?? throw new ArgumentException("Specialty is required for a doctor.", nameof(request)));
                break;
            case HealthTechnician technician:
                technician.ChangeTitle(request.Title ?? throw new ArgumentException("Title is required for a technician.", nameof(request)));
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(staff);
    }

    public async Task<StaffDto> SetServicesAsync(Guid staffId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        var wantedIds = serviceIds.Distinct().ToList();
        var services = await serviceRepository.GetByIdsAsync(wantedIds, cancellationToken);

        var missing = wantedIds.Except(services.Select(s => s.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException($"Service(s) not found: {string.Join(", ", missing)}.");
        }

        // Rejects (ArgumentException) any service of another department and then changes nothing.
        staff.SetServices(services);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(staff);
    }

    public async Task<StaffSlotDurationDto> SetSlotDurationAsync(Guid staffId, int slotDurationMinutes, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        // Throws ArgumentOutOfRangeException for a length that is not allowed.
        staff.SetSlotDuration(slotDurationMinutes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StaffSlotDurationDto(staff.Id, staff.SlotDurationMinutes);
    }

    public async Task<StaffDto> DeactivateAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        if (staff.IsActive)
        {
            await EnsureNoUpcomingAppointmentsAsync(staff, "deactivated", cancellationToken);

            staff.Deactivate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToDto(staff);
    }

    public async Task<StaffDto> ActivateAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        if (!staff.IsActive)
        {
            staff.Activate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToDto(staff);
    }

    public async Task DeleteAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);

        await EnsureNoUpcomingAppointmentsAsync(staff, "deleted", cancellationToken);

        staffRepository.Remove(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // "Upcoming" = an active (pending/confirmed) appointment that has not finished yet, so one that is
    // in progress also blocks. Cancelled, rejected and finished appointments do not.
    private async Task EnsureNoUpcomingAppointmentsAsync(StaffMember staff, string action, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (await appointmentRepository.HasUpcomingAsync(staff.Id, now, cancellationToken))
        {
            throw new InvalidOperationException($"The staff member has upcoming appointments and cannot be {action}. Cancel or reassign them first.");
        }
    }

    private async Task<StaffMember> LoadAsync(Guid staffId, CancellationToken cancellationToken) =>
        await staffRepository.GetByIdAsync(staffId, cancellationToken)
        ?? throw new KeyNotFoundException($"Staff member {staffId} was not found.");

    private async Task<Clinic.Domain.Clinics.Department> LoadDepartmentAsync(Guid departmentId, CancellationToken cancellationToken) =>
        await departmentRepository.GetByIdAsync(departmentId, cancellationToken)
        ?? throw new KeyNotFoundException($"Department {departmentId} was not found.");

    private static StaffDto ToDto(StaffMember staff) => new(
        staff.Id,
        staff.RoleName,
        staff.FullName,
        staff.Email,
        staff.PhoneNumber,
        staff.ClinicFacilityId,
        staff.DepartmentId,
        staff.SlotDurationMinutes,
        staff.IsActive,
        (staff as Doctor)?.Specialty,
        (staff as HealthTechnician)?.Title,
        staff.ServicesProvided.Select(s => s.Id).ToList());
}
