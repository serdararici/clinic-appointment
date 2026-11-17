using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class PatientRepository(ClinicDbContext dbContext)
    : Repository<Patient>(dbContext), IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> GetByIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken = default) =>
        await DbSet.Where(p => patientIds.Contains(p.Id)).ToListAsync(cancellationToken);
}
