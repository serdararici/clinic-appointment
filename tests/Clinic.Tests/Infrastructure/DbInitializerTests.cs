using Clinic.Infrastructure.Seed;

namespace Clinic.Tests.Infrastructure;

public class DbInitializerTests
{
    [Fact]
    public void BuildWeekdayWindows_SkipsWeekends()
    {
        // 2026-03-02 is a Monday; 14 days = two full weeks = 10 weekdays.
        var windows = DbInitializer.BuildWeekdayWindows(new DateOnly(2026, 3, 2), 14, new TimeOnly(9, 0), new TimeOnly(12, 0));

        Assert.Equal(10, windows.Count);
        Assert.DoesNotContain(windows, w => w.StartUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
    }

    [Fact]
    public void BuildWeekdayWindows_TreatsTimesAsTurkeyLocal_AndReturnsUtc()
    {
        var windows = DbInitializer.BuildWeekdayWindows(new DateOnly(2026, 3, 2), 1, new TimeOnly(9, 0), new TimeOnly(12, 0));

        // Turkey is UTC+3: 09:00-12:00 local is 06:00-09:00 UTC.
        Assert.Equal(new DateTime(2026, 3, 2, 6, 0, 0), windows[0].StartUtc);
        Assert.Equal(new DateTime(2026, 3, 2, 9, 0, 0), windows[0].EndUtc);
    }

    [Fact]
    public void BuildWeekdayWindows_StartingOnASaturday_FirstWindowIsMonday()
    {
        // 2026-03-07 is a Saturday.
        var windows = DbInitializer.BuildWeekdayWindows(new DateOnly(2026, 3, 7), 3, new TimeOnly(10, 0), new TimeOnly(15, 0));

        var only = Assert.Single(windows);
        Assert.Equal(DayOfWeek.Monday, only.StartUtc.DayOfWeek);
    }
}
