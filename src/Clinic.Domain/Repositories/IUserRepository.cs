using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

/// <summary>Repository over every kind of user, looked up by email for login and registration.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>The user with this email (compared case-insensitively), or null.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
