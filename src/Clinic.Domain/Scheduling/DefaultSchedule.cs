using Clinic.Domain.Common;

namespace Clinic.Domain.Scheduling;

/// <summary>
/// The default work schedule of an automatic-schedule doctor: weekdays only, two clinic-local windows
/// (12:00-13:00 is the lunch break). All of them are whole multiples of every allowed slot duration (10 to 60
/// minutes), so each window is cut into complete slots.
/// </summary>
public static class DefaultSchedule
{
    public static readonly IReadOnlyList<(TimeOnly From, TimeOnly To)> Windows =
    [
        (new TimeOnly(9, 0), new TimeOnly(12, 0)),
        (new TimeOnly(13, 0), new TimeOnly(16, 0)),
    ];

    public static bool IsWorkingDay(DateOnly date) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>
    /// The default windows of one local day as UTC ranges (none on a weekend). A window is shortened to a whole
    /// number of slots of <paramref name="slotDurationMinutes"/> and dropped if not even one slot fits.
    /// </summary>
    public static IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> RangesFor(DateOnly localDate, int slotDurationMinutes)
    {
        var ranges = new List<(DateTime, DateTime)>();
        if (!IsWorkingDay(localDate))
        {
            return ranges;
        }

        foreach (var (from, to) in Windows)
        {
            var minutes = (int)(to - from).TotalMinutes;
            var wholeSlotMinutes = minutes / slotDurationMinutes * slotDurationMinutes;
            if (wholeSlotMinutes == 0)
            {
                continue;
            }

            var startLocal = localDate.ToDateTime(from);
            ranges.Add((ClinicTimeZone.ToUtc(startLocal), ClinicTimeZone.ToUtc(startLocal.AddMinutes(wholeSlotMinutes))));
        }

        return ranges;
    }
}
