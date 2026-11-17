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
    /// <summary>Largest price the database column (decimal(10,2)) can hold.</summary>
    public const decimal MaxBasePrice = 99_999_999.99m;

    /// <summary>Longest allowed duration; keeps "start + duration" arithmetic far from date overflow.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public TimeSpan Duration { get; private set; }
    public decimal BasePrice { get; private set; }

    public Service(Guid departmentId, string name, ServiceType serviceType, TimeSpan duration, decimal basePrice)
    {
        DepartmentId = departmentId;
        Name = ValidateName(name);
        ServiceType = ValidateType(serviceType);
        Duration = ValidateDuration(duration);
        BasePrice = ValidatePrice(basePrice);
    }

    protected Service()
    {
        Name = string.Empty;
    }

    /// <summary>Replaces the service's editable details; the department never changes.</summary>
    public void Update(string name, ServiceType serviceType, TimeSpan duration, decimal basePrice)
    {
        // Validate everything first so a rejected update changes nothing.
        var validName = ValidateName(name);
        var validType = ValidateType(serviceType);
        var validDuration = ValidateDuration(duration);
        var validPrice = ValidatePrice(basePrice);

        Name = validName;
        ServiceType = validType;
        Duration = validDuration;
        BasePrice = validPrice;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Service name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static ServiceType ValidateType(ServiceType serviceType)
    {
        if (!Enum.IsDefined(serviceType))
        {
            throw new ArgumentOutOfRangeException(nameof(serviceType), "Unknown service type.");
        }

        return serviceType;
    }

    private static TimeSpan ValidateDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
        }

        if (duration > MaxDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), $"Duration cannot exceed {MaxDuration.TotalHours:0} hours.");
        }

        return duration;
    }

    private static decimal ValidatePrice(decimal basePrice)
    {
        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Price cannot be negative.");
        }

        if (basePrice > MaxBasePrice)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), $"Price cannot exceed {MaxBasePrice}.");
        }

        // Prices are stored with two decimals; reject finer values instead of silently rounding them.
        if (decimal.Round(basePrice, 2) != basePrice)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Price can have at most two decimal places.");
        }

        return basePrice;
    }
}
