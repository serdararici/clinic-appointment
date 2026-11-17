using Clinic.Domain.Clinics;
using Clinic.Domain.Common;

namespace Clinic.Tests.Domain;

public class ClinicFacilityTests
{
    // 2026-03-02 is a Monday.
    private static readonly DateTime Monday = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

    private static ClinicFacility CreateClinic()
    {
        var clinic = new ClinicFacility(Guid.NewGuid(), "Test Clinic", "Test Address");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            clinic.AddWorkingHour(day, new TimeOnly(8, 0), new TimeOnly(17, 0));
        }

        return clinic;
    }

    [Fact]
    public void ClinicTimeZone_ConvertsUtcToTurkeyLocal()
    {
        // Turkey is UTC+3 all year (no daylight saving).
        Assert.Equal(new DateTime(2026, 3, 2, 17, 0, 0), ClinicTimeZone.ToLocal(Monday.AddHours(14)));
        Assert.Equal(Monday.AddHours(14), ClinicTimeZone.ToUtc(new DateTime(2026, 3, 2, 17, 0, 0)));
    }

    [Fact]
    public void IsOpenDuring_ReturnsTrue_WhenRangeIsInsideWorkingHours()
    {
        // 06:00-06:30 UTC = 09:00-09:30 local.
        Assert.True(CreateClinic().IsOpenDuring(Monday.AddHours(6), Monday.AddHours(6.5)));
    }

    [Fact]
    public void IsOpenDuring_ReturnsFalse_WhenRangeIsOutsideWorkingHours()
    {
        // 15:00-15:30 UTC = 18:00-18:30 local.
        Assert.False(CreateClinic().IsOpenDuring(Monday.AddHours(15), Monday.AddHours(15.5)));
    }

    [Fact]
    public void IsOpenDuring_UsesLocalTime_NotUtc_AtTheClosingBoundary()
    {
        var clinic = CreateClinic();

        // 13:30-14:00 UTC = 16:30-17:00 local: ends exactly at closing, so it is open.
        Assert.True(clinic.IsOpenDuring(Monday.AddHours(13.5), Monday.AddHours(14)));

        // 14:00-14:30 UTC = 17:00-17:30 local: starts at closing, so it is not.
        // (Read as UTC wall-clock, 14:00 would wrongly look like mid-afternoon.)
        Assert.False(clinic.IsOpenDuring(Monday.AddHours(14), Monday.AddHours(14.5)));
    }

    [Fact]
    public void IsOpenDuring_ReturnsFalse_WhenRangeCrossesLocalMidnight()
    {
        var clinic = new ClinicFacility(Guid.NewGuid(), "Night Clinic", "Test Address");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Tuesday; day++)
        {
            clinic.AddWorkingHour(day, new TimeOnly(0, 0), new TimeOnly(23, 59));
        }

        // 20:30-21:30 UTC = 23:30 Monday to 00:30 Tuesday local.
        Assert.False(clinic.IsOpenDuring(Monday.AddHours(20.5), Monday.AddHours(21.5)));
    }

    [Fact]
    public void IsOpenDuring_ReturnsFalse_WhenClinicHasNoWorkingHours()
    {
        var clinic = new ClinicFacility(Guid.NewGuid(), "Empty Clinic", "Test Address");

        Assert.False(clinic.IsOpenDuring(Monday.AddHours(6), Monday.AddHours(6.5)));
    }
}
