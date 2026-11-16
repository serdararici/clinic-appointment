namespace Clinic.Domain.Common;

/// <summary>
/// Base class for all domain entities, providing identity.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
