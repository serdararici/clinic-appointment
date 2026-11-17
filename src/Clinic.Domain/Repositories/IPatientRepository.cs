using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>The patients with the given ids (ids that do not exist are simply absent).</summary>
    Task<IReadOnlyList<Patient>> GetByIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken = default);
}
