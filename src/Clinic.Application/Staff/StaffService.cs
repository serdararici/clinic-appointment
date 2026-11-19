using Clinic.Application.Auth;
using Clinic.Application.Availability;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
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
    IUserRepository userRepository,
    IPasswordHashing passwordHashing,
    IDefaultScheduleService defaultSchedule,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    AccessGuard guard) : IStaffService
{
    public async Task<StaffDto> CreateDoctorAsync(Guid departmentId, CreateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);
        await guard.EnsureDepartmentAsync(department, cancellationToken);

        // The doctor gets a login: a valid, unused email and a temporary password (same rules as registration).
        var email = AuthService.ValidateEmail(request.Email);
        AuthService.ValidatePassword(request.Password);
        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var doctor = new Doctor(
            request.FullName,
            email,
            request.PhoneNumber ?? string.Empty,
            department,
            request.SlotDurationMinutes ?? StaffMember.DefaultSlotDurationMinutes);
        doctor.SetPasswordHash(passwordHashing.Hash(request.Password));

        // The services the doctor offers: the ones given (at least one), otherwise every Examination service of the department,
        // so patients can book the new doctor right away. They are saved together with the doctor.
        doctor.SetServices(await ChooseServicesAsync(department, doctor, request.ServiceIds, atLeastOne: true, cancellationToken));

        await staffRepository.AddAsync(doctor, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ... and the default work schedule (weekdays 09:00-12:00 and 13:00-16:00 for the next 14 days) is filled in
        // at once, so the new doctor is bookable immediately instead of at the next scheduled run.
        await defaultSchedule.EnsureDefaultAvailabilityAsync(doctor.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return ToDto(doctor);
    }

    public async Task<StaffDto> CreateTechnicianAsync(Guid departmentId, CreateTechnicianRequest request, CancellationToken cancellationToken = default)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);
        await guard.EnsureDepartmentAsync(department, cancellationToken);

        // A technician has no login, but the email is still the person's unique identity.
        var email = AuthService.ValidateEmail(request.Email);
        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var technician = new HealthTechnician(
            request.FullName,
            email,
            request.PhoneNumber ?? string.Empty,
            department,
            request.Title ?? HealthTechnician.DefaultTitle);

        // The diagnostic services the technician is linked to (a definition only): the ones given (none is fine), otherwise
        // every Diagnostic service of the department. Saved together with the technician.
        technician.SetServices(await ChooseServicesAsync(department, technician, request.ServiceIds, atLeastOne: false, cancellationToken));

        await staffRepository.AddAsync(technician, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(technician);
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> ListActiveDoctorsAsync(Guid departmentId, Guid? serviceId = null, CancellationToken cancellationToken = default)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);

        return (await staffRepository.ListAsync(null, departmentId, cancellationToken))
            .OfType<Doctor>()
            .Where(d => d.IsActive && (serviceId is null || d.OffersService(serviceId.Value)))
            .Select(d => new DoctorSummaryDto(
                d.Id,
                d.FullName,
                department.Name,
                d.SlotDurationMinutes,
                d.ServicesProvided.OrderBy(s => s.Name).Select(Management.ServiceDto.From).ToList()))
            .ToList();
    }

    public async Task<DoctorProfileDto> GetDoctorProfileAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId);

        var doctor = await LoadAsync(doctorId, cancellationToken) as Doctor
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");
        var department = await LoadDepartmentAsync(doctor.DepartmentId, cancellationToken);

        return new DoctorProfileDto(doctor.Id, doctor.FullName, department.Id, department.Name, doctor.SlotDurationMinutes, doctor.AutoConfirmAppointments);
    }

    public async Task<DoctorProfileDto> SetAutoConfirmAsync(Guid doctorId, bool enabled, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId);

        var doctor = await LoadAsync(doctorId, cancellationToken) as Doctor
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

        // Only future bookings are affected: appointments that are already Pending stay Pending.
        doctor.SetAutoConfirm(enabled);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetDoctorProfileAsync(doctorId, cancellationToken);
    }

    public async Task<StaffDto> GetAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        await guard.EnsureStaffAsync(staff, cancellationToken);

        return ToDto(staff);
    }

    public async Task<IReadOnlyList<StaffDto>> ListAsync(Guid? branchId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        if (guard.IsSystemAdmin)
        {
            return (await staffRepository.ListAsync(branchId, departmentId, cancellationToken)).Select(ToDto).ToList();
        }

        // A clinic manager only ever sees their own organization's staff: a given department or branch must be
        // theirs (403 otherwise), and with no filter the list is limited to the branches of their organization.
        if (departmentId is { } filterDepartmentId)
        {
            var department = await LoadDepartmentAsync(filterDepartmentId, cancellationToken);
            await guard.EnsureDepartmentAsync(department, cancellationToken);
            return (await staffRepository.ListAsync(branchId, filterDepartmentId, cancellationToken)).Select(ToDto).ToList();
        }

        if (branchId is { } filterBranchId)
        {
            await guard.EnsureBranchAsync(filterBranchId, cancellationToken);
            return (await staffRepository.ListAsync(filterBranchId, null, cancellationToken)).Select(ToDto).ToList();
        }

        var own = new List<StaffMember>();
        foreach (var ownBranchId in await guard.OwnBranchIdsAsync(cancellationToken))
        {
            own.AddRange(await staffRepository.ListAsync(ownBranchId, null, cancellationToken));
        }

        return own.OrderBy(s => s.FullName).Select(ToDto).ToList();
    }

    public async Task<StaffDto> UpdateAsync(Guid staffId, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        await guard.EnsureStaffAsync(staff, cancellationToken);

        // A changed email must not belong to someone else (it is the login name and unique).
        var newEmail = request.Email?.Trim() ?? string.Empty;
        if (newEmail.Length > 0
            && !string.Equals(staff.Email, newEmail, StringComparison.OrdinalIgnoreCase)
            && await userRepository.EmailExistsAsync(newEmail, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        staff.Rename(request.FullName);
        staff.UpdateContactInfo(request.Email ?? string.Empty, request.PhoneNumber ?? string.Empty);

        // The role-specific field depends on the concrete type.
        switch (staff)
        {
            case HealthTechnician technician:
                if (request.Title is not null)
                {
                    technician.ChangeTitle(request.Title);
                }

                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(staff);
    }

    public async Task<StaffDto> SetServicesAsync(Guid staffId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        await guard.EnsureStaffAsync(staff, cancellationToken);

        var wantedIds = serviceIds.Distinct().ToList();
        var services = await serviceRepository.GetByIdsAsync(wantedIds, cancellationToken);

        var missing = wantedIds.Except(services.Select(s => s.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException($"Service(s) not found: {string.Join(", ", missing)}.");
        }

        // Rejects (ArgumentException) any service of another department or of the wrong kind (a doctor: Examination,
        // a technician: Diagnostic) and then changes nothing.
        staff.SetServices(services);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(staff);
    }

    public async Task<StaffSlotDurationDto> SetSlotDurationAsync(Guid staffId, int slotDurationMinutes, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        await guard.EnsureCanChangeSlotDurationAsync(staff, cancellationToken);

        // Throws ArgumentOutOfRangeException for a length that is not allowed.
        staff.SetSlotDuration(slotDurationMinutes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StaffSlotDurationDto(staff.Id, staff.SlotDurationMinutes);
    }

    public async Task<StaffDto> DeactivateAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        await guard.EnsureStaffAsync(staff, cancellationToken);

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
        await guard.EnsureStaffAsync(staff, cancellationToken);

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
        await guard.EnsureStaffAsync(staff, cancellationToken);

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

    private async Task<IReadOnlyList<Service>> ChooseServicesAsync(
        Department department, StaffMember staff, IReadOnlyList<Guid>? requestedIds, bool atLeastOne, CancellationToken cancellationToken)
    {
        if (requestedIds is null)
        {
            return (await serviceRepository.GetByDepartmentIdAsync(department.Id, cancellationToken))
                .Where(service => service.ServiceType == staff.OfferedServiceType)
                .ToList();
        }

        var wantedIds = requestedIds.Distinct().ToList();
        if (atLeastOne && wantedIds.Count == 0)
        {
            throw new ArgumentException("A doctor must offer at least one service.", nameof(requestedIds));
        }

        var services = await serviceRepository.GetByIdsAsync(wantedIds, cancellationToken);
        var missing = wantedIds.Except(services.Select(service => service.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException($"Service(s) not found: {string.Join(", ", missing)}.");
        }

        return services; // the department and the kind are checked by SetServices
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
        (staff as HealthTechnician)?.Title,
        staff.ServicesProvided.Select(s => s.Id).ToList());
}
