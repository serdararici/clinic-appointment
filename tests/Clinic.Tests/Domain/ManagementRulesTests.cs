using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Tests.Domain;

/// <summary>Domain rules behind the management endpoints: names, working hours, services, staff.</summary>
public class ManagementRulesTests
{
    private static ClinicFacility NewBranch() => new(Guid.NewGuid(), "Merkez", "Sakarya");

    // =========================================================================================
    // Organization, branch, department names
    // =========================================================================================

    [Fact]
    public void Organization_Rename_ChangesTheName_AndRejectsABlankOne()
    {
        var organization = new HealthOrganization("Eski Ad");

        organization.Rename("  Yeni Ad  ");
        Assert.Equal("Yeni Ad", organization.Name); // trimmed

        Assert.Throws<ArgumentException>(() => organization.Rename(" "));
        Assert.Equal("Yeni Ad", organization.Name);  // a rejected rename changes nothing
    }

    [Fact]
    public void Branch_UpdateDetails_ChangesNameAndAddress_AndKeepsTheOrganization()
    {
        var organizationId = Guid.NewGuid();
        var branch = new ClinicFacility(organizationId, "Merkez", "Eski adres");

        branch.UpdateDetails("Adapazarı Şubesi", "Yeni adres");

        Assert.Equal("Adapazarı Şubesi", branch.Name);
        Assert.Equal("Yeni adres", branch.Address);
        Assert.Equal(organizationId, branch.OrganizationId);
    }

    [Theory]
    [InlineData("", "Adres")]
    [InlineData("Ad", " ")]
    public void Branch_UpdateDetails_RejectsBlankValues_AndChangesNothing(string name, string address)
    {
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");

        Assert.Throws<ArgumentException>(() => branch.UpdateDetails(name, address));

        Assert.Equal("Merkez", branch.Name);
        Assert.Equal("Sakarya", branch.Address);
    }

    [Fact]
    public void Branch_RequiresAnAddressWhenCreated()
    {
        Assert.Throws<ArgumentException>(() => new ClinicFacility(Guid.NewGuid(), "Merkez", ""));
    }

    [Fact]
    public void Department_Rename_ChangesTheName_AndRejectsABlankOne()
    {
        var department = TestData.NewDepartment("Dahiliye");

        department.Rename("Radyoloji");
        Assert.Equal("Radyoloji", department.Name);

        Assert.Throws<ArgumentException>(() => department.Rename(""));
        Assert.Equal("Radyoloji", department.Name);
    }

    // =========================================================================================
    // Working hours
    // =========================================================================================

    [Fact]
    public void WorkingHours_RejectARangeThatOverlapsAnotherOnTheSameDay()
    {
        var branch = NewBranch();
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));

        Assert.Throws<InvalidOperationException>(() =>
            branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0))); // overlaps the end
        Assert.Throws<InvalidOperationException>(() =>
            branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(9, 0)));   // overlaps the start
        Assert.Throws<InvalidOperationException>(() =>
            branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)));  // inside
        Assert.Throws<InvalidOperationException>(() =>
            branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(18, 0)));  // around it
        Assert.Throws<InvalidOperationException>(() =>
            branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)));  // identical

        Assert.Single(branch.WorkingHours);
    }

    [Fact]
    public void WorkingHours_AllowBackToBackRanges_AGapAndTheSameTimesOnAnotherDay()
    {
        var branch = NewBranch();
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));

        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(13, 0));  // touches 12:00
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(17, 0));  // after a lunch break
        branch.AddWorkingHour(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(12, 0));  // another weekday

        Assert.Equal(4, branch.WorkingHours.Count);
    }

    [Fact]
    public void WorkingHours_RejectAClosingTimeThatIsNotAfterOpening()
    {
        var branch = NewBranch();

        Assert.Throws<ArgumentException>(() => branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(8, 0)));
        Assert.Throws<ArgumentException>(() => branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(9, 0)));
        Assert.Empty(branch.WorkingHours);
    }

    [Fact]
    public void WorkingHours_CanBeRemovedById_AndAnUnknownIdReturnsNull()
    {
        var branch = NewBranch();
        var monday = branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        Assert.Null(branch.RemoveWorkingHour(Guid.NewGuid()));
        Assert.Same(monday, branch.RemoveWorkingHour(monday.Id));
        Assert.Empty(branch.WorkingHours);

        // The freed time can be used again.
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
        Assert.Single(branch.WorkingHours);
    }

    // =========================================================================================
    // Service validation and update
    // =========================================================================================

    [Fact]
    public void Service_Update_ReplacesAllEditableDetails_ButNotTheDepartment()
    {
        var department = TestData.NewDepartment();
        var service = department.AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);

        service.Update("  MR  ", ServiceType.Examination, TimeSpan.FromMinutes(45), 1800.50m);

        Assert.Equal("MR", service.Name);
        Assert.Equal(ServiceType.Examination, service.ServiceType);
        Assert.Equal(TimeSpan.FromMinutes(45), service.Duration);
        Assert.Equal(1800.50m, service.BasePrice);
        Assert.Equal(department.Id, service.DepartmentId);
    }

    [Fact]
    public void Service_Update_WithAnInvalidValue_ChangesNothing()
    {
        var service = TestData.NewDepartment().AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);

        // A valid name and duration followed by an invalid price: nothing may be applied.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.Update("MR", ServiceType.Examination, TimeSpan.FromMinutes(45), -1m));

        Assert.Equal("Röntgen", service.Name);
        Assert.Equal(ServiceType.Diagnostic, service.ServiceType);
        Assert.Equal(TimeSpan.FromMinutes(20), service.Duration);
        Assert.Equal(350m, service.BasePrice);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    [InlineData(100_000_000)]   // above the largest value the decimal(10,2) column holds
    [InlineData(10.001)]        // more than two decimal places
    [InlineData(0.005)]
    public void Service_RejectsPricesThatAreNegativeTooLargeOrNeedMoreThanTwoDecimals(double price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), (decimal)price));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(99.99)]
    [InlineData(99_999_999.99)]
    public void Service_AcceptsPricesFromZeroUpToTheLargestStorableValue(double price)
    {
        var service = new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), (decimal)price);

        Assert.Equal((decimal)price, service.BasePrice);
    }

    [Fact]
    public void Service_RejectsADurationThatIsNotPositiveOrLongerThanADay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.Zero, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.FromMinutes(-5), 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.FromHours(24).Add(TimeSpan.FromMinutes(1)), 10m));

        // Exactly 24 hours is still allowed.
        Assert.Equal(TimeSpan.FromHours(24),
            new Service(Guid.NewGuid(), "Test", ServiceType.Diagnostic, TimeSpan.FromHours(24), 10m).Duration);
    }

    // =========================================================================================
    // Staff
    // =========================================================================================

    [Fact]
    public void StaffMember_StartsActive_AndCanBeDeactivatedAndReactivated()
    {
        var doctor = TestData.NewDoctor();
        Assert.True(doctor.IsActive);

        doctor.Deactivate();
        Assert.False(doctor.IsActive);

        doctor.Activate();
        Assert.True(doctor.IsActive);
    }

    [Fact]
    public void Staff_Rename_AndTechnicianTitle_AreValidated()
    {
        var doctor = TestData.NewDoctor();
        var technician = new HealthTechnician("Kemal Arslan", "kemal@example.com", "+90-500-000-0001", TestData.NewDepartment(), "Radyoloji Teknikeri");

        doctor.Rename("Dr. Yeni Ad");
        technician.ChangeTitle("Laboratuvar Teknisyeni");

        Assert.Equal("Dr. Yeni Ad", doctor.FullName);
        Assert.Equal("Laboratuvar Teknisyeni", technician.Title);

        Assert.Throws<ArgumentException>(() => doctor.Rename(" "));
        Assert.Throws<ArgumentException>(() => technician.ChangeTitle(" "));
    }

    [Fact]
    public void SetServices_ReplacesTheWholeSet_AndCollapsesDuplicates()
    {
        var department = TestData.NewDepartment();
        var first = department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 450m);
        var second = department.AddService("Kontrol Muayenesi", ServiceType.Examination, TimeSpan.FromMinutes(15), 250m);
        var doctor = TestData.NewDoctor(department);
        doctor.AddService(first);

        doctor.SetServices([second, second]);

        Assert.Equal([second.Id], doctor.ServicesProvided.Select(s => s.Id));
        Assert.False(doctor.OffersService(first.Id));
    }

    [Fact]
    public void SetServices_WithAnEmptySet_ClearsTheOfferedServices()
    {
        var department = TestData.NewDepartment();
        var doctor = TestData.NewDoctor(department);
        doctor.AddService(department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 450m));

        doctor.SetServices([]);

        Assert.Empty(doctor.ServicesProvided);
    }

    [Fact]
    public void SetServices_RejectsAServiceOfAnotherDepartment_AndChangesNothing()
    {
        var department = TestData.NewDepartment("Dahiliye");
        var own = department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 450m);
        var foreign = TestData.NewDepartment("Radyoloji").AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);
        var doctor = TestData.NewDoctor(department);
        doctor.AddService(own);

        // The valid service comes first: the whole call must still be rejected as a unit.
        Assert.Throws<ArgumentException>(() => doctor.SetServices([own, foreign]));

        Assert.Equal([own.Id], doctor.ServicesProvided.Select(s => s.Id));
    }
}
