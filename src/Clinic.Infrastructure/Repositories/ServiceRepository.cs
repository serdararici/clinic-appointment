using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class ServiceRepository(ClinicDbContext dbContext)
    : Repository<Service>(dbContext), IServiceRepository
{
    public async Task<IReadOnlyList<Service>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(s => s.DepartmentId == departmentId).OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Service>> GetByIdsAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default) =>
        await DbSet.Where(s => serviceIds.Contains(s.Id)).ToListAsync(cancellationToken);
}
