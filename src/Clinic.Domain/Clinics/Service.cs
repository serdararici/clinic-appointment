using Clinic.Domain.Common;
using Clinic.Domain.Enums;

namespace Clinic.Domain.Clinics;

/// <summary>
/// A billable health service offered by a department (e.g. general examination, X-ray),
/// with a base price and a duration defined by the manager. How the duration is used depends
/// on <see cref="ServiceType"/>: a Diagnostic service always lasts exactly this long, while an
/// Examination appointment is as long as the chosen doctor's slot duration.
/// </summary>
public class Service : Entity
{
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public TimeSpan Duration { get; private set; }
    public decimal BasePrice { get; private set; }

    public Service(Guid departmentId, string name, ServiceType serviceType, TimeSpan duration, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Service name is required.", nameof(name));
        }

        if (!Enum.IsDefined(serviceType))
        {
            throw new ArgumentOutOfRangeException(nameof(serviceType), "Unknown service type.");
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
        ServiceType = serviceType;
        Duration = duration;
        BasePrice = basePrice;
    }

    protected Service()
    {
        Name = string.Empty;
    }
}
