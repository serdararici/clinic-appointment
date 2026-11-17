using Clinic.Domain.Clinics;

namespace Clinic.Domain.Repositories;

public interface IDepartmentRepository : IRepository<Department>
{
    /// <summary>The departments of a branch, ordered by name.</summary>
    Task<IReadOnlyList<Department>> GetByClinicFacilityIdAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default);

    /// <summary>Whether the department still has services (such a department cannot be deleted).</summary>
    Task<bool> HasServicesAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>Whether any staff member (active or not) still belongs to the department.</summary>
    Task<bool> HasStaffAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
