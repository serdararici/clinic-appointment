using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Tests.Domain;

public class StaffMemberTests
{
    private static HealthTechnician NewTechnician(Clinic.Domain.Clinics.Department? department = null) =>
        new("Kemal Arslan", "kemal@example.com", "+90-500-000-0001", department ?? TestData.NewDepartment(), "Radyoloji Teknikeri");

    // ---- Slot duration ---------------------------------------------------------------------

    [Fact]
    public void SlotDuration_DefaultsToThirtyMinutes()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", TestData.NewDepartment(), "İç Hastalıkları");

        Assert.Equal(30, doctor.SlotDurationMinutes);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(60)]
    public void SlotDuration_AcceptsEveryAllowedValue(int minutes)
    {
        var doctor = TestData.NewDoctor(slotDurationMinutes: minutes);
        Assert.Equal(minutes, doctor.SlotDurationMinutes);

        var other = TestData.NewDoctor();
        other.SetSlotDuration(minutes);
        Assert.Equal(minutes, other.SlotDurationMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(5)]
    [InlineData(25)]
    [InlineData(45)]
    [InlineData(90)]
    public void SlotDuration_RejectsValuesThatAreNotAllowed(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.NewDoctor(slotDurationMinutes: minutes));

        var doctor = TestData.NewDoctor(slotDurationMinutes: 20);
        Assert.Throws<ArgumentOutOfRangeException>(() => doctor.SetSlotDuration(minutes));
        Assert.Equal(20, doctor.SlotDurationMinutes); // a rejected change leaves the old value
    }

    // ---- Department / facility link --------------------------------------------------------

    [Fact]
    public void StaffMember_TakesItsFacilityFromTheDepartment()
    {
        var department = TestData.NewDepartment();

        var doctor = TestData.NewDoctor(department);

        Assert.Equal(department.Id, doctor.DepartmentId);
        Assert.Equal(department.ClinicFacilityId, doctor.ClinicFacilityId);
    }

    [Fact]
    public void StaffMember_Throws_WhenDepartmentIsMissing()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", null!, "İç Hastalıkları"));
    }

    // ---- Services provided -----------------------------------------------------------------

    [Fact]
    public void AddService_Throws_WhenServiceBelongsToAnotherDepartment()
    {
        var radiology = TestData.NewDepartment("Radyoloji");
        var xRay = radiology.AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);
        var doctor = TestData.NewDoctor(TestData.NewDepartment("Dahiliye"));

        Assert.Throws<InvalidOperationException>(() => doctor.AddService(xRay));
        Assert.Empty(doctor.ServicesProvided);
    }

    [Fact]
    public void AddService_IgnoresDuplicates()
    {
        var department = TestData.NewDepartment();
        var service = department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 450m);
        var doctor = TestData.NewDoctor(department);

        doctor.AddService(service);
        doctor.AddService(service);

        Assert.Single(doctor.ServicesProvided);
    }

    // ---- Doctor / HealthTechnician ---------------------------------------------------------

    [Fact]
    public void Doctor_Throws_WhenSpecialtyIsMissing()
    {
        Assert.Throws<ArgumentException>(() =>
            new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", TestData.NewDepartment(), " "));
    }

    [Fact]
    public void HealthTechnician_Throws_WhenTitleIsMissing()
    {
        Assert.Throws<ArgumentException>(() =>
            new HealthTechnician("Kemal Arslan", "kemal@example.com", "+90-500-000-0001", TestData.NewDepartment(), ""));
    }

    [Fact]
    public void HealthTechnician_IsAStaffMemberWithDefaultSlotDuration_AndItsOwnTitle()
    {
        var department = TestData.NewDepartment("Radyoloji");

        var technician = NewTechnician(department);

        Assert.IsAssignableFrom<StaffMember>(technician);
        Assert.Equal("Radyoloji Teknikeri", technician.Title);
        Assert.Equal(department.Id, technician.DepartmentId);
        Assert.Equal(30, technician.SlotDurationMinutes);
    }

    [Fact]
    public void RoleName_IsResolvedPolymorphically_AcrossTheHierarchy()
    {
        User[] users =
        [
            TestData.NewDoctor(),
            NewTechnician(),
            new ClinicManager("Ayşe Yılmaz", "ayse@example.com", "+90-500-000-0002", Guid.NewGuid()),
            new Patient("Ahmet Çelik", "ahmet@example.com", "+90-500-000-0003", new DateOnly(1990, 1, 1)),
        ];

        Assert.Equal(["Doctor", "HealthTechnician", "ClinicManager", "Patient"], users.Select(u => u.RoleName));
    }
}
