using Clinic.Domain.Clinics;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>Charges the service's base price with no adjustment.</summary>
public class StandardPricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(Service service, Patient patient) => service.BasePrice;
}
