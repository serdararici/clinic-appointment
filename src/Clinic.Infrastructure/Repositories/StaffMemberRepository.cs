using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class StaffMemberRepository(ClinicDbContext dbContext)
    : Repository<StaffMember>(dbContext), IStaffMemberRepository
{
    // Offered services are loaded so they can be shown and replaced (SetServices).
    public override Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(s => s.ServicesProvided)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StaffMember>> ListAsync(Guid? clinicFacilityId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(s => s.ServicesProvided)
            .Where(s => (clinicFacilityId == null || s.ClinicFacilityId == clinicFacilityId) &&
                        (departmentId == null || s.DepartmentId == departmentId))
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyOfferingServiceAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(s => s.ServicesProvided.Any(sv => sv.Id == serviceId), cancellationToken);
}
