namespace Clinic.Application.Staff;

public interface IStaffService
{
    Task<StaffDto> CreateDoctorAsync(Guid departmentId, CreateDoctorRequest request, CancellationToken cancellationToken = default);

    Task<StaffDto> CreateTechnicianAsync(Guid departmentId, CreateTechnicianRequest request, CancellationToken cancellationToken = default);

    /// <summary>The department's active doctors (no technicians, no deactivated staff) with their offered services.</summary>
    Task<IReadOnlyList<DoctorSummaryDto>> ListActiveDoctorsAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>The logged-in doctor's own name and department (403 for anyone else's id).</summary>
    Task<DoctorProfileDto> GetDoctorProfileAsync(Guid doctorId, CancellationToken cancellationToken = default);

    /// <summary>The doctor switches their own appointment approval: on = new appointments are confirmed at once, off = they wait as Pending.</summary>
    Task<DoctorProfileDto> SetAutoConfirmAsync(Guid doctorId, bool enabled, CancellationToken cancellationToken = default);

    Task<StaffDto> GetAsync(Guid staffId, CancellationToken cancellationToken = default);

    /// <summary>Staff of a branch and/or department (null means any), ordered by name; inactive staff are included.</summary>
    Task<IReadOnlyList<StaffDto>> ListAsync(Guid? branchId, Guid? departmentId, CancellationToken cancellationToken = default);

    Task<StaffDto> UpdateAsync(Guid staffId, UpdateStaffRequest request, CancellationToken cancellationToken = default);

    /// <summary>Replaces the offered services; every service must belong to the staff member's own department.</summary>
    Task<StaffDto> SetServicesAsync(Guid staffId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default);

    Task<StaffSlotDurationDto> SetSlotDurationAsync(Guid staffId, int slotDurationMinutes, CancellationToken cancellationToken = default);

    /// <summary>Deactivates the staff member; refused while they have upcoming active appointments.</summary>
    Task<StaffDto> DeactivateAsync(Guid staffId, CancellationToken cancellationToken = default);

    Task<StaffDto> ActivateAsync(Guid staffId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the staff member; refused while they have upcoming active appointments.</summary>
    Task DeleteAsync(Guid staffId, CancellationToken cancellationToken = default);
}
