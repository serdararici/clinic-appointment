namespace Clinic.Domain.Repositories;

/// <summary>
/// Commits the changes made through repositories in a single persistence transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
