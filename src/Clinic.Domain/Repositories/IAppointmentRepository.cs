using Clinic.Domain.Appointments;

namespace Clinic.Domain.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Whether the given staff member already has a non-cancelled/non-rejected appointment
    /// overlapping [start, end). Used to enforce the no-double-booking rule.
    /// </summary>
    Task<bool> HasConflictAsync(Guid staffMemberId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByStaffMemberIdAsync(Guid staffMemberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
