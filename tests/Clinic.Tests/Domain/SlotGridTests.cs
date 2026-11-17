using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Enums;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Users;

namespace Clinic.Tests.Domain;

/// <summary>
/// Slot grids. Times are written as clinic-local wall-clock (Turkey, UTC+3) through <see cref="Utc"/>,
/// so the expected values read like the schedule a patient sees. 2026-03-02 is a Monday.
/// </summary>
public class SlotGridTests
{
    private static readonly DateOnly Monday = new(2026, 3, 2);
    private static readonly DateOnly Tuesday = new(2026, 3, 3);
    private static readonly DateOnly Wednesday = new(2026, 3, 4);
    private static readonly DateOnly Saturday = new(2026, 3, 7);
    private static readonly DateOnly Sunday = new(2026, 3, 8);

    /// <summary>A "now" long before every test date, so nothing is Past unless a test says so.</summary>
    private static readonly DateTime LongAgo = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime Utc(DateOnly day, int hour, int minute = 0) =>
        ClinicTimeZone.ToUtc(day.ToDateTime(new TimeOnly(hour, minute)));

    private static ClinicFacility NewClinic(TimeOnly? opens = null, TimeOnly? closes = null, bool saturday = false)
    {
        var clinic = new ClinicFacility(Guid.NewGuid(), "Merkez", "Test Address");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            clinic.AddWorkingHour(day, opens ?? new TimeOnly(8, 0), closes ?? new TimeOnly(17, 0));
        }

        if (saturday)
        {
            clinic.AddWorkingHour(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        }

        return clinic;
    }

    private static Doctor NewDoctor(int slotMinutes) => TestData.NewDoctor(slotDurationMinutes: slotMinutes);

    private static Appointment Booking(Guid? staffId, Guid serviceId, DateTime start, DateTime end) =>
        new(Guid.NewGuid(), staffId, serviceId, start, end, 100m, AppointmentStatus.Pending);

    private static Service NewService(string name, ServiceType type, int minutes) =>
        TestData.NewDepartment("Radyoloji").AddService(name, type, TimeSpan.FromMinutes(minutes), 350m);

    private static List<DateTime> Starts(IEnumerable<TimeSlot> slots) => slots.Select(s => s.StartUtc).ToList();

    // =========================================================================================
    // Examination grid
    // =========================================================================================

    [Theory]
    [InlineData(30, 6)]
    [InlineData(20, 9)]
    [InlineData(15, 12)]
    [InlineData(10, 18)]
    [InlineData(60, 3)]
    public void Examination_SplitsAvailabilityIntoSlotsOfTheDoctorsDuration(int slotMinutes, int expectedCount)
    {
        var doctor = NewDoctor(slotMinutes);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(expectedCount, slots.Count);
        Assert.Equal(Utc(Monday, 9), slots[0].StartUtc);
        Assert.Equal(Utc(Monday, 12), slots[^1].EndUtc);
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromMinutes(slotMinutes), s.EndUtc - s.StartUtc));
        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
    }

    [Fact]
    public void Examination_SlotsAreContiguous()
    {
        var doctor = NewDoctor(20);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        for (var i = 1; i < slots.Count; i++)
        {
            Assert.Equal(slots[i - 1].EndUtc, slots[i].StartUtc);
        }
    }

    [Theory]
    [InlineData(30, 3)] // 09:00-10:50 holds three 30-minute slots; 10:30-10:50 is dropped
    [InlineData(60, 1)] // only 09:00-10:00 fits
    [InlineData(20, 5)] // 09:00-10:40 fits five; 10:40-10:50 is dropped
    public void Examination_DropsATrailingRemainderShorterThanOneSlot(int slotMinutes, int expectedCount)
    {
        var doctor = NewDoctor(slotMinutes);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 10, 50));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(expectedCount, slots.Count);
        Assert.True(slots[^1].EndUtc <= Utc(Monday, 10, 50));
    }

    [Fact]
    public void Examination_ReturnsSlotsOfEveryAvailabilityWindow_InTimeOrder()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 13), Utc(Monday, 16));
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12)); // added second, listed first

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(12, slots.Count);
        Assert.Equal(Starts(slots).Order().ToList(), Starts(slots));
        Assert.DoesNotContain(slots, s => s.StartUtc >= Utc(Monday, 12) && s.StartUtc < Utc(Monday, 13));
    }

    [Fact]
    public void Examination_KeepsOnlySlotsInsideBranchWorkingHours_AtOpening()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 7), Utc(Monday, 10)); // branch opens at 08:00

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(4, slots.Count); // 08:00, 08:30, 09:00, 09:30
        Assert.Equal(Utc(Monday, 8), slots[0].StartUtc);
    }

    [Fact]
    public void Examination_KeepsOnlySlotsInsideBranchWorkingHours_AtClosing()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 15), Utc(Monday, 18)); // branch closes at 17:00

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(4, slots.Count); // 15:00, 15:30, 16:00, 16:30
        Assert.Equal(Utc(Monday, 17), slots[^1].EndUtc);
    }

    [Fact]
    public void Examination_GridStartsAtTheWindowStart_SoASlotStraddlingOpeningIsDropped()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 7, 10), Utc(Monday, 10)); // slots 07:10, 07:40, 08:10, 08:40, 09:10...

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        // 07:40-08:10 starts before opening, so the first kept slot is 08:10; 09:40-10:10 passes the window end.
        Assert.Equal([Utc(Monday, 8, 10), Utc(Monday, 8, 40), Utc(Monday, 9, 10)], Starts(slots));
    }

    [Fact]
    public void Examination_ReadsTheDateAsClinicLocalTime_NotUtc()
    {
        var doctor = NewDoctor(30);
        // Local Tuesday 00:00 to Wednesday 00:00, which is Monday 21:00Z to Tuesday 21:00Z.
        doctor.AddAvailability(Utc(Tuesday, 0), Utc(Wednesday, 0));
        var alwaysOpen = NewClinic(new TimeOnly(0, 0), new TimeOnly(23, 59));

        var tuesday = SlotGrid.BuildExaminationSlots(doctor, alwaysOpen, Tuesday, [], LongAgo);
        var monday = SlotGrid.BuildExaminationSlots(doctor, alwaysOpen, Monday, [], LongAgo);

        Assert.Equal(new DateTime(2026, 3, 2, 21, 0, 0), tuesday[0].StartUtc); // Monday 21:00 UTC is Tuesday 00:00 local
        Assert.All(tuesday, s => Assert.Equal(Tuesday, DateOnly.FromDateTime(ClinicTimeZone.ToLocal(s.StartUtc))));
        Assert.Empty(monday);
    }

    [Fact]
    public void Examination_CutsAWindowThatSpansSeveralDays_ToTheRequestedDay()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Wednesday, 12)); // Monday 09:00 to Wednesday 12:00
        var clinic = NewClinic();

        var monday = SlotGrid.BuildExaminationSlots(doctor, clinic, Monday, [], LongAgo);
        var tuesday = SlotGrid.BuildExaminationSlots(doctor, clinic, Tuesday, [], LongAgo);
        var wednesday = SlotGrid.BuildExaminationSlots(doctor, clinic, Wednesday, [], LongAgo);

        Assert.Equal(16, monday.Count);    // 09:00-17:00
        Assert.Equal(18, tuesday.Count);   // 08:00-17:00, the whole working day
        Assert.Equal(8, wednesday.Count);  // 08:00-12:00
        Assert.Equal(Utc(Tuesday, 8), tuesday[0].StartUtc);
    }

    [Fact]
    public void Examination_ReturnsNothing_WhenTheDoctorHasNoAvailabilityThatDay()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12));

        Assert.Empty(SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Tuesday, [], LongAgo));
        Assert.Empty(SlotGrid.BuildExaminationSlots(NewDoctor(30), NewClinic(), Monday, [], LongAgo));
    }

    [Fact]
    public void Examination_ReturnsNothing_OnADayTheBranchIsClosed()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Sunday, 9), Utc(Sunday, 12));

        Assert.Empty(SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Sunday, [], LongAgo));
    }

    [Fact]
    public void Examination_UsesTheBranchHoursOfThatWeekday()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Saturday, 8), Utc(Saturday, 14));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(saturday: true), Saturday, [], LongAgo);

        Assert.Equal(8, slots.Count); // Saturday opens 09:00-13:00 only
        Assert.Equal(Utc(Saturday, 9), slots[0].StartUtc);
        Assert.Equal(Utc(Saturday, 13), slots[^1].EndUtc);
    }

    // ---- Examination statuses ------------------------------------------------------------

    [Fact]
    public void Examination_MarksEveryOverlappedSlotBooked_AndTheRestAvailable()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 11));
        // 09:15-09:45 touches both the 09:00 and the 09:30 slot.
        var booking = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9, 15), Utc(Monday, 9, 45));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [booking], LongAgo);

        Assert.Equal(
            [SlotStatus.Booked, SlotStatus.Booked, SlotStatus.Available, SlotStatus.Available],
            slots.Select(s => s.Status));
    }

    [Fact]
    public void Examination_BackToBackAppointmentDoesNotBookTheNeighbouringSlot()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 10));
        var booking = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9), Utc(Monday, 9, 30));

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [booking], LongAgo);

        Assert.Equal([SlotStatus.Booked, SlotStatus.Available], slots.Select(s => s.Status));
    }

    [Fact]
    public void Examination_CancelledAndRejectedAppointmentsDoNotBlockASlot()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 10));
        var cancelled = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9), Utc(Monday, 9, 30));
        cancelled.Cancel();
        var rejected = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9, 30), Utc(Monday, 10));
        rejected.Reject();

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [cancelled, rejected], LongAgo);

        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
    }

    [Fact]
    public void Examination_PendingAndConfirmedAppointmentsBothBlockASlot()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 10));
        var pending = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9), Utc(Monday, 9, 30));
        var confirmed = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9, 30), Utc(Monday, 10));
        confirmed.Confirm();

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [pending, confirmed], LongAgo);

        Assert.All(slots, s => Assert.Equal(SlotStatus.Booked, s.Status));
    }

    [Fact]
    public void Examination_MarksSlotsThatAlreadyStartedAsPast_AndAPastSlotStaysPastEvenIfBooked()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 11));
        var booking = Booking(doctor.Id, Guid.NewGuid(), Utc(Monday, 9), Utc(Monday, 9, 30)); // already in the past
        var now = Utc(Monday, 10); // exactly when the 10:00 slot starts

        var slots = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [booking], now);

        // 09:00 and 09:30 started before now; 10:00 starts exactly now and can still be chosen.
        Assert.Equal(
            [SlotStatus.Past, SlotStatus.Past, SlotStatus.Available, SlotStatus.Available],
            slots.Select(s => s.Status));
    }

    // ---- Examination alignment -----------------------------------------------------------

    [Fact]
    public void ExaminationAlignment_AcceptsStartsOnTheGrid_AndRejectsTheRest()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12));

        Assert.True(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9)));
        Assert.True(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9, 30)));
        Assert.True(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 11, 30)));
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9, 15)));
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9).AddSeconds(30)));
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9).AddMilliseconds(1)));
    }

    [Fact]
    public void ExaminationAlignment_RejectsASlotThatWouldRunPastTheWindowOrStartOutsideIt()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9), Utc(Monday, 12));

        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 11, 45))); // 11:45-12:15
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 12)));     // after the window
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 8, 30)));  // before the window
    }

    [Fact]
    public void ExaminationAlignment_IsMeasuredFromTheWindowStart()
    {
        var doctor = NewDoctor(30);
        doctor.AddAvailability(Utc(Monday, 9, 10), Utc(Monday, 12));

        Assert.True(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9, 10)));
        Assert.True(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9, 40)));
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, Utc(Monday, 9, 30)));
    }

    [Fact]
    public void ExaminationAlignment_AgreesWithTheListedSlots()
    {
        var doctor = NewDoctor(20);
        doctor.AddAvailability(Utc(Monday, 9, 5), Utc(Monday, 12));
        var listed = SlotGrid.BuildExaminationSlots(doctor, NewClinic(), Monday, [], LongAgo);

        // Every listed slot is bookable, and so is nothing between them.
        Assert.All(listed, s => Assert.True(SlotGrid.IsExaminationSlotStart(doctor, s.StartUtc)));
        Assert.False(SlotGrid.IsExaminationSlotStart(doctor, listed[0].StartUtc.AddMinutes(10)));
    }

    // =========================================================================================
    // Diagnostic grid
    // =========================================================================================

    [Theory]
    [InlineData(20, 27)] // 540 minutes / 20
    [InlineData(15, 36)]
    [InlineData(45, 12)] // 540 / 45 = 12 exactly
    public void Diagnostic_StepsByTheServicesDurationFromOpeningToClosing(int minutes, int expectedCount)
    {
        var service = NewService("Test", ServiceType.Diagnostic, minutes);

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [], LongAgo);

        Assert.Equal(expectedCount, slots.Count);
        Assert.Equal(Utc(Monday, 8), slots[0].StartUtc);
        Assert.Equal(Utc(Monday, 17), slots[^1].EndUtc);
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromMinutes(minutes), s.EndUtc - s.StartUtc));
        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
    }

    [Fact]
    public void Diagnostic_DropsTheLastSlotWhoseEndPassesClosing()
    {
        // Open 08:30-16:30 is 480 minutes; ten 45-minute slots fit (450) and the 30-minute rest is dropped.
        var service = NewService("MR", ServiceType.Diagnostic, 45);
        var clinic = NewClinic(new TimeOnly(8, 30), new TimeOnly(16, 30));

        var slots = SlotGrid.BuildDiagnosticSlots(service, clinic, Monday, [], LongAgo);

        Assert.Equal(10, slots.Count);
        Assert.Equal(Utc(Monday, 8, 30), slots[0].StartUtc);
        Assert.Equal(Utc(Monday, 16, 0), slots[^1].EndUtc);
        Assert.True(slots[^1].EndUtc <= Utc(Monday, 16, 30));
    }

    [Fact]
    public void Diagnostic_RestartsTheGridAtEachOpeningRange()
    {
        // Open 08:00-12:00 and 13:00-17:00: 45-minute slots fit 5 in each range (the 15-minute rests are dropped).
        var clinic = new ClinicFacility(Guid.NewGuid(), "Merkez", "Test Address");
        clinic.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        clinic.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(17, 0));
        var service = NewService("MR", ServiceType.Diagnostic, 45);

        var slots = SlotGrid.BuildDiagnosticSlots(service, clinic, Monday, [], LongAgo);

        Assert.Equal(10, slots.Count);
        Assert.Equal(Utc(Monday, 13), slots[5].StartUtc); // second range starts on the hour it opens
        Assert.DoesNotContain(slots, s => s.StartUtc >= Utc(Monday, 12) && s.StartUtc < Utc(Monday, 13));
    }

    [Fact]
    public void Diagnostic_ReturnsNothing_WhenTheBranchIsClosed()
    {
        var service = NewService("Test", ServiceType.Diagnostic, 20);

        Assert.Empty(SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Sunday, [], LongAgo));
        Assert.Empty(SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Saturday, [], LongAgo)); // no Saturday hours
    }

    [Fact]
    public void Diagnostic_UsesTheBranchHoursOfThatWeekday()
    {
        var service = NewService("Test", ServiceType.Diagnostic, 20);

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(saturday: true), Saturday, [], LongAgo);

        Assert.Equal(12, slots.Count); // 09:00-13:00 / 20 minutes
        Assert.Equal(Utc(Saturday, 9), slots[0].StartUtc);
    }

    [Fact]
    public void Diagnostic_ReadsTheDateAsClinicLocalTime()
    {
        var service = NewService("Test", ServiceType.Diagnostic, 20);

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [], LongAgo);

        // Local 08:00 Monday is 05:00 UTC the same day.
        Assert.Equal(new DateTime(2026, 3, 2, 5, 0, 0), slots[0].StartUtc);
    }

    // ---- Diagnostic statuses -------------------------------------------------------------

    [Fact]
    public void Diagnostic_MarksOverlappedSlotsBooked()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var booking = Booking(null, service.Id, Utc(Monday, 8, 20), Utc(Monday, 8, 40));

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [booking], LongAgo);

        Assert.Equal(SlotStatus.Available, slots[0].Status); // 08:00
        Assert.Equal(SlotStatus.Booked, slots[1].Status);    // 08:20
        Assert.Equal(SlotStatus.Available, slots[2].Status); // 08:40
        Assert.Equal(1, slots.Count(s => s.Status == SlotStatus.Booked));
    }

    [Fact]
    public void Diagnostic_CancelledAndRejectedAppointmentsDoNotBlockASlot()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var cancelled = Booking(null, service.Id, Utc(Monday, 8), Utc(Monday, 8, 20));
        cancelled.Cancel();
        var rejected = Booking(null, service.Id, Utc(Monday, 8, 20), Utc(Monday, 8, 40));
        rejected.Reject();

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [cancelled, rejected], LongAgo);

        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
    }

    [Fact]
    public void Diagnostic_MarksSlotsThatAlreadyStartedAsPast()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var booking = Booking(null, service.Id, Utc(Monday, 8), Utc(Monday, 8, 20));
        var now = Utc(Monday, 8, 40); // exactly when the third slot starts

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [booking], now);

        Assert.Equal(SlotStatus.Past, slots[0].Status);      // booked, but past wins
        Assert.Equal(SlotStatus.Past, slots[1].Status);
        Assert.Equal(SlotStatus.Available, slots[2].Status); // starts exactly now
    }

    [Fact]
    public void Diagnostic_EveryOfTheThreeStatusesCanAppearInOneDay()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var booking = Booking(null, service.Id, Utc(Monday, 9), Utc(Monday, 9, 20));

        var slots = SlotGrid.BuildDiagnosticSlots(service, NewClinic(), Monday, [booking], Utc(Monday, 8, 30));

        Assert.Contains(slots, s => s.Status == SlotStatus.Past);
        Assert.Contains(slots, s => s.Status == SlotStatus.Booked);
        Assert.Contains(slots, s => s.Status == SlotStatus.Available);
    }

    // ---- Diagnostic alignment ------------------------------------------------------------

    [Fact]
    public void DiagnosticAlignment_AcceptsStartsOnTheGrid_AndRejectsTheRest()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var clinic = NewClinic();

        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 8)));
        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 8, 40)));
        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 16, 40))); // last slot
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 8, 10)));
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 8).AddSeconds(1)));
    }

    [Fact]
    public void DiagnosticAlignment_RejectsASlotThatEndsAfterClosingOrIsOutsideHours()
    {
        var service = NewService("Röntgen", ServiceType.Diagnostic, 20);
        var clinic = NewClinic();

        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 16, 50))); // ends 17:10
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 17)));     // at closing
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 7, 40)));  // before opening
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Sunday, 9)));      // closed day
    }

    [Fact]
    public void DiagnosticAlignment_IsMeasuredFromOpeningOfTheBranch_WithTheServicesStep()
    {
        var service = NewService("MR", ServiceType.Diagnostic, 45);
        var clinic = NewClinic(new TimeOnly(8, 30), new TimeOnly(16, 30));

        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 8, 30)));
        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 9, 15)));
        Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 15, 15))); // last slot: 15:15-16:00
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 9, 0)));   // on the hour, but off the grid
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 15, 45))); // off the grid
    }

    [Fact]
    public void DiagnosticAlignment_AgreesWithTheListedSlots()
    {
        var service = NewService("MR", ServiceType.Diagnostic, 45);
        var clinic = NewClinic(new TimeOnly(8, 30), new TimeOnly(16, 30));
        var listed = SlotGrid.BuildDiagnosticSlots(service, clinic, Monday, [], LongAgo);

        Assert.All(listed, s => Assert.True(SlotGrid.IsDiagnosticSlotStart(service, clinic, s.StartUtc)));
        // The would-be eleventh slot (16:00-16:45) passes closing, so it is neither listed nor bookable.
        Assert.False(SlotGrid.IsDiagnosticSlotStart(service, clinic, Utc(Monday, 16, 0)));
    }
}
