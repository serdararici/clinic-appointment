using Clinic.Domain.Appointments;
using Clinic.Domain.Enums;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class AppointmentRepository(ClinicDbContext dbContext)
    : Repository<Appointment>(dbContext), IAppointmentRepository
{
    public Task<bool> HasConflictAsync(Guid staffMemberId, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(a =>
                a.StaffMemberId == staffMemberId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Rejected &&
                a.ScheduledStartUtc < end &&
                start < a.ScheduledEndUtc,
            cancellationToken);

    public Task<bool> HasServiceConflictAsync(Guid serviceId, DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(a =>
                a.ServiceId == serviceId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Rejected &&
                a.ScheduledStartUtc < end &&
                start < a.ScheduledEndUtc,
            cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetActiveByStaffMemberAsync(Guid staffMemberId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(a => a.StaffMemberId == staffMemberId &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.Rejected &&
                        a.ScheduledStartUtc < toUtc &&
                        fromUtc < a.ScheduledEndUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetActiveByServiceAsync(Guid serviceId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(a => a.ServiceId == serviceId &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.Rejected &&
                        a.ScheduledStartUtc < toUtc &&
                        fromUtc < a.ScheduledEndUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByStaffMemberIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(a => a.StaffMemberId == staffMemberId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(a => a.PatientId == patientId).ToListAsync(cancellationToken);
}
