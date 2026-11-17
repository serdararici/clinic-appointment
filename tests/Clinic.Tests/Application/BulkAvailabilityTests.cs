using Clinic.Application.Availability;
using Clinic.Domain.Common;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>Bulk availability: one range per matching local date, skipping days that overlap an existing range.</summary>
public class BulkAvailabilityTests
{
    // 2026-03-02 is a Monday; 03-02 .. 03-15 covers two full weeks.
    private static readonly DateOnly Monday = new(2026, 3, 2);
    private static readonly DateOnly TwoWeeksLater = new(2026, 3, 15);
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    private readonly FakeDoctorRepository _doctors = new();
    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Doctor _doctor = TestData.NewDoctor();

    private DoctorAvailabilityService CreateService()
    {
        _doctors.Items.Add(_doctor);
        return new DoctorAvailabilityService(_doctors, _appointments, _unitOfWork);
    }

    private static DateTime Utc(DateOnly day, int hour, int minute = 0) =>
        ClinicTimeZone.ToUtc(day.ToDateTime(new TimeOnly(hour, minute)));

    private static BulkAvailabilityRequest Request(
        DateOnly? from = null, DateOnly? to = null, string[]? weekdays = null, TimeOnly? start = null, TimeOnly? end = null) =>
        new(from ?? Monday, to ?? TwoWeeksLater, weekdays ?? Weekdays, start ?? new TimeOnly(9, 0), end ?? new TimeOnly(12, 0));

    [Fact]
    public async Task Creates_OneRangePerMatchingWeekday_InUtc()
    {
        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id, Request());

        Assert.Equal(10, result.Created.Count); // Mon-Fri over two weeks
        Assert.Empty(result.SkippedDates);
        Assert.Equal(10, _doctor.AvailabilitySlots.Count);
        Assert.Equal(10, _doctors.AddedSlots.Count);
        Assert.Equal(1, _unitOfWork.SaveCount);

        // 09:00-12:00 Turkey time (UTC+3) is 06:00-09:00 UTC.
        Assert.Equal(new DateTime(2026, 3, 2, 6, 0, 0), result.Created[0].StartUtc);
        Assert.Equal(new DateTime(2026, 3, 2, 9, 0, 0), result.Created[0].EndUtc);
        Assert.All(result.Created, c => Assert.Equal(DateTimeKind.Utc, c.StartUtc.Kind));
    }

    [Fact]
    public async Task OnlyTheSelectedWeekdays_AreUsed_AndNamesAreCaseInsensitive()
    {
        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id, Request(weekdays: ["monday", "WEDNESDAY"]));

        Assert.Equal(4, result.Created.Count);
        Assert.All(result.Created, c =>
            Assert.Contains(ClinicTimeZone.ToLocal(c.StartUtc).DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }));
    }

    [Fact]
    public async Task ReturnsNothing_AndDoesNotSave_WhenNoDateMatchesTheWeekdays()
    {
        // A single Monday asked for on Tuesdays only.
        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id, Request(Monday, Monday, ["Tuesday"]));

        Assert.Empty(result.Created);
        Assert.Empty(result.SkippedDates);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SkipsADay_WhoseRangeOverlapsAnExistingOne_AndReportsIt()
    {
        var tuesday = Monday.AddDays(1);
        _doctor.AddAvailability(Utc(tuesday, 11), Utc(tuesday, 13)); // overlaps the 09:00-12:00 window

        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id, Request());

        Assert.Equal(9, result.Created.Count);
        Assert.Equal([tuesday], result.SkippedDates); // only that Tuesday; the next week's is free
        Assert.DoesNotContain(result.Created, c => ClinicTimeZone.ToLocal(c.StartUtc).Date == tuesday.ToDateTime(TimeOnly.MinValue));
        Assert.Equal(1 + 9, _doctor.AvailabilitySlots.Count);
    }

    [Fact]
    public async Task DoesNotSkip_ARangeThatOnlyTouchesAnExistingOne()
    {
        // The existing range ends exactly when the new window starts (09:00 local): back to back is not an overlap.
        _doctor.AddAvailability(Utc(Monday, 8), Utc(Monday, 9));

        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id, Request(Monday, Monday, ["Monday"]));

        Assert.Single(result.Created);
        Assert.Empty(result.SkippedDates);
    }

    [Fact]
    public async Task RunningTheSameRequestTwice_CreatesNothingTheSecondTime()
    {
        var service = CreateService();
        await service.AddBulkAvailabilityAsync(_doctor.Id, Request());

        var second = await service.AddBulkAvailabilityAsync(_doctor.Id, Request());

        Assert.Empty(second.Created);
        Assert.Equal(10, second.SkippedDates.Count);
        Assert.Equal(10, _doctor.AvailabilitySlots.Count);
    }

    [Fact]
    public async Task ConvertsTheLocalWindowAcrossTheUtcDateBoundary()
    {
        // 00:30-02:00 local on Monday is 21:30-23:00 UTC on the previous (Sunday) UTC date.
        var result = await CreateService().AddBulkAvailabilityAsync(_doctor.Id,
            Request(Monday, Monday, ["Monday"], new TimeOnly(0, 30), new TimeOnly(2, 0)));

        Assert.Equal(new DateTime(2026, 3, 1, 21, 30, 0), result.Created.Single().StartUtc);
        Assert.Equal(new DateTime(2026, 3, 1, 23, 0, 0), result.Created.Single().EndUtc);
    }

    [Fact]
    public async Task AllowsTheLongestRange_ButRejectsOneDayMore()
    {
        var service = CreateService();
        var limit = Monday.AddDays(DoctorAvailabilityService.MaxBulkDays - 1);

        await service.AddBulkAvailabilityAsync(_doctor.Id, Request(Monday, limit, ["Monday"]));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddBulkAvailabilityAsync(_doctor.Id, Request(Monday, limit.AddDays(1), ["Monday"])));
    }

    [Theory]
    [InlineData("2026-03-10", "2026-03-02", "Monday", 9, 12)]     // ToDate before FromDate
    [InlineData("2026-03-02", "2026-03-15", "Monday", 12, 9)]     // end before start
    [InlineData("2026-03-02", "2026-03-15", "Monday", 9, 9)]      // empty window
    [InlineData("2026-03-02", "2026-03-15", "Funday", 9, 12)]     // not a weekday
    [InlineData("2026-03-02", "2026-03-15", "1", 9, 12)]          // numbers are not accepted
    public async Task RejectsAnInvalidRequest_AndCreatesNothing(string from, string to, string weekday, int startHour, int endHour)
    {
        var request = new BulkAvailabilityRequest(
            DateOnly.Parse(from), DateOnly.Parse(to), [weekday], new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().AddBulkAvailabilityAsync(_doctor.Id, request));

        Assert.Empty(_doctor.AvailabilitySlots);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RejectsAnEmptyWeekdayList_AndAMixOfValidAndInvalidNames_WithoutCreatingAnything()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddBulkAvailabilityAsync(_doctor.Id, Request(weekdays: [])));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddBulkAvailabilityAsync(_doctor.Id, Request(weekdays: ["Monday", "Funday"])));

        Assert.Empty(_doctor.AvailabilitySlots);
    }

    [Fact]
    public async Task Throws_WhenTheDoctorDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().AddBulkAvailabilityAsync(Guid.NewGuid(), Request()));
    }
}
