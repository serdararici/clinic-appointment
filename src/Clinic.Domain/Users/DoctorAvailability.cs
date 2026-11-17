using Clinic.Domain.Common;

namespace Clinic.Domain.Users;

/// <summary>
/// A time window in which a doctor is available to be booked.
/// Appointments may only be created inside one of these windows.
/// </summary>
public class DoctorAvailability : Entity
{
    public Guid DoctorId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }

    public DoctorAvailability(Guid doctorId, DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("End time must be after start time.", nameof(endUtc));
        }

        DoctorId = doctorId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    protected DoctorAvailability()
    {
    }

    /// <summary>Whether the given [start, end) range overlaps this window (touching ends do not count).</summary>
    public bool Overlaps(DateTime start, DateTime end) => StartUtc < end && start < EndUtc;

    /// <summary>Whether the given [start, end) range fits entirely within this availability window.</summary>
    public bool Contains(DateTime start, DateTime end) => start >= StartUtc && end <= EndUtc;
}
