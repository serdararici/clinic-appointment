using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(string specialty, CancellationToken cancellationToken = default);
}
