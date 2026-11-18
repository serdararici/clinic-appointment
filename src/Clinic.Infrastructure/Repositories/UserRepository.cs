using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class UserRepository(ClinicDbContext dbContext)
    : Repository<User>(dbContext), IUserRepository
{
    // SQL Server's default collation compares text case-insensitively, so "A@x.com" finds "a@x.com".
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<ClinicManager>> GetClinicManagersAsync(CancellationToken cancellationToken = default) =>
        await DbContext.ClinicManagers.OrderBy(m => m.FullName).ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(u => u.Email == email, cancellationToken);
}
