using Clinic.Domain.Clinics;

namespace Clinic.Domain.Repositories;

public interface IHealthOrganizationRepository : IRepository<HealthOrganization>
{
    /// <summary>Whether the organization still has at least one branch (such an organization cannot be deleted).</summary>
    Task<bool> HasBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
