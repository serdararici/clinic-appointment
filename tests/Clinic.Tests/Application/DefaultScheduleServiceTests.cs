using Clinic.Application.Auth;
using Clinic.Application.Availability;
using Clinic.Application.Exceptions;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>
/// The automatic default work schedule. 2026-03-02 is a Monday; the clock is Monday 03:00 local (00:00 UTC), so the
/// booking window is Monday 2 March to Monday 16 March (11 weekdays: 2-6, 9-13 and 16 March).
/// </summary>
public class DefaultScheduleServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeDoctorRepository _doctors = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Department _department = TestData.NewDepartment();

    private DefaultScheduleService CreateService() => new(_doctors, _unitOfWork);

    private Doctor AddDoctor(int slotMinutes = 30, string email = "doctor@example.com")
    {
        var doctor = TestData.NewDoctor(_department, slotMinutes, email);
        _doctors.Items.Add(doctor);
        return doctor;
    }

    private static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(ClinicTimeZone.ToLocal(utc));

    private static IEnumerable<DateOnly> Days(Doctor doctor) => doctor.AvailabilitySlots.Select(s => LocalDate(s.StartUtc)).Distinct().Order();

    [Fact]
    public async Task ADoctorWithoutAvailability_GetsTheDefaultWindowsOnEveryWeekdayFromTodayToTodayPlus14()
    {
        var doctor = AddDoctor();

        var added = await CreateService().EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        Assert.Equal(22, added); // 11 weekdays x 2 windows
        Assert.Equal(new DateOnly(2026, 3, 2), Days(doctor).First()); // today is covered
        Assert.Equal(new DateOnly(2026, 3, 16), Days(doctor).Last());
        Assert.DoesNotContain(Days(doctor), d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task TheWindowsAreTurkeyWallClockTimes_StoredAsUtc_WithALunchBreak()
    {
        var doctor = AddDoctor();

        await CreateService().EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        var monday = doctor.AvailabilitySlots.Where(s => LocalDate(s.StartUtc) == new DateOnly(2026, 3, 2)).OrderBy(s => s.StartUtc).ToList();
        Assert.Equal(2, monday.Count);
        Assert.Equal(new DateTime(2026, 3, 2, 6, 0, 0), monday[0].StartUtc);   // 09:00 local
        Assert.Equal(new DateTime(2026, 3, 2, 9, 0, 0), monday[0].EndUtc);     // 12:00 local
        Assert.Equal(new DateTime(2026, 3, 2, 10, 0, 0), monday[1].StartUtc);  // 13:00 local (lunch 12:00-13:00)
        Assert.Equal(new DateTime(2026, 3, 2, 13, 0, 0), monday[1].EndUtc);    // 16:00 local
    }

    [Fact]
    public async Task ItIsIdempotent_ASecondRunAddsNothing()
    {
        var doctor = AddDoctor();
        var service = CreateService();

        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);
        var count = doctor.AvailabilitySlots.Count;
        var again = await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        Assert.Equal(0, again);
        Assert.Equal(count, doctor.AvailabilitySlots.Count);
        Assert.Equal(1, _unitOfWork.SaveCount); // nothing to save the second time
    }

    [Fact]
    public async Task ItOnlyExtendsForward_WhenTheWindowMovesOn()
    {
        var doctor = AddDoctor();
        var service = CreateService();
        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        // Two days later (Wednesday the 4th) the window reaches Wednesday the 18th: the 17th and the 18th are new.
        var added = await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now.AddDays(2));

        Assert.Equal(4, added);
        Assert.Equal(new DateOnly(2026, 3, 18), Days(doctor).Last());
    }

    [Fact]
    public async Task DaysTheDoctorRemoved_StayClosed_WhileLaterDaysAreStillAdded()
    {
        var doctor = AddDoctor();
        var service = CreateService();
        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        // The doctor takes Wednesday the 4th off.
        foreach (var slot in doctor.AvailabilitySlots.Where(s => LocalDate(s.StartUtc) == new DateOnly(2026, 3, 4)).ToList())
        {
            doctor.RemoveAvailability(slot.Id);
        }

        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);                 // same day: the hole is not refilled
        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now.AddDays(3));      // later: only the end is extended

        Assert.DoesNotContain(Days(doctor), d => d == new DateOnly(2026, 3, 4));
        Assert.Equal(new DateOnly(2026, 3, 19), Days(doctor).Last());
    }

    [Fact]
    public async Task IfTheDoctorRemovesTheLastDays_TheyComeBack()
    {
        var doctor = AddDoctor();
        var service = CreateService();
        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);
        foreach (var slot in doctor.AvailabilitySlots.Where(s => LocalDate(s.StartUtc) == new DateOnly(2026, 3, 16)).ToList())
        {
            doctor.RemoveAvailability(slot.Id);
        }

        await service.EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        Assert.Equal(new DateOnly(2026, 3, 16), Days(doctor).Last()); // the 13th is now the last day, so the 16th is refilled
    }

    [Fact]
    public async Task ItNeverCreatesOverlaps_WithAnExistingFutureRange_AndStartsAfterIt()
    {
        var doctor = AddDoctor();
        doctor.AddAvailability(new DateTime(2026, 3, 5, 6, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 5, 9, 0, 0, DateTimeKind.Utc)); // Thursday the 5th

        await CreateService().EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        // The doctor already has a range on the 5th, so nothing is added before it (the 2nd to 4th stay empty).
        Assert.Equal(new DateOnly(2026, 3, 5), Days(doctor).First());
        Assert.Equal(new DateOnly(2026, 3, 6), Days(doctor).Skip(1).First());
    }

    [Fact]
    public async Task ADoctorWithTheSwitchOff_GetsNothing_AndSoDoesAnInactiveDoctor()
    {
        var off = AddDoctor(email: "off@example.com");
        off.SetAutoSchedule(false);
        var inactive = AddDoctor(email: "inactive@example.com");
        inactive.Deactivate();
        var service = CreateService();

        Assert.Equal(0, await service.EnsureDefaultAvailabilityAsync(off.Id, Now));
        Assert.Equal(0, await service.EnsureDefaultAvailabilityAsync(inactive.Id, Now));
        Assert.Empty(off.AvailabilitySlots);
        Assert.Empty(inactive.AvailabilitySlots);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task EnsureForAll_FillsEveryAutoScheduleDoctor_AndSavesOnce()
    {
        var first = AddDoctor(email: "a@example.com");
        var second = AddDoctor(email: "b@example.com");
        var off = AddDoctor(email: "off@example.com");
        off.SetAutoSchedule(false);

        var added = await CreateService().EnsureForAllAsync(Now);

        Assert.Equal(44, added);
        Assert.Equal(22, first.AvailabilitySlots.Count);
        Assert.Equal(22, second.AvailabilitySlots.Count);
        Assert.Empty(off.AvailabilitySlots);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(10, 18)]
    [InlineData(15, 12)]
    [InlineData(20, 9)]
    [InlineData(30, 6)]
    [InlineData(60, 3)]
    public async Task EveryWindowIsCutIntoWholeSlots_ForEveryAllowedSlotDuration(int slotMinutes, int slotsPerWindow)
    {
        var doctor = AddDoctor(slotMinutes);
        await CreateService().EnsureDefaultAvailabilityAsync(doctor.Id, Now);

        foreach (var window in doctor.AvailabilitySlots)
        {
            var minutes = (window.EndUtc - window.StartUtc).TotalMinutes;
            Assert.Equal(0, minutes % slotMinutes);
            Assert.Equal(slotsPerWindow, (int)(minutes / slotMinutes));
        }
    }

    [Fact]
    public void ARangeThatIsNotAWholeNumberOfSlots_IsShortenedToWholeSlots()
    {
        // The default windows are 3 hours, so this guards the rule with a duration that does not divide 180 minutes.
        var ranges = DefaultSchedule.RangesFor(new DateOnly(2026, 3, 2), 25);

        Assert.All(ranges, r => Assert.Equal(0, (r.EndUtc - r.StartUtc).TotalMinutes % 25));
        Assert.Equal(175, (ranges[0].EndUtc - ranges[0].StartUtc).TotalMinutes);
    }

    [Fact]
    public void Weekends_HaveNoDefaultWindows()
    {
        Assert.Empty(DefaultSchedule.RangesFor(new DateOnly(2026, 3, 7), 30)); // Saturday
        Assert.Empty(DefaultSchedule.RangesFor(new DateOnly(2026, 3, 8), 30)); // Sunday
    }

    [Fact]
    public void TheBookingWindow_IsTodayThroughTodayPlus14_InTurkeyCalendarDays()
    {
        var lateEvening = new DateTime(2026, 3, 1, 22, 0, 0, DateTimeKind.Utc); // 01:00 on the 2nd in Turkey

        Assert.Equal(new DateOnly(2026, 3, 2), BookingRules.Today(lateEvening));
        Assert.True(BookingRules.IsWithinWindow(new DateOnly(2026, 3, 16), lateEvening));
        Assert.False(BookingRules.IsWithinWindow(new DateOnly(2026, 3, 17), lateEvening));
        Assert.False(BookingRules.IsWithinWindow(new DateOnly(2026, 3, 1), lateEvening));
    }

    // ---- The doctor's switch (API-facing service) ----

    private AutoScheduleService SwitchFor(ICurrentUser user) =>
        new(_doctors, _unitOfWork, CreateService(), new FixedTimeProvider(Now),
            new AccessGuard(user, new FakeClinicFacilityRepository(), new FakeDepartmentRepository(new FakeServiceRepository(), new FakeStaffMemberRepository())));

    [Fact]
    public async Task TheDoctorCanReadAndSwitchTheirOwnAutoSchedule_AndSwitchingOnFillsTheWindow()
    {
        var doctor = AddDoctor();
        var service = SwitchFor(FakeCurrentUser.Doctor(doctor.Id));

        Assert.True((await service.GetAsync(doctor.Id)).Enabled); // on by default

        Assert.False((await service.SetAsync(doctor.Id, false)).Enabled);
        Assert.False(doctor.AutoScheduleEnabled);
        Assert.Empty(doctor.AvailabilitySlots);

        Assert.True((await service.SetAsync(doctor.Id, true)).Enabled);
        Assert.Equal(22, doctor.AvailabilitySlots.Count);
    }

    [Fact]
    public async Task TheAutoScheduleSwitch_IsOwnDoctorOnly()
    {
        var doctor = AddDoctor();
        var other = AddDoctor(email: "other@example.com");

        await Assert.ThrowsAsync<ForbiddenException>(() => SwitchFor(FakeCurrentUser.Doctor(other.Id)).GetAsync(doctor.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => SwitchFor(FakeCurrentUser.Doctor(other.Id)).SetAsync(doctor.Id, false));
        await Assert.ThrowsAsync<ForbiddenException>(() => SwitchFor(FakeCurrentUser.Patient(Guid.NewGuid())).GetAsync(doctor.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => SwitchFor(FakeCurrentUser.Admin()).SetAsync(doctor.Id, false));
        Assert.True(doctor.AutoScheduleEnabled);
    }
}
