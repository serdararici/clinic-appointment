using Clinic.Domain.Common;

namespace Clinic.Domain.Scheduling;

/// <summary>
/// The rolling booking window, in one place: patients can book from today up to <see cref="WindowDays"/> days
/// later, counted in Europe/Istanbul calendar days. The slot lists, the booking check and the automatic default
/// schedule all use this, so they cannot disagree. (The frontend mirrors the number in its own constant.)
/// </summary>
public static class BookingRules
{
    public const int WindowDays = 14;

    /// <summary>Today's calendar date in the clinic time zone.</summary>
    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(ClinicTimeZone.ToLocal(utcNow));

    /// <summary>The last bookable calendar date (today + <see cref="WindowDays"/>).</summary>
    public static DateOnly LastBookableDate(DateTime utcNow) => Today(utcNow).AddDays(WindowDays);

    /// <summary>Whether a clinic-local calendar date lies inside the window (today and the last date included).</summary>
    public static bool IsWithinWindow(DateOnly localDate, DateTime utcNow) =>
        localDate >= Today(utcNow) && localDate <= LastBookableDate(utcNow);
}
