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

    public async Task<IReadOnlyList<ClinicFacility>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(c => c.OrganizationId == organizationId).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<bool> HasManagersAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default) =>
        DbContext.ClinicManagers.AnyAsync(m => m.ClinicFacilityId == clinicFacilityId, cancellationToken);

    public void AddWorkingHour(WorkingHour workingHour) => DbContext.Set<WorkingHour>().Add(workingHour);

    public void RemoveWorkingHour(WorkingHour workingHour) => DbContext.Set<WorkingHour>().Remove(workingHour);

    public Task<ClinicFacility?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(
                c => c.Departments.Any(d => d.Services.Any(s => s.Id == serviceId)),
                cancellationToken);
}
