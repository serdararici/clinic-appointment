using Clinic.Application.Auth;
using Clinic.Application.Appointments;
using Clinic.Application.Exceptions;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>
/// Booking rules for both service types. The branch is open Monday-Friday 08:00-17:00 Turkey time
/// (UTC+3), so each UTC value is written next to its local equivalent. 2026-03-02 is a Monday and
/// 2026-03-07 a Saturday. Local Monday 08:00 is 05:00 UTC.
/// </summary>
public class AppointmentServiceTests
{
    private static readonly DateTime Monday = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Saturday = new(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>The test clock sits before every test date, so nothing is "in the past" unless a test says so.</summary>
    private static readonly DateTime LongAgo = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeDoctorRepository _doctors = new();
    private readonly FakePatientRepository _patients = new();
    private readonly FakeServiceRepository _services = new();
    private readonly FakeClinicFacilityRepository _clinics = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly HealthOrganization _organization = new("Sakarya Şehir Hastanesi");
    private readonly FakeHealthOrganizationRepository _organizations;
    private readonly ClinicFacility _clinic;
    private readonly Department _department;
    private readonly Service _checkUp;
    private readonly Service _followUp;
    private readonly Service _xRay;
    private readonly Service _mr;
    private readonly Doctor _doctor;
    private readonly Patient _patient = new("Pat Ient", "pat@example.com", "+90-500-000-0000", new DateOnly(1990, 1, 1));
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _clinic = new(_organization.Id, "Merkez", "Test Address");
        _organizations = new FakeHealthOrganizationRepository(_clinics);
        _organizations.Items.Add(_organization);
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            _clinic.AddWorkingHour(day, new TimeOnly(8, 0), new TimeOnly(17, 0));
        }

        _department = _clinic.AddDepartment("Dahiliye");
        _checkUp = _department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 300m);
        _followUp = _department.AddService("Kontrol Muayenesi", ServiceType.Examination, TimeSpan.FromMinutes(15), 200m);

        var radiology = _clinic.AddDepartment("Radyoloji");
        _xRay = radiology.AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);
        _mr = radiology.AddService("MR", ServiceType.Diagnostic, TimeSpan.FromMinutes(45), 1800m);

        _doctor = NewDoctor(slotDurationMinutes: 30);
        // Available all week (UTC), so tests that fail on working hours are not masked by availability.
        _doctor.AddAvailability(Monday, Monday.AddDays(7));

        _doctors.Items.Add(_doctor);
        _patients.Items.Add(_patient);
        _services.Items.AddRange([_checkUp, _followUp, _xRay, _mr]);
        _clinics.Items.Add(_clinic);

        _service = CreateService(new StandardPricingStrategy(), new AutoConfirmationPolicy());
    }

    private Doctor NewDoctor(int slotDurationMinutes, string email = "doctor@example.com")
    {
        var doctor = new Doctor("Dr. Test", email, "+90-500-000-0001", _department, slotDurationMinutes);
        doctor.AddService(_checkUp);
        return doctor;
    }

    private AppointmentService CreateService(IPricingStrategy pricing, IConfirmationPolicy policy, DateTime? now = null, ICurrentUser? user = null) =>
        new(_appointments, _doctors, _patients, _services, _clinics, _organizations, pricing, policy, _unitOfWork,
            new FixedTimeProvider(now ?? LongAgo),
            new AccessGuard(
                user ?? FakeCurrentUser.Patient(_patient.Id),
                _clinics,
                new FakeDepartmentRepository(_services, new FakeStaffMemberRepository())));

    /// <summary>The service as seen by a given caller (the default strategies: standard price, auto-confirm).</summary>
    private AppointmentService ServiceFor(ICurrentUser user) =>
        CreateService(new StandardPricingStrategy(), new AutoConfirmationPolicy(), user: user);

    private Task<AppointmentDto> BookExaminationAsync(DateTime startUtc, Doctor? doctor = null, Service? service = null) =>
        _service.CreateAppointmentAsync(new CreateAppointmentRequest((service ?? _checkUp).Id, startUtc, (doctor ?? _doctor).Id));

    private Task<AppointmentDto> BookDiagnosticAsync(Service service, DateTime startUtc, Guid? doctorId = null) =>
        _service.CreateAppointmentAsync(new CreateAppointmentRequest(service.Id, startUtc, doctorId));

    // =========================================================================================
    // Examination
    // =========================================================================================

    [Theory]
    [InlineData(6, 0)]   // 09:00 local, mid-morning
    [InlineData(5, 0)]   // 08:00 local, starts exactly at opening
    [InlineData(13, 30)] // 16:30-17:00 local, ends exactly at closing
    public async Task Examination_Succeeds_WhenSlotIsInsideWorkingHours(int hourUtc, int minuteUtc)
    {
        var start = Monday.AddHours(hourUtc).AddMinutes(minuteUtc);

        var dto = await BookExaminationAsync(start);

        Assert.Equal(start, dto.ScheduledStartUtc);
        Assert.Equal(start.AddMinutes(30), dto.ScheduledEndUtc);
        Assert.Equal(_doctor.Id, dto.StaffMemberId);
        Assert.Equal(300m, dto.Price);
        Assert.Equal(AppointmentStatus.Confirmed, dto.Status);
        Assert.Single(_appointments.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Examination_OccupiesExactlyOneSlot_OfTheDoctorsSlotDuration()
    {
        var quickDoctor = NewDoctor(slotDurationMinutes: 20, email: "quick@example.com");
        quickDoctor.AddAvailability(Monday, Monday.AddDays(7));
        _doctors.Items.Add(quickDoctor);

        var dto = await BookExaminationAsync(Monday.AddHours(6), quickDoctor);

        // The service is defined as 30 minutes, but an examination lasts one slot of this doctor.
        Assert.Equal(Monday.AddHours(6).AddMinutes(20), dto.ScheduledEndUtc);
        Assert.Equal(300m, dto.Price);
    }

    [Theory]
    [InlineData(15, 0)]  // 18:00 local, after closing
    [InlineData(4, 30)]  // 07:30 local, before opening
    [InlineData(4, 45)]  // 07:45-08:15 local, straddles opening
    public async Task Examination_Throws_WhenSlotIsOutsideWorkingHours(int hourUtc, int minuteUtc)
    {
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() =>
            BookExaminationAsync(Monday.AddHours(hourUtc).AddMinutes(minuteUtc)));

        Assert.Empty(_appointments.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Examination_Throws_WhenSlotIsOnAClosedDay()
    {
        // Saturday 10:00 local: inside the daily hours but the clinic is closed on weekends.
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookExaminationAsync(Saturday.AddHours(7)));
    }

    [Fact]
    public async Task Examination_Throws_WhenUtcTimeIsInsideHoursButLocalTimeIsAtClosing()
    {
        // 14:00 UTC looks like mid-afternoon, but it is 17:00 in Turkey: the clinic is closing.
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookExaminationAsync(Monday.AddHours(14)));
    }

    [Fact]
    public async Task Examination_Succeeds_WhenSlotEndsExactlyAtLocalClosingTime()
    {
        // 13:30-14:00 UTC is 16:30-17:00 local: the last slot of the day.
        var dto = await BookExaminationAsync(Monday.AddHours(13).AddMinutes(30));

        Assert.Equal(Monday.AddHours(14), dto.ScheduledEndUtc);
    }

    [Fact]
    public async Task Examination_ConvertsOffsetInputToUtc_BeforeChecking()
    {
        // 09:00+03:00 is 06:00 UTC. DateTimeOffset.LocalDateTime yields Kind=Local, which is
        // how System.Text.Json delivers a value that carries an offset.
        var withOffset = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(3)).LocalDateTime;

        var dto = await BookExaminationAsync(withOffset);

        Assert.Equal(Monday.AddHours(6), dto.ScheduledStartUtc);
    }

    [Fact]
    public async Task Booking_Throws_WhenTheStartTimeIsInThePast_ForBothServiceTypes()
    {
        // The clock says 10:00 local (07:00 UTC) on Monday: 09:00 local (06:00 UTC) has already started.
        var service = CreateService(new StandardPricingStrategy(), new AutoConfirmationPolicy(), now: Monday.AddHours(7));

        await Assert.ThrowsAsync<PastBookingException>(() => service.CreateAppointmentAsync(
            new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id)));
        await Assert.ThrowsAsync<PastBookingException>(() => service.CreateAppointmentAsync(
            new CreateAppointmentRequest(_xRay.Id, Monday.AddHours(6))));

        Assert.Empty(_appointments.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Booking_Allows_AStartExactlyAtNow_ButNotOneTickEarlier()
    {
        var now = Monday.AddHours(6).AddMinutes(30); // 09:30 local, exactly when a slot starts
        var service = CreateService(new StandardPricingStrategy(), new AutoConfirmationPolicy(), now);

        // A slot that starts exactly now can still be booked (the slot list shows it as Available)...
        var dto = await service.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, now, _doctor.Id));
        Assert.Equal(now, dto.ScheduledStartUtc);

        // ...but the slot before it has already started.
        await Assert.ThrowsAsync<PastBookingException>(() => service.CreateAppointmentAsync(
            new CreateAppointmentRequest(_checkUp.Id, now.AddMinutes(-30), _doctor.Id)));
    }

    [Fact]
    public async Task Booking_Throws_WhenDateTimeHasNoZoneInformation()
    {
        var unspecified = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(() => BookExaminationAsync(unspecified));
        await Assert.ThrowsAsync<ArgumentException>(() => BookDiagnosticAsync(_xRay, unspecified));

        Assert.Empty(_appointments.Items);
    }

    // ---- Slot grid alignment (examination) -----------------------------------------------

    [Theory]
    [InlineData(6, 15)]   // 15 minutes into a 30-minute grid
    [InlineData(6, 20)]   // valid for a 20-minute doctor, not for this 30-minute one
    [InlineData(6, 1)]
    public async Task Examination_Throws_WhenStartIsNotOnTheDoctorsSlotGrid(int hourUtc, int minuteUtc)
    {
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookExaminationAsync(Monday.AddHours(hourUtc).AddMinutes(minuteUtc)));

        Assert.Empty(_appointments.Items);
    }

    [Fact]
    public async Task Examination_Throws_WhenStartHasSecondsThatBreakTheGrid()
    {
        await Assert.ThrowsAsync<SlotNotAlignedException>(() => BookExaminationAsync(Monday.AddHours(6).AddSeconds(30)));
    }

    [Fact]
    public async Task Examination_GridFollowsTheDoctorsSlotDuration()
    {
        var quickDoctor = NewDoctor(slotDurationMinutes: 20, email: "quick@example.com");
        quickDoctor.AddAvailability(Monday, Monday.AddDays(7));
        _doctors.Items.Add(quickDoctor);

        // 06:20 is on a 20-minute grid; 06:10 is not.
        await BookExaminationAsync(Monday.AddHours(6).AddMinutes(20), quickDoctor);
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookExaminationAsync(Monday.AddHours(6).AddMinutes(10), quickDoctor));
    }

    [Fact]
    public async Task Examination_GridStartsAtTheAvailabilityWindowStart_NotAtTheHour()
    {
        var offsetDoctor = NewDoctor(slotDurationMinutes: 20, email: "offset@example.com");
        // Window 09:10-11:10 local, so slots are 09:10, 09:30, 09:50...
        offsetDoctor.AddAvailability(Monday.AddHours(6).AddMinutes(10), Monday.AddHours(8).AddMinutes(10));
        _doctors.Items.Add(offsetDoctor);

        await BookExaminationAsync(Monday.AddHours(6).AddMinutes(10), offsetDoctor);
        await BookExaminationAsync(Monday.AddHours(6).AddMinutes(30), offsetDoctor);
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookExaminationAsync(Monday.AddHours(6).AddMinutes(20), offsetDoctor));
    }

    // ---- Doctor, service, availability and conflicts (examination) ------------------------

    [Fact]
    public async Task Examination_Throws_WhenNoDoctorIsGiven()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6))));
    }

    [Fact]
    public async Task Examination_Throws_WhenDoctorDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), Guid.NewGuid())));
    }

    [Fact]
    public async Task Examination_Throws_WhenTheDoctorIsInactive()
    {
        _doctor.Deactivate();

        await Assert.ThrowsAsync<StaffInactiveException>(() => BookExaminationAsync(Monday.AddHours(6)));

        Assert.Empty(_appointments.Items);

        // Reactivated, the same slot can be booked again.
        _doctor.Activate();
        await BookExaminationAsync(Monday.AddHours(6));
    }

    [Fact]
    public async Task Examination_Throws_WhenDoctorDoesNotOfferTheService()
    {
        // The doctor offers Genel Muayene only, not the follow-up, even at an otherwise valid time.
        await Assert.ThrowsAsync<ServiceNotOfferedException>(() =>
            BookExaminationAsync(Monday.AddHours(6), service: _followUp));

        Assert.Empty(_appointments.Items);
    }

    [Fact]
    public async Task Examination_Throws_WhenDoctorHasNoAvailabilityForTheTime()
    {
        var busyDoctor = NewDoctor(slotDurationMinutes: 30, email: "busy@example.com");
        busyDoctor.AddAvailability(Monday.AddHours(6), Monday.AddHours(7)); // 09:00-10:00 local only
        _doctors.Items.Add(busyDoctor);

        // 11:00 local is inside clinic hours but outside this doctor's availability.
        await Assert.ThrowsAsync<NotAvailableException>(() => BookExaminationAsync(Monday.AddHours(8), busyDoctor));
    }

    [Fact]
    public async Task Examination_Throws_WhenDoctorIsAlreadyBookedInThatSlot()
    {
        await BookExaminationAsync(Monday.AddHours(6));

        await Assert.ThrowsAsync<AppointmentConflictException>(() => BookExaminationAsync(Monday.AddHours(6)));

        Assert.Single(_appointments.Items);
    }

    [Fact]
    public async Task Examination_AllowsTheNextSlot_AfterABookedOne()
    {
        await BookExaminationAsync(Monday.AddHours(6));

        var next = await BookExaminationAsync(Monday.AddHours(6).AddMinutes(30));

        Assert.Equal(Monday.AddHours(6).AddMinutes(30), next.ScheduledStartUtc);
        Assert.Equal(2, _appointments.Items.Count);
    }

    [Fact]
    public async Task Examination_SlotIsFreeAgain_AfterTheAppointmentIsCancelled()
    {
        await BookExaminationAsync(Monday.AddHours(6));
        _appointments.Items[0].Cancel();

        await BookExaminationAsync(Monday.AddHours(6));

        Assert.Equal(2, _appointments.Items.Count);
    }

    // =========================================================================================
    // Diagnostic (Röntgen 20 min, MR 45 min; opening 08:00 local = 05:00 UTC)
    // =========================================================================================

    [Fact]
    public async Task Diagnostic_Succeeds_WithNoStaff_AndTheServicesFixedDuration()
    {
        var start = Monday.AddHours(5).AddMinutes(20); // 08:20 local, second slot of the day

        var dto = await BookDiagnosticAsync(_xRay, start);

        Assert.Null(dto.StaffMemberId);
        Assert.Equal(start, dto.ScheduledStartUtc);
        Assert.Equal(start.AddMinutes(20), dto.ScheduledEndUtc);
        Assert.Equal(350m, dto.Price);
        Assert.Equal(AppointmentStatus.Confirmed, dto.Status);
        Assert.Null(_appointments.Items.Single().StaffMemberId);
    }

    [Fact]
    public async Task Diagnostic_IgnoresADoctorIfOneIsSent_AndStaysUnassigned()
    {
        var dto = await BookDiagnosticAsync(_xRay, Monday.AddHours(5), doctorId: _doctor.Id);

        Assert.Null(dto.StaffMemberId);
    }

    [Fact]
    public async Task Diagnostic_Succeeds_ForTheLastSlotEndingExactlyAtClosing()
    {
        // 13:40-14:00 UTC = 16:40-17:00 local.
        var dto = await BookDiagnosticAsync(_xRay, Monday.AddHours(13).AddMinutes(40));

        Assert.Equal(Monday.AddHours(14), dto.ScheduledEndUtc);
    }

    [Theory]
    [InlineData(5, 10)]  // 08:10 local: 10 minutes into a 20-minute grid
    [InlineData(5, 30)]
    [InlineData(5, 1)]
    public async Task Diagnostic_Throws_WhenStartIsNotOnTheServiceGrid(int hourUtc, int minuteUtc)
    {
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookDiagnosticAsync(_xRay, Monday.AddHours(hourUtc).AddMinutes(minuteUtc)));

        Assert.Empty(_appointments.Items);
    }

    [Fact]
    public async Task Diagnostic_Throws_WhenStartHasSecondsThatBreakTheGrid()
    {
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookDiagnosticAsync(_xRay, Monday.AddHours(5).AddSeconds(30)));
    }

    [Fact]
    public async Task Diagnostic_GridStepIsTheServicesOwnDuration()
    {
        // MR lasts 45 minutes: slots start at 08:00, 08:45, 09:30 local (05:00, 05:45, 06:30 UTC).
        var second = await BookDiagnosticAsync(_mr, Monday.AddHours(5).AddMinutes(45));
        Assert.Equal(Monday.AddHours(6).AddMinutes(30), second.ScheduledEndUtc);

        // 05:20 is on the 20-minute X-ray grid but not on the 45-minute MR grid.
        await Assert.ThrowsAsync<SlotNotAlignedException>(() =>
            BookDiagnosticAsync(_mr, Monday.AddHours(5).AddMinutes(20)));
    }

    [Theory]
    [InlineData(4, 40)]  // 07:40 local, before opening
    [InlineData(14, 0)]  // 17:00 local, at closing
    [InlineData(13, 50)] // 16:50-17:10 local: the end would pass closing
    public async Task Diagnostic_Throws_WhenOutsideWorkingHours(int hourUtc, int minuteUtc)
    {
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() =>
            BookDiagnosticAsync(_xRay, Monday.AddHours(hourUtc).AddMinutes(minuteUtc)));

        Assert.Empty(_appointments.Items);
    }

    [Fact]
    public async Task Diagnostic_Throws_OnAClosedDay()
    {
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() =>
            BookDiagnosticAsync(_xRay, Saturday.AddHours(7)));
    }

    [Fact]
    public async Task Diagnostic_UsesTheWorkingHoursOfTheServicesOwnBranch()
    {
        // A second branch that only opens 09:00-12:00 local, with its own Röntgen service.
        var otherBranch = new ClinicFacility(Guid.NewGuid(), "Adapazarı Şubesi", "Other Address");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            otherBranch.AddWorkingHour(day, new TimeOnly(9, 0), new TimeOnly(12, 0));
        }

        var otherXRay = otherBranch.AddDepartment("Radyoloji").AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 300m);
        _clinics.Items.Add(otherBranch);
        _services.Items.Add(otherXRay);

        // 08:00 local is open at the first branch but closed at this one.
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5));
        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookDiagnosticAsync(otherXRay, Monday.AddHours(5)));

        // This branch's grid starts at its own opening: 09:00 local = 06:00 UTC.
        var dto = await BookDiagnosticAsync(otherXRay, Monday.AddHours(6));
        Assert.Equal(300m, dto.Price);
    }

    // ---- Overlap rule: same service in the same branch -----------------------------------

    [Fact]
    public async Task Diagnostic_Throws_WhenSameServiceIsAlreadyBookedInThatSlot()
    {
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5).AddMinutes(20));

        await Assert.ThrowsAsync<ServiceSlotConflictException>(() =>
            BookDiagnosticAsync(_xRay, Monday.AddHours(5).AddMinutes(20)));

        Assert.Single(_appointments.Items);
    }

    [Fact]
    public async Task Diagnostic_AllowsBackToBackSlots_OfTheSameService()
    {
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5));

        await BookDiagnosticAsync(_xRay, Monday.AddHours(5).AddMinutes(20));

        Assert.Equal(2, _appointments.Items.Count);
    }

    [Fact]
    public async Task Diagnostic_AllowsOverlappingTime_ForADifferentService()
    {
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5));

        // MR at the same opening time: a different service, so it does not conflict.
        await BookDiagnosticAsync(_mr, Monday.AddHours(5));

        Assert.Equal(2, _appointments.Items.Count);
    }

    [Fact]
    public async Task Diagnostic_OverlapIsJudgedOnTheWholeTimeRange_NotJustTheStart()
    {
        // MR 08:00-08:45 and 08:45-09:30 are back to back, so both can be booked...
        await BookDiagnosticAsync(_mr, Monday.AddHours(5));
        await BookDiagnosticAsync(_mr, Monday.AddHours(5).AddMinutes(45));

        // ...but 08:00 again overlaps the first one.
        await Assert.ThrowsAsync<ServiceSlotConflictException>(() => BookDiagnosticAsync(_mr, Monday.AddHours(5)));
        Assert.Equal(2, _appointments.Items.Count);
    }

    [Fact]
    public async Task Diagnostic_SlotIsFreeAgain_AfterTheAppointmentIsCancelled()
    {
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5));
        _appointments.Items[0].Cancel();

        await BookDiagnosticAsync(_xRay, Monday.AddHours(5));

        Assert.Equal(2, _appointments.Items.Count);
    }

    // =========================================================================================
    // Cancel, and appointment lists by patient / doctor
    // =========================================================================================

    [Fact]
    public async Task Cancel_ReleasesTheSlot_AndCannotBeRepeated()
    {
        var dto = await BookExaminationAsync(Monday.AddHours(6));

        await _service.CancelAppointmentAsync(dto.Id);

        Assert.Equal(AppointmentStatus.Cancelled, _appointments.Items.Single().Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAppointmentAsync(dto.Id));
        await BookExaminationAsync(Monday.AddHours(6)); // the freed slot can be booked again
    }

    [Fact]
    public async Task Cancel_Throws_ForAnUnknownAppointment()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CancelAppointmentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Cancel_Throws_ForARejectedAppointment()
    {
        var pendingService = CreateService(new StandardPricingStrategy(), new ManagerApprovalConfirmationPolicy());
        var dto = await pendingService.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id));
        await CreateService(new StandardPricingStrategy(), new ManagerApprovalConfirmationPolicy(), user: FakeCurrentUser.Admin())
            .RejectAppointmentAsync(dto.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pendingService.CancelAppointmentAsync(dto.Id));
    }

    [Fact]
    public async Task PatientAppointments_ListServiceTimePriceAndStatus_ByStartTime_IncludingUnassignedOnes()
    {
        var examination = await BookExaminationAsync(Monday.AddHours(6));
        var diagnostic = await BookDiagnosticAsync(_xRay, Monday.AddHours(5));
        _appointments.Items.Single(a => a.Id == examination.Id).Cancel();
        // Someone else's appointment must not appear.
        _appointments.Items.Add(new Clinic.Domain.Appointments.Appointment(
            Guid.NewGuid(), _doctor.Id, _checkUp.Id, Monday.AddHours(7), Monday.AddHours(7.5), 300m, AppointmentStatus.Pending));

        var list = await _service.GetByPatientAsync(_patient.Id);

        Assert.Equal([diagnostic.Id, examination.Id], list.Select(a => a.Id)); // 08:00 local before 09:00 local

        Assert.Equal("Röntgen", list[0].ServiceName);
        Assert.Equal(ServiceType.Diagnostic, list[0].ServiceType);
        Assert.Null(list[0].StaffMemberId);
        Assert.Null(list[0].StaffMemberName);      // unassigned
        Assert.Equal("Pat Ient", list[0].PatientName);
        Assert.Equal("Sakarya Şehir Hastanesi", list[0].OrganizationName);
        Assert.Equal("Merkez", list[0].BranchName);
        Assert.Equal(350m, list[0].Price);
        Assert.Equal(AppointmentStatus.Confirmed, list[0].Status);
        Assert.Equal(Monday.AddHours(5), list[0].ScheduledStartUtc);
        Assert.Equal(DateTimeKind.Utc, list[0].ScheduledStartUtc.Kind);

        Assert.Equal("Genel Muayene", list[1].ServiceName);
        Assert.Equal(_doctor.Id, list[1].StaffMemberId);
        Assert.Equal("Dr. Test", list[1].StaffMemberName);
        Assert.Equal("Sakarya Şehir Hastanesi", list[1].OrganizationName);
        Assert.Equal("Merkez", list[1].BranchName);
        Assert.Equal(AppointmentStatus.Cancelled, list[1].Status); // every status is listed
    }

    [Fact]
    public async Task PatientAppointments_AreEmptyForAPatientWithNone_AndThrowForAnUnknownPatient()
    {
        Assert.Empty(await _service.GetByPatientAsync(_patient.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByPatientAsync(Guid.NewGuid())); // not their own id
    }

    [Fact]
    public async Task DoctorAppointments_ShowEachPatientsOwnName()
    {
        var second = new Patient("Fatma Yıldırım", "fatma@example.com", "+90-500-000-0005", new DateOnly(1985, 11, 3));
        _patients.Items.Add(second);
        await BookExaminationAsync(Monday.AddHours(6));
        await ServiceFor(FakeCurrentUser.Patient(second.Id))
            .CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6.5), _doctor.Id));

        var list = await ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).GetByDoctorAsync(_doctor.Id);

        Assert.Equal(["Pat Ient", "Fatma Yıldırım"], list.Select(a => a.PatientName));
    }

    [Fact]
    public async Task DoctorAppointments_ListOnlyThatDoctorsAppointments()
    {
        await BookExaminationAsync(Monday.AddHours(6));
        await BookDiagnosticAsync(_xRay, Monday.AddHours(5)); // unassigned: not the doctor's
        var otherDoctor = NewDoctor(slotDurationMinutes: 30, email: "other@example.com");
        otherDoctor.AddAvailability(Monday, Monday.AddDays(7));
        _doctors.Items.Add(otherDoctor);
        await BookExaminationAsync(Monday.AddHours(7), otherDoctor);

        var doctorService = ServiceFor(FakeCurrentUser.Doctor(_doctor.Id));
        var list = await doctorService.GetByDoctorAsync(_doctor.Id);

        var only = Assert.Single(list);
        Assert.Equal(_doctor.Id, only.StaffMemberId);
        Assert.Equal("Genel Muayene", only.ServiceName);
        Assert.Equal(ServiceType.Examination, only.ServiceType);
        Assert.Equal(300m, only.Price);
        Assert.Equal("Dr. Test", only.StaffMemberName);
        Assert.Equal("Pat Ient", only.PatientName);
        Assert.Equal("Sakarya Şehir Hastanesi", only.OrganizationName);
        Assert.Equal("Merkez", only.BranchName);
        await Assert.ThrowsAsync<ForbiddenException>(() => doctorService.GetByDoctorAsync(Guid.NewGuid())); // not their own id
    }

    // =========================================================================================
    // Ownership and scope (who the token says the caller is)
    // =========================================================================================

    private Patient AddOtherPatient()
    {
        var other = new Patient("Fatma Yıldırım", "fatma@example.com", "+90-500-000-0005", new DateOnly(1985, 11, 3));
        _patients.Items.Add(other);
        return other;
    }

    /// <summary>A pending appointment of the test patient with the test doctor (booked under a manager-approval policy).</summary>
    private async Task<Guid> PendingAppointmentAsync()
    {
        var pending = CreateService(new StandardPricingStrategy(), new ManagerApprovalConfirmationPolicy());
        return (await pending.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id))).Id;
    }

    private Guid OwnOrganization => _clinic.OrganizationId;

    [Fact]
    public async Task Booking_UsesThePatientFromTheToken()
    {
        var other = AddOtherPatient();

        var dto = await ServiceFor(FakeCurrentUser.Patient(other.Id))
            .CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id));

        Assert.Equal(other.Id, dto.PatientId);
        Assert.Equal(other.Id, _appointments.Items.Single().PatientId);
    }

    [Fact]
    public async Task Booking_IsRefused_ForEveryCallerThatIsNotAPatient()
    {
        ICurrentUser[] callers =
        [
            FakeCurrentUser.Anonymous(),
            FakeCurrentUser.Doctor(_doctor.Id),
            FakeCurrentUser.Manager(OwnOrganization),
            FakeCurrentUser.Admin()
        ];

        foreach (var caller in callers)
        {
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                ServiceFor(caller).CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id)));
        }

        Assert.Empty(_appointments.Items);
    }

    [Fact]
    public async Task PatientList_IsOnlyForThePatientThemselves()
    {
        var other = AddOtherPatient();
        await BookExaminationAsync(Monday.AddHours(6));

        Assert.Single(await _service.GetByPatientAsync(_patient.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Patient(other.Id)).GetByPatientAsync(_patient.Id));

        // The right id is not enough without the Patient role in the token.
        foreach (var caller in new ICurrentUser[] { FakeCurrentUser.Doctor(_patient.Id), FakeCurrentUser.Manager(OwnOrganization), FakeCurrentUser.Admin() })
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(caller).GetByPatientAsync(_patient.Id));
        }
    }

    [Fact]
    public async Task DoctorList_IsOnlyForTheDoctorThemselves()
    {
        await BookExaminationAsync(Monday.AddHours(6));
        var colleague = NewDoctor(slotDurationMinutes: 30, email: "colleague@example.com");
        _doctors.Items.Add(colleague);

        Assert.Single(await ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).GetByDoctorAsync(_doctor.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(colleague.Id)).GetByDoctorAsync(_doctor.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Patient(_patient.Id)).GetByDoctorAsync(_doctor.Id));
    }

    // ---- Cancel: the patient's own, or a manager of the appointment's organization, or an admin ----

    [Fact]
    public async Task Cancel_IsRefused_ForAnotherPatient_AndForAnotherDoctor()
    {
        var dto = await BookExaminationAsync(Monday.AddHours(6));
        var other = AddOtherPatient();
        var colleague = NewDoctor(slotDurationMinutes: 30, email: "colleague@example.com");
        _doctors.Items.Add(colleague);

        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Patient(other.Id)).CancelAppointmentAsync(dto.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(colleague.Id)).CancelAppointmentAsync(dto.Id));

        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.Single().Status);
    }

    // ---- Price preview ----

    private static readonly InsuranceDiscountPricingStrategy Discounting = new();

    [Fact]
    public async Task PricePreview_EqualsTheStoredPrice_ForAnInsuredPatient_AndCreatesNothing()
    {
        var insured = new Patient("Sigortalı Hasta", "insured@example.com", "+90-500-000-0009", new DateOnly(1980, 1, 1), insuranceNumber: "SGK-1");
        _patients.Items.Add(insured);
        var service = CreateService(Discounting, new AutoConfirmationPolicy(), user: FakeCurrentUser.Patient(insured.Id));

        var preview = await service.PreviewPriceAsync(_checkUp.Id);
        Assert.Empty(_appointments.Items); // a preview books nothing

        var booked = await service.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id));

        Assert.True(preview.HasInsurance);
        Assert.Equal(300m, preview.BasePrice);
        Assert.Equal(240m, preview.Price);
        Assert.Equal(booked.Price, preview.Price);
        Assert.Equal(booked.Price, _appointments.Items.Single().Price);
    }

    [Fact]
    public async Task PricePreview_EqualsTheStoredPrice_ForAPatientWithoutInsurance()
    {
        var service = CreateService(Discounting, new AutoConfirmationPolicy(), user: FakeCurrentUser.Patient(_patient.Id));

        var preview = await service.PreviewPriceAsync(_checkUp.Id);
        var booked = await service.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id));

        Assert.False(preview.HasInsurance);
        Assert.Equal(preview.BasePrice, preview.Price);
        Assert.Equal(booked.Price, preview.Price);
    }

    [Fact]
    public async Task PricePreview_IsForPatientsOnly_AndNeedsAKnownService()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).PreviewPriceAsync(_checkUp.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Admin()).PreviewPriceAsync(_checkUp.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.PreviewPriceAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Cancel_IsAllowed_ForTheAssignedDoctor_AndThePatientThenSeesItCancelled()
    {
        var dto = await BookExaminationAsync(Monday.AddHours(6));
        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.Single().Status);

        await ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).CancelAppointmentAsync(dto.Id);

        var list = await ServiceFor(FakeCurrentUser.Patient(_patient.Id)).GetByPatientAsync(_patient.Id);
        Assert.Equal(AppointmentStatus.Cancelled, Assert.Single(list).Status);
    }

    [Fact]
    public async Task BookingResult_SerializesTheStatusAsItsName()
    {
        var dto = await BookExaminationAsync(Monday.AddHours(6));

        var json = System.Text.Json.JsonSerializer.Serialize(dto);

        Assert.Contains("\"Status\":\"Confirmed\"", json);
    }

    [Fact]
    public async Task Cancel_IsAllowed_ForAManagerOfTheSameOrganization_AndForAnAdmin_ButNotForAnotherOrganization()
    {
        var first = await BookExaminationAsync(Monday.AddHours(6));
        var second = await BookExaminationAsync(Monday.AddHours(6).AddMinutes(30));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            ServiceFor(FakeCurrentUser.Manager(Guid.NewGuid())).CancelAppointmentAsync(first.Id)); // another organization
        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.First(a => a.Id == first.Id).Status);

        await ServiceFor(FakeCurrentUser.Manager(OwnOrganization)).CancelAppointmentAsync(first.Id);
        await ServiceFor(FakeCurrentUser.Admin()).CancelAppointmentAsync(second.Id);

        Assert.All(_appointments.Items, a => Assert.Equal(AppointmentStatus.Cancelled, a.Status));
    }

    // ---- Confirm / reject: the assigned doctor, a manager of the organization, or an admin ----

    [Fact]
    public async Task Confirm_IsAllowed_ForTheAssignedDoctor_ButNotForAnotherDoctorOrAPatient()
    {
        var id = await PendingAppointmentAsync();
        var colleague = NewDoctor(slotDurationMinutes: 30, email: "colleague@example.com");
        _doctors.Items.Add(colleague);

        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(colleague.Id)).ConfirmAppointmentAsync(id));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Patient(_patient.Id)).ConfirmAppointmentAsync(id));
        Assert.Equal(AppointmentStatus.Pending, _appointments.Items.Single().Status);

        await ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).ConfirmAppointmentAsync(id);

        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.Single().Status);
    }

    [Fact]
    public async Task Reject_IsAllowed_ForTheAssignedDoctor_ButNotForAnotherDoctor()
    {
        var id = await PendingAppointmentAsync();
        var colleague = NewDoctor(slotDurationMinutes: 30, email: "colleague@example.com");
        _doctors.Items.Add(colleague);

        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(colleague.Id)).RejectAppointmentAsync(id));

        await ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).RejectAppointmentAsync(id);

        Assert.Equal(AppointmentStatus.Rejected, _appointments.Items.Single().Status);
    }

    [Fact]
    public async Task ConfirmAndReject_AreScopedToTheOrganization_ForManagers_AndOpenToAnAdmin()
    {
        var first = await PendingAppointmentAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Manager(Guid.NewGuid())).ConfirmAppointmentAsync(first));
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Manager(Guid.NewGuid())).RejectAppointmentAsync(first));

        await ServiceFor(FakeCurrentUser.Manager(OwnOrganization)).ConfirmAppointmentAsync(first);
        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.Single().Status);

        var pending = CreateService(new StandardPricingStrategy(), new ManagerApprovalConfirmationPolicy());
        var second = await pending.CreateAppointmentAsync(new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(7), _doctor.Id));
        await ServiceFor(FakeCurrentUser.Admin()).RejectAppointmentAsync(second.Id);
        Assert.Equal(AppointmentStatus.Rejected, _appointments.Items.Single(a => a.Id == second.Id).Status);
    }

    [Fact]
    public async Task ADoctor_CannotDecideAnUnassignedDiagnosticAppointment_ButItsManagerCan()
    {
        var pending = CreateService(new StandardPricingStrategy(), new ManagerApprovalConfirmationPolicy());
        var dto = await pending.CreateAppointmentAsync(new CreateAppointmentRequest(_xRay.Id, Monday.AddHours(5)));

        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(FakeCurrentUser.Doctor(_doctor.Id)).ConfirmAppointmentAsync(dto.Id));

        await ServiceFor(FakeCurrentUser.Manager(OwnOrganization)).ConfirmAppointmentAsync(dto.Id);

        Assert.Equal(AppointmentStatus.Confirmed, _appointments.Items.Single().Status);
    }

    // =========================================================================================
    // Strategies apply to both paths
    // =========================================================================================

    [Fact]
    public async Task PricingAndConfirmationStrategies_AreUsedByBothBookingPaths()
    {
        var insured = new Patient("Ins Ured", "ins@example.com", "+90-500-000-0009", new DateOnly(1980, 1, 1), insuranceNumber: "SGK-1");
        _patients.Items.Add(insured);
        var service = CreateService(new InsuranceDiscountPricingStrategy(), new ManagerApprovalConfirmationPolicy(), user: FakeCurrentUser.Patient(insured.Id));

        var examination = await service.CreateAppointmentAsync(
            new CreateAppointmentRequest(_checkUp.Id, Monday.AddHours(6), _doctor.Id));
        var diagnostic = await service.CreateAppointmentAsync(
            new CreateAppointmentRequest(_xRay.Id, Monday.AddHours(5)));

        // 20% insurance discount, and the manager-approval policy leaves both pending.
        Assert.Equal(240m, examination.Price);
        Assert.Equal(280m, diagnostic.Price);
        Assert.Equal(AppointmentStatus.Pending, examination.Status);
        Assert.Equal(AppointmentStatus.Pending, diagnostic.Status);
    }
}
