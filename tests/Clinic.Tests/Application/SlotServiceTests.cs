using Clinic.Application.Slots;
using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>
/// Slot listing endpoints' logic. The grid maths is covered in SlotGridTests; these tests check that the
/// service loads the right data (doctor, service, branch, active appointments) and applies the clock.
/// 2026-03-02 is a Monday; local time is UTC+3.
/// </summary>
public class SlotServiceTests
{
    private static readonly DateOnly Monday = new(2026, 3, 2);
    private static readonly DateTime MondayUtc = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LongAgo = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeDoctorRepository _doctors = new();
    private readonly FakeServiceRepository _services = new();
    private readonly FakeClinicFacilityRepository _clinics = new();
    private readonly FakeAppointmentRepository _appointments = new();

    private readonly ClinicFacility _clinic = new(Guid.NewGuid(), "Merkez", "Test Address");
    private readonly Service _checkUp;
    private readonly Service _xRay;
    private readonly Doctor _doctor;

    public SlotServiceTests()
    {
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            _clinic.AddWorkingHour(day, new TimeOnly(8, 0), new TimeOnly(17, 0));
        }

        var department = _clinic.AddDepartment("Dahiliye");
        _checkUp = department.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 450m);
        _xRay = _clinic.AddDepartment("Radyoloji").AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);

        _doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0001", department, 30);
        _doctor.AddService(_checkUp);
        _doctor.AddAvailability(MondayUtc.AddHours(6), MondayUtc.AddHours(9)); // 09:00-12:00 local

        _doctors.Items.Add(_doctor);
        _services.Items.AddRange([_checkUp, _xRay]);
        _clinics.Items.Add(_clinic);
    }

    private SlotService CreateService(DateTime? now = null) =>
        new(_doctors, _services, _clinics, _appointments, new FixedTimeProvider(now ?? LongAgo));

    private Appointment Booking(Guid? staffId, Guid serviceId, DateTime start, DateTime end) =>
        new(Guid.NewGuid(), staffId, serviceId, start, end, 100m, AppointmentStatus.Pending);

    // ---- Examination slots ---------------------------------------------------------------

    [Fact]
    public async Task ExaminationSlots_ListsAllSlotsOfTheDoctorsDay_AsAvailable()
    {
        var slots = await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday);

        Assert.Equal(6, slots.Count);
        Assert.Equal(MondayUtc.AddHours(6), slots[0].StartUtc);   // 09:00 local
        Assert.Equal(MondayUtc.AddHours(6).AddMinutes(30), slots[0].EndUtc);
        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
        Assert.All(slots, s => Assert.Equal(DateTimeKind.Utc, s.StartUtc.Kind)); // serializes with a 'Z'
    }

    [Fact]
    public async Task ExaminationSlots_MarksBookedSlots_OfThisDoctorOnly()
    {
        _appointments.Items.Add(Booking(_doctor.Id, _checkUp.Id, MondayUtc.AddHours(6), MondayUtc.AddHours(6.5)));
        _appointments.Items.Add(Booking(Guid.NewGuid(), _checkUp.Id, MondayUtc.AddHours(7), MondayUtc.AddHours(7.5))); // another doctor

        var slots = await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday);

        Assert.Equal(SlotStatus.Booked, slots[0].Status);
        Assert.Equal(SlotStatus.Available, slots[1].Status);
        Assert.Equal(SlotStatus.Available, slots[2].Status); // 10:00 local: the other doctor's booking does not matter
        Assert.Equal(1, slots.Count(s => s.Status == SlotStatus.Booked));
    }

    [Fact]
    public async Task ExaminationSlots_IgnoreCancelledAndRejectedAppointments()
    {
        var cancelled = Booking(_doctor.Id, _checkUp.Id, MondayUtc.AddHours(6), MondayUtc.AddHours(6.5));
        cancelled.Cancel();
        var rejected = Booking(_doctor.Id, _checkUp.Id, MondayUtc.AddHours(6.5), MondayUtc.AddHours(7));
        rejected.Reject();
        _appointments.Items.AddRange([cancelled, rejected]);

        var slots = await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday);

        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
    }

    [Fact]
    public async Task ExaminationSlots_UseTheClockToMarkPastSlots()
    {
        var now = MondayUtc.AddHours(7); // 10:00 local

        var slots = await CreateService(now).GetExaminationSlotsAsync(_doctor.Id, Monday);

        Assert.Equal(
            [SlotStatus.Past, SlotStatus.Past, SlotStatus.Available, SlotStatus.Available, SlotStatus.Available, SlotStatus.Available],
            slots.Select(s => s.Status));
    }

    [Fact]
    public async Task ExaminationSlots_AreEmpty_ForAnInactiveDoctor()
    {
        _doctor.Deactivate();

        var slots = await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday);

        Assert.Empty(slots);

        _doctor.Activate();
        Assert.NotEmpty(await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday));
    }

    [Fact]
    public async Task ExaminationSlots_AreEmpty_OnADayWithoutAvailability()
    {
        var slots = await CreateService().GetExaminationSlotsAsync(_doctor.Id, Monday.AddDays(1));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task ExaminationSlots_Throw_WhenDoctorDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateService().GetExaminationSlotsAsync(Guid.NewGuid(), Monday));
    }

    // ---- Diagnostic slots ----------------------------------------------------------------

    [Fact]
    public async Task DiagnosticSlots_ListsTheBranchDayInTheServicesStep()
    {
        var slots = await CreateService().GetDiagnosticSlotsAsync(_xRay.Id, Monday);

        Assert.Equal(27, slots.Count);                              // 08:00-17:00 in 20-minute steps
        Assert.Equal(MondayUtc.AddHours(5), slots[0].StartUtc);    // 08:00 local
        Assert.Equal(MondayUtc.AddHours(14), slots[^1].EndUtc);    // 17:00 local
        Assert.All(slots, s => Assert.Equal(SlotStatus.Available, s.Status));
        Assert.All(slots, s => Assert.Equal(DateTimeKind.Utc, s.StartUtc.Kind));
    }

    [Fact]
    public async Task DiagnosticSlots_MarkBookedSlots_ForThisServiceOnly()
    {
        var mr = _clinic.AddDepartment("Görüntüleme").AddService("MR", ServiceType.Diagnostic, TimeSpan.FromMinutes(45), 1800m);
        _services.Items.Add(mr);
        _appointments.Items.Add(Booking(null, _xRay.Id, MondayUtc.AddHours(5).AddMinutes(20), MondayUtc.AddHours(5).AddMinutes(40)));
        _appointments.Items.Add(Booking(null, mr.Id, MondayUtc.AddHours(5), MondayUtc.AddHours(5).AddMinutes(45))); // other service

        var slots = await CreateService().GetDiagnosticSlotsAsync(_xRay.Id, Monday);

        Assert.Equal(SlotStatus.Available, slots[0].Status);
        Assert.Equal(SlotStatus.Booked, slots[1].Status);
        Assert.Equal(1, slots.Count(s => s.Status == SlotStatus.Booked));
    }

    [Fact]
    public async Task DiagnosticSlots_UseTheClockToMarkPastSlots()
    {
        var now = MondayUtc.AddHours(5).AddMinutes(40); // 08:40 local

        var slots = await CreateService(now).GetDiagnosticSlotsAsync(_xRay.Id, Monday);

        Assert.Equal(SlotStatus.Past, slots[0].Status);
        Assert.Equal(SlotStatus.Past, slots[1].Status);
        Assert.Equal(SlotStatus.Available, slots[2].Status);
    }

    [Fact]
    public async Task DiagnosticSlots_UseTheHoursOfTheBranchThatOwnsTheService()
    {
        var otherBranch = new ClinicFacility(Guid.NewGuid(), "Adapazarı Şubesi", "Other Address");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            otherBranch.AddWorkingHour(day, new TimeOnly(9, 0), new TimeOnly(12, 0));
        }

        var otherXRay = otherBranch.AddDepartment("Radyoloji").AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 300m);
        _clinics.Items.Add(otherBranch);
        _services.Items.Add(otherXRay);

        var slots = await CreateService().GetDiagnosticSlotsAsync(otherXRay.Id, Monday);

        Assert.Equal(9, slots.Count);                            // 09:00-12:00 in 20-minute steps
        Assert.Equal(MondayUtc.AddHours(6), slots[0].StartUtc);  // 09:00 local
    }

    [Fact]
    public async Task DiagnosticSlots_Throw_ForAnExaminationService()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().GetDiagnosticSlotsAsync(_checkUp.Id, Monday));
    }

    [Fact]
    public async Task DiagnosticSlots_Throw_WhenServiceDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateService().GetDiagnosticSlotsAsync(Guid.NewGuid(), Monday));
    }

    [Fact]
    public async Task DiagnosticSlots_Throw_WhenTheServiceHasNoBranch()
    {
        var orphan = TestData.NewDepartment("Yetim").AddService("Tetkik", ServiceType.Diagnostic, TimeSpan.FromMinutes(10), 50m);
        _services.Items.Add(orphan); // its department is not part of any registered branch

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService().GetDiagnosticSlotsAsync(orphan.Id, Monday));
    }
}
