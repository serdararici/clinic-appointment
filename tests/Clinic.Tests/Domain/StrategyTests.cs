using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;
using Xunit;

namespace Clinic.Tests.Domain;

public class StrategyTests
{
    [Fact]
    public void StandardPricingStrategy_ReturnsServiceBasePrice()
    {
        var service = new Service(Guid.NewGuid(), "General Check-up", ServiceType.Examination, TimeSpan.FromMinutes(30), 300m);
        var patient = new Patient("John Doe", "john@example.com", "+90-500-000-0000", new DateOnly(1990, 1, 1));
        IPricingStrategy strategy = new StandardPricingStrategy();

        var price = strategy.CalculatePrice(service, patient);

        Assert.Equal(300m, price);
    }

    [Fact]
    public void InsuranceDiscountPricingStrategy_DiscountsPrice_WhenPatientHasInsurance()
    {
        var service = new Service(Guid.NewGuid(), "General Check-up", ServiceType.Examination, TimeSpan.FromMinutes(30), 300m);
        var insuredPatient = new Patient("John Doe", "john@example.com", "+90-500-000-0000", new DateOnly(1990, 1, 1), insuranceNumber: "INS-1");
        IPricingStrategy strategy = new InsuranceDiscountPricingStrategy();

        var price = strategy.CalculatePrice(service, insuredPatient);

        Assert.Equal(240m, price);
    }

    [Fact]
    public void InsuranceDiscountPricingStrategy_ChargesFullPrice_WhenPatientHasNoInsurance()
    {
        var service = new Service(Guid.NewGuid(), "General Check-up", ServiceType.Examination, TimeSpan.FromMinutes(30), 300m);
        var uninsuredPatient = new Patient("John Doe", "john@example.com", "+90-500-000-0000", new DateOnly(1990, 1, 1));
        IPricingStrategy strategy = new InsuranceDiscountPricingStrategy();

        var price = strategy.CalculatePrice(service, uninsuredPatient);

        Assert.Equal(300m, price);
    }

    [Fact]
    public void AutoConfirmationPolicy_ReturnsConfirmed()
    {
        IConfirmationPolicy policy = new AutoConfirmationPolicy();

        Assert.Equal(AppointmentStatus.Confirmed, policy.DetermineInitialStatus(null));
    }

    [Fact]
    public void ApprovalRequiredConfirmationPolicy_ReturnsPending()
    {
        IConfirmationPolicy policy = new ApprovalRequiredConfirmationPolicy();

        Assert.Equal(AppointmentStatus.Pending, policy.DetermineInitialStatus(null));
    }

    // ---- DoctorPreferenceConfirmationPolicy: each doctor chooses ----

    private static IConfirmationPolicy DoctorPreference() =>
        new DoctorPreferenceConfirmationPolicy(new AutoConfirmationPolicy(), new ApprovalRequiredConfirmationPolicy());

    [Fact]
    public void DoctorPreferencePolicy_ConfirmsABookingWithoutADoctor()
    {
        Assert.Equal(AppointmentStatus.Confirmed, DoctorPreference().DetermineInitialStatus(null));
    }

    [Fact]
    public void DoctorPreferencePolicy_ConfirmsWhenTheDoctorAutoConfirms_AndIsPendingWhenTheyApprove()
    {
        var doctor = TestData.NewDoctor();
        Assert.True(doctor.AutoConfirmAppointments); // on by default
        Assert.Equal(AppointmentStatus.Confirmed, DoctorPreference().DetermineInitialStatus(doctor));

        doctor.SetAutoConfirm(false);
        Assert.Equal(AppointmentStatus.Pending, DoctorPreference().DetermineInitialStatus(doctor));

        doctor.SetAutoConfirm(true);
        Assert.Equal(AppointmentStatus.Confirmed, DoctorPreference().DetermineInitialStatus(doctor));
    }
}
