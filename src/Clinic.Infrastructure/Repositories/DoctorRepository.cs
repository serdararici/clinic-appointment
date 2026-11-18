using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class DoctorRepository(ClinicDbContext dbContext)
    : Repository<Doctor>(dbContext), IDoctorRepository
{
    public override Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(d => d.AvailabilitySlots)
            .Include(d => d.ServicesProvided)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Doctor>> GetAutoScheduleDoctorsAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(d => d.AvailabilitySlots)
            .Where(d => d.AutoScheduleEnabled && d.IsActive)
            .ToListAsync(cancellationToken);

    public void AddAvailability(DoctorAvailability slot) => DbContext.Set<DoctorAvailability>().Add(slot);

    public void RemoveAvailability(DoctorAvailability slot) => DbContext.Set<DoctorAvailability>().Remove(slot);
}
