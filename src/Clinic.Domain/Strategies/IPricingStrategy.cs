using Clinic.Domain.Clinics;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>
/// Strategy pattern: computes the price a patient pays for a service.
/// Swappable per appointment (e.g. standard vs. insurance-discounted pricing).
/// </summary>
public interface IPricingStrategy
{
    decimal CalculatePrice(Service service, Patient patient);
}
