using Clinic.Domain.Clinics;

namespace Clinic.Domain.Repositories;

public interface IServiceRepository : IRepository<Service>
{
    /// <summary>The services of a department, ordered by name.</summary>
    Task<IReadOnlyList<Service>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>The services with the given ids (ids that do not exist are simply absent from the result).</summary>
    Task<IReadOnlyList<Service>> GetByIdsAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default);
}
