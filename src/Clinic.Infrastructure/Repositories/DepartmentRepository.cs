using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class DepartmentRepository(ClinicDbContext dbContext)
    : Repository<Department>(dbContext), IDepartmentRepository
{
    public async Task<IReadOnlyList<Department>> GetByClinicFacilityIdAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(d => d.ClinicFacilityId == clinicFacilityId).OrderBy(d => d.Name).ToListAsync(cancellationToken);

    public Task<bool> HasServicesAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        DbContext.Services.AnyAsync(s => s.DepartmentId == departmentId, cancellationToken);

    public Task<bool> HasStaffAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        DbContext.StaffMembers.AnyAsync(s => s.DepartmentId == departmentId, cancellationToken);
}
