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

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
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

    public Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(string specialty, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Doctor>>(Items.Where(d => d.Specialty == specialty).ToList());

    public void AddAvailability(DoctorAvailability slot) => AddedSlots.Add(slot);

    public void RemoveAvailability(DoctorAvailability slot) => RemovedSlots.Add(slot);
}

internal class FakeAppointmentRepository : FakeRepository<Appointment>, IAppointmentRepository
{
    // Mirrors the real query: cancelled/rejected appointments do not block a time range.
    public Task<bool> HasConflictAsync(Guid doctorId, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(a =>
            a.DoctorId == doctorId &&
            a.Status is not (AppointmentStatus.Cancelled or AppointmentStatus.Rejected) &&
            a.OverlapsWith(start, end)));

    public Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(Items.Where(a => a.DoctorId == doctorId).ToList());

    public Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Appointment>>(Items.Where(a => a.PatientId == patientId).ToList());
}

internal class FakePatientRepository : FakeRepository<Patient>, IPatientRepository
{
}

internal class FakeServiceRepository : FakeRepository<Service>, IServiceRepository
{
}

internal class FakeClinicFacilityRepository : FakeRepository<ClinicFacility>, IClinicFacilityRepository
{
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
