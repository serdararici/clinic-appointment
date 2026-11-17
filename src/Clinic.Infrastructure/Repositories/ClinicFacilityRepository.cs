using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class ClinicFacilityRepository(ClinicDbContext dbContext)
    : Repository<ClinicFacility>(dbContext), IClinicFacilityRepository
{
    public override Task<ClinicFacility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(c => c.Departments).ThenInclude(d => d.Services)
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<ClinicFacility?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(
                c => c.Departments.Any(d => d.Services.Any(s => s.Id == serviceId)),
                cancellationToken);
}
