namespace Clinic.Application.Availability;

/// <summary>
/// Input DTO for creating availability over a date range. Dates and times are clinic local time
/// (Europe/Istanbul): for every date from <paramref name="FromDate"/> to <paramref name="ToDate"/> (inclusive)
/// whose weekday is listed in <paramref name="Weekdays"/> (names such as "Monday"), one range
/// [<paramref name="StartTime"/>, <paramref name="EndTime"/>) is created.
/// </summary>
public record BulkAvailabilityRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<string> Weekdays,
    TimeOnly StartTime,
    TimeOnly EndTime);

/// <summary>
/// Which ranges were created, which dates were skipped because the range would overlap an existing one,
/// and which were skipped because the range has already started (past).
/// </summary>
public record BulkAvailabilityResultDto(
    IReadOnlyList<AvailabilityDto> Created,
    IReadOnlyList<DateOnly> SkippedDates,
    IReadOnlyList<DateOnly> PastDates);
