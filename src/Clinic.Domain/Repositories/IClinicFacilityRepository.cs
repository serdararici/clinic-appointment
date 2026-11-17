using Clinic.Domain.Clinics;

namespace Clinic.Domain.Repositories;

public interface IClinicFacilityRepository : IRepository<ClinicFacility>
{
    /// <summary>The branch (with its working hours) that owns the service, via the service's department.</summary>
    Task<ClinicFacility?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
