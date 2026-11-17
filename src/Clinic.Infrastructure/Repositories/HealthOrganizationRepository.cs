using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class HealthOrganizationRepository(ClinicDbContext dbContext)
    : Repository<HealthOrganization>(dbContext), IHealthOrganizationRepository
{
    public override async Task<IReadOnlyList<HealthOrganization>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.OrderBy(o => o.Name).ToListAsync(cancellationToken);

    public Task<bool> HasBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        DbContext.ClinicFacilities.AnyAsync(c => c.OrganizationId == organizationId, cancellationToken);
}
