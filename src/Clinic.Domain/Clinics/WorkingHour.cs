using Clinic.Domain.Common;

namespace Clinic.Domain.Clinics;

/// <summary>
/// A single day-of-week opening window for a clinic facility.
/// </summary>
public class WorkingHour : Entity
{
    public Guid ClinicFacilityId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpensAt { get; private set; }
    public TimeOnly ClosesAt { get; private set; }

    public WorkingHour(Guid clinicFacilityId, DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        if (closesAt <= opensAt)
        {
            throw new ArgumentException("Closing time must be after opening time.", nameof(closesAt));
        }

        ClinicFacilityId = clinicFacilityId;
        DayOfWeek = dayOfWeek;
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    protected WorkingHour()
    {
    }

    public bool Contains(DayOfWeek day, TimeOnly time) =>
        DayOfWeek == day && time >= OpensAt && time <= ClosesAt;
}
