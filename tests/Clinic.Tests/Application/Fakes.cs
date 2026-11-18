using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Enums;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>Generic in-memory repository so Application services can be tested without a database.</summary>
internal class FakeRepository<T> : IRepository<T> where T : Entity
{
    public List<T> Items { get; } = [];

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(Items.ToList());

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => Items.Remove(entity);
}

internal class FakeDoctorRepository : FakeRepository<Doctor>, IDoctorRepository
{
    public List<DoctorAvailability> AddedSlots { get; } = [];
    public List<DoctorAvailability> RemovedSlots { get; } = [];

    public void AddAvailability(DoctorAvailability slot) => AddedSlots.Add(slot);

    public void RemoveAvailability(DoctorAvailability slot) => RemovedSlots.Add(slot);
}

internal class FakeAppointmentRepository : FakeRepository<Appointment>, IAppointmentRepository
{
    // Mirrors the real query: cancelled/rejected appointments do not block a time range.
    public Task<bool> HasConflictAsync(Guid staffMemberId, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(a =>
            a.StaffMemberId == staffMemberId &&
            a.Status is not (AppointmentStatus.Cancelled or AppointmentStatus.Rejected) &&
            a.OverlapsWith(start, end)));

    public Task<bool> HasServiceConflictAsync(Guid serviceId, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(a => a.ServiceId == serviceId && a.IsActive && a.OverlapsWith(start, end)));

    public Task<bool> AnyForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(a => a.ServiceId == serviceId));

    public Task<bool> HasUpcomingAsync(Guid staffMemberId, DateTime fromUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(a => a.StaffMemberId == staffMemberId && a.IsActive && a.ScheduledEndUtc > fromUtc));

    public Task<IReadOnlyList<Appointment>> GetActiveByStaffMemberAsync(Guid staffMemberId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(
            Items.Where(a => a.StaffMemberId == staffMemberId && a.IsActive && a.OverlapsWith(fromUtc, toUtc)).ToList());

    public Task<IReadOnlyList<Appointment>> GetActiveByServiceAsync(Guid serviceId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(
            Items.Where(a => a.ServiceId == serviceId && a.IsActive && a.OverlapsWith(fromUtc, toUtc)).ToList());

    public Task<IReadOnlyList<Appointment>> GetByStaffMemberIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(Items.Where(a => a.StaffMemberId == staffMemberId).ToList());

    public Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(Items.Where(a => a.PatientId == patientId).ToList());
}

internal class FakeStaffMemberRepository : FakeRepository<StaffMember>, IStaffMemberRepository
{
    public Task<IReadOnlyList<StaffMember>> ListAsync(Guid? clinicFacilityId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StaffMember>>(Items
            .Where(s => (clinicFacilityId is null || s.ClinicFacilityId == clinicFacilityId) &&
                        (departmentId is null || s.DepartmentId == departmentId))
            .OrderBy(s => s.FullName)
            .ToList());

    public Task<bool> AnyOfferingServiceAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(s => s.OffersService(serviceId)));
}

internal class FakePatientRepository : FakeRepository<Patient>, IPatientRepository
{
    public Task<IReadOnlyList<Patient>> GetByIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Patient>>(Items.Where(p => patientIds.Contains(p.Id)).ToList());
}

internal class FakeServiceRepository : FakeRepository<Service>, IServiceRepository
{
    public Task<IReadOnlyList<Service>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Service>>(Items.Where(s => s.DepartmentId == departmentId).OrderBy(s => s.Name).ToList());

    public Task<IReadOnlyList<Service>> GetByIdsAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Service>>(Items.Where(s => serviceIds.Contains(s.Id)).ToList());
}

internal class FakeClinicFacilityRepository : FakeRepository<ClinicFacility>, IClinicFacilityRepository
{
    /// <summary>Clinic managers assigned to branches (the real repository queries the managers table).</summary>
    public List<ClinicManager> Managers { get; } = [];

    public List<WorkingHour> AddedWorkingHours { get; } = [];
    public List<WorkingHour> RemovedWorkingHours { get; } = [];

    public Task<ClinicFacility?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Departments.Any(d => d.Services.Any(sv => sv.Id == serviceId))));

    public Task<Guid?> GetOrganizationIdAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == clinicFacilityId)?.OrganizationId);

    public Task<IReadOnlyList<ClinicFacility>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClinicFacility>>(Items.Where(c => c.OrganizationId == organizationId).OrderBy(c => c.Name).ToList());

    public Task<bool> HasManagersAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Managers.Any(m => m.ClinicFacilityId == clinicFacilityId));

    public void AddWorkingHour(WorkingHour workingHour) => AddedWorkingHours.Add(workingHour);

    public void RemoveWorkingHour(WorkingHour workingHour) => RemovedWorkingHours.Add(workingHour);
}

internal class FakeHealthOrganizationRepository(FakeClinicFacilityRepository clinics)
    : FakeRepository<HealthOrganization>, IHealthOrganizationRepository
{
    public override Task<IReadOnlyList<HealthOrganization>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HealthOrganization>>(Items.OrderBy(o => o.Name).ToList());

    public Task<bool> HasBranchesAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(clinics.Items.Any(c => c.OrganizationId == organizationId));
}

internal class FakeDepartmentRepository(FakeServiceRepository services, FakeStaffMemberRepository staff)
    : FakeRepository<Department>, IDepartmentRepository
{
    public Task<IReadOnlyList<Department>> GetByClinicFacilityIdAsync(Guid clinicFacilityId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Department>>(Items.Where(d => d.ClinicFacilityId == clinicFacilityId).OrderBy(d => d.Name).ToList());

    public Task<bool> HasServicesAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(services.Items.Any(s => s.DepartmentId == departmentId));

    public Task<bool> HasStaffAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(staff.Items.Any(s => s.DepartmentId == departmentId));
}

/// <summary>A clock frozen at a chosen instant, so Past/Available slot statuses are deterministic.</summary>
internal class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
}

internal class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}
