using Clinic.Application.Management;
using Clinic.Application.Staff;
using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>
/// Wires the in-memory fakes together the way the real repositories relate (e.g. a department "has staff"
/// when the staff fake holds someone in it) and offers builders, so each management test only spells out
/// the rule it checks. The clock is fixed at <see cref="Now"/>.
/// </summary>
internal class ManagementFixture
{
    /// <summary>Monday 2026-03-02, 11:00 local (08:00 UTC).</summary>
    public static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeClinicFacilityRepository Clinics { get; } = new();
    public FakeServiceRepository Services { get; } = new();
    public FakeStaffMemberRepository Staff { get; } = new();
    public FakeAppointmentRepository Appointments { get; } = new();
    public FakeHealthOrganizationRepository Organizations { get; }
    public FakeDepartmentRepository Departments { get; }
    public TimeProvider Clock { get; } = new FixedTimeProvider(Now);

    public ManagementFixture()
    {
        Organizations = new FakeHealthOrganizationRepository(Clinics);
        Departments = new FakeDepartmentRepository(Services, Staff);
    }

    public OrganizationService OrganizationService() => new(Organizations, UnitOfWork);

    public BranchService BranchService() => new(Organizations, Clinics, UnitOfWork);

    public DepartmentService DepartmentService() => new(Clinics, Departments, UnitOfWork);

    public ServiceCatalogService ServiceCatalog() => new(Departments, Services, Appointments, Staff, UnitOfWork);

    public StaffService StaffService() => new(Staff, Departments, Services, Appointments, UnitOfWork, Clock);

    // ---- Builders (they add to the fakes, like data that already exists in the database) ----

    public HealthOrganization AddOrganization(string name = "Sakarya Şehir Hastanesi")
    {
        var organization = new HealthOrganization(name);
        Organizations.Items.Add(organization);
        return organization;
    }

    public ClinicFacility AddBranch(HealthOrganization organization, string name = "Merkez")
    {
        var branch = new ClinicFacility(organization.Id, name, "Sakarya");
        Clinics.Items.Add(branch);
        return branch;
    }

    public Department AddDepartment(ClinicFacility branch, string name = "Dahiliye")
    {
        var department = branch.AddDepartment(name);
        Departments.Items.Add(department);
        return department;
    }

    public Service AddService(
        Department department,
        string name = "Genel Muayene",
        ServiceType type = ServiceType.Examination,
        int minutes = 30,
        decimal price = 450m)
    {
        var service = department.AddService(name, type, TimeSpan.FromMinutes(minutes), price);
        Services.Items.Add(service);
        return service;
    }

    public Doctor AddDoctor(Department department, string email = "doctor@example.com", string name = "Dr. Test", int slotMinutes = 30)
    {
        var doctor = new Doctor(name, email, "+90-500-000-0001", department, "İç Hastalıkları", slotMinutes);
        Staff.Items.Add(doctor);
        return doctor;
    }

    public HealthTechnician AddTechnician(Department department, string email = "tech@example.com", string name = "Kemal Arslan")
    {
        var technician = new HealthTechnician(name, email, "+90-500-000-0002", department, "Radyoloji Teknikeri");
        Staff.Items.Add(technician);
        return technician;
    }

    public Appointment AddAppointment(
        Guid? staffMemberId,
        Guid serviceId,
        DateTime start,
        DateTime end,
        AppointmentStatus status = AppointmentStatus.Pending)
    {
        var appointment = new Appointment(Guid.NewGuid(), staffMemberId, serviceId, start, end, 100m, status);
        Appointments.Items.Add(appointment);
        return appointment;
    }
}
