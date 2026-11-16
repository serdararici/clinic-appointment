using Clinic.Domain.Repositories;

namespace Clinic.Infrastructure.Persistence;

public class UnitOfWork(ClinicDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
