using Clinic.Domain.Appointments;

namespace Clinic.Domain.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Whether the given staff member already has a non-cancelled/non-rejected appointment
    /// overlapping [start, end). Used to enforce the no-double-booking rule.
    /// </summary>
    Task<bool> HasConflictAsync(Guid staffMemberId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether an active (not cancelled, not rejected) appointment for the same service overlaps
    /// [start, end). A service belongs to exactly one branch, so this is "same service, same branch".
    /// Used by diagnostic bookings, which have no staff member to conflict on.
    /// </summary>
    Task<bool> HasServiceConflictAsync(Guid serviceId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>Whether any appointment, in any status, still refers to the service.</summary>
    Task<bool> AnyForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the staff member has an active (not cancelled, not rejected) appointment that has not
    /// finished yet at <paramref name="fromUtc"/>, i.e. one still ahead or in progress.
    /// </summary>
    Task<bool> HasUpcomingAsync(Guid staffMemberId, DateTime fromUtc, CancellationToken cancellationToken = default);

    /// <summary>Active appointments of a staff member that overlap [fromUtc, toUtc); used to mark booked slots.</summary>
    Task<IReadOnlyList<Appointment>> GetActiveByStaffMemberAsync(Guid staffMemberId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Active appointments for a service that overlap [fromUtc, toUtc); used to mark booked slots.</summary>
    Task<IReadOnlyList<Appointment>> GetActiveByServiceAsync(Guid serviceId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByStaffMemberIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
