namespace Clinic.Application.Availability;

/// <summary>
/// Keeps the default work schedule filled in for doctors who have the automatic schedule switched on. It acts as the
/// system, not as a logged-in user, so it has no access checks: the callers are the startup run, the periodic
/// background worker and (later) the "create doctor" flow.
/// </summary>
public interface IDefaultScheduleService
{
    /// <summary>
    /// Adds the default windows (weekdays 09:00-12:00 and 13:00-16:00, clinic time) for every day from
    /// max(today, the day after the doctor's last existing availability) up to today + 14 days. It only ever extends
    /// forward, so days the doctor removed before their last availability day stay closed, and it does nothing for a
    /// doctor with the automatic schedule off. Returns how many ranges were added (0 when already up to date).
    /// </summary>
    Task<int> EnsureDefaultAvailabilityAsync(Guid doctorId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>The same for every active doctor with the automatic schedule on; saves once. Returns the ranges added.</summary>
    Task<int> EnsureForAllAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
