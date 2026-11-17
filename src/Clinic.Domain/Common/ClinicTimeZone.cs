namespace Clinic.Domain.Common;

/// <summary>
/// The single place that defines the clinic's local time zone. All date-times are
/// stored and exchanged in UTC; this class is the only bridge to clinic-local time
/// (used for working hours and for seeding local-time schedules).
/// </summary>
public static class ClinicTimeZone
{
    /// <summary>IANA id for Turkey local time (UTC+3, no daylight saving).</summary>
    public const string Id = "Europe/Istanbul";

    // Resolved lazily so a missing time zone database fails at first use with a clear message.
    private static readonly Lazy<TimeZoneInfo> Zone = new(() => TimeZoneInfo.FindSystemTimeZoneById(Id));

    public static TimeZoneInfo Info => Zone.Value;

    /// <summary>Converts a UTC date-time to clinic local time.</summary>
    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Info);

    /// <summary>Converts a clinic-local wall-clock date-time to UTC.</summary>
    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Info);
}
