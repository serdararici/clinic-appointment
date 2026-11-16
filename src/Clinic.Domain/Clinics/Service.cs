using Clinic.Domain.Common;

namespace Clinic.Domain.Clinics;

/// <summary>
/// A billable health service offered by a department (e.g. general check-up, X-ray),
/// with a fixed duration used for slot/appointment scheduling and a base price.
/// </summary>
public class Service : Entity
{
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; }
    public TimeSpan Duration { get; private set; }
    public decimal BasePrice { get; private set; }

    public Service(Guid departmentId, string name, TimeSpan duration, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Service name is required.", nameof(name));
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
        }

        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Price cannot be negative.");
        }

        DepartmentId = departmentId;
        Name = name;
        Duration = duration;
        BasePrice = basePrice;
    }

    protected Service()
    {
        Name = string.Empty;
    }
}
