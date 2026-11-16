using Clinic.Domain.Clinics;
using Clinic.Domain.Users;

namespace Clinic.Domain.Strategies;

/// <summary>Applies a flat discount to patients who have insurance on file.</summary>
public class InsuranceDiscountPricingStrategy : IPricingStrategy
{
    private const decimal DiscountRate = 0.20m;

    public decimal CalculatePrice(Service service, Patient patient) =>
        patient.HasInsurance
            ? Math.Round(service.BasePrice * (1 - DiscountRate), 2)
            : service.BasePrice;
}
