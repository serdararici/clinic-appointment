using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;

namespace Clinic.Tests.Domain;

public class OrganizationAndServiceTests
{
    // ---- Organization and branch -----------------------------------------------------------

    [Fact]
    public void HealthOrganization_RequiresAName()
    {
        Assert.Throws<ArgumentException>(() => new HealthOrganization(" "));
        Assert.Equal("Sakarya Şehir Hastanesi", new HealthOrganization("Sakarya Şehir Hastanesi").Name);
    }

    [Fact]
    public void ClinicFacility_BelongsToAnOrganization_AndNamesTheBranch()
    {
        var organization = new HealthOrganization("Sakarya Şehir Hastanesi");

        var branch = new ClinicFacility(organization.Id, "Adapazarı Şubesi", "Adapazarı / Sakarya");

        Assert.Equal(organization.Id, branch.OrganizationId);
        Assert.Equal("Adapazarı Şubesi", branch.Name);
    }

    [Fact]
    public void ClinicFacility_Throws_WhenThereIsNoOrganization()
    {
        Assert.Throws<ArgumentException>(() => new ClinicFacility(Guid.Empty, "Merkez", "Sakarya"));
    }

    [Fact]
    public void ClinicFacility_Throws_WhenBranchNameIsMissing()
    {
        Assert.Throws<ArgumentException>(() => new ClinicFacility(Guid.NewGuid(), "", "Sakarya"));
    }

    [Fact]
    public void ClinicFacility_AddDepartment_LinksTheDepartmentToTheBranch()
    {
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");

        var department = branch.AddDepartment("Dahiliye");

        Assert.Equal(branch.Id, department.ClinicFacilityId);
    }

    // ---- Service type ----------------------------------------------------------------------

    [Theory]
    [InlineData(ServiceType.Examination)]
    [InlineData(ServiceType.Diagnostic)]
    public void Service_KeepsItsTypeNameDurationAndBasePrice(ServiceType type)
    {
        var department = TestData.NewDepartment("Radyoloji");

        var service = department.AddService("MR", type, TimeSpan.FromMinutes(45), 1800m);

        Assert.Equal(type, service.ServiceType);
        Assert.Equal("MR", service.Name);
        Assert.Equal(TimeSpan.FromMinutes(45), service.Duration);
        Assert.Equal(1800m, service.BasePrice);
        Assert.Equal(department.Id, service.DepartmentId);
    }

    [Fact]
    public void Service_Throws_WhenTypeIsNotAKnownValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "MR", (ServiceType)99, TimeSpan.FromMinutes(45), 1800m));
    }

    [Fact]
    public void Service_Throws_WhenDurationOrPriceIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "MR", ServiceType.Diagnostic, TimeSpan.Zero, 1800m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "MR", ServiceType.Diagnostic, TimeSpan.FromMinutes(45), -1m));
    }
}
