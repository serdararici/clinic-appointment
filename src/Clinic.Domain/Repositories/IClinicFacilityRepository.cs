using Clinic.Domain.Clinics;

namespace Clinic.Domain.Repositories;

public interface IClinicFacilityRepository : IRepository<ClinicFacility>
{
    /// <summary>The branch (with its working hours) that owns the service, via the service's department.</summary>
    Task<ClinicFacility?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>The branches of an organization, ordered by name.</summary>
    Task<IReadOnlyList<ClinicFacility>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Whether a clinic manager is still assigned to the branch (such a branch cannot be deleted).</summary>
    Task<bool> HasManagersAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a range created via <see cref="ClinicFacility.AddWorkingHour"/> as new. Needed because ranges
    /// get their Guid id client-side, so EF cannot tell on its own that they are not yet in the database.
    /// </summary>
    void AddWorkingHour(WorkingHour workingHour);

    /// <summary>Marks a range removed via <see cref="ClinicFacility.RemoveWorkingHour"/> for deletion.</summary>
    void RemoveWorkingHour(WorkingHour workingHour);
}
