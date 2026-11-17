using Clinic.Application.Appointments;
using Clinic.Application.Exceptions;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>
/// Booking rules. The clinic is open Monday-Friday 08:00-17:00 Turkey time (UTC+3), so the
/// UTC values below are written next to their local equivalent. 2026-03-02 is a Monday.
/// </summary>
public class AppointmentServiceTests
{
    private static readonly DateTime Monday = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Saturday = new(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ClinicFacility _clinic = new("Test Clinic", "Test Address");
    private readonly Service _checkUp;
    private readonly Doctor _doctor;
    private readonly Patient _patient = new("Pat Ient", "pat@example.com", "+90-500-000-0000", new DateOnly(1990, 1, 1));
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            _clinic.AddWorkingHour(day, new TimeOnly(8, 0), new TimeOnly(17, 0));
        }

        _checkUp = _clinic.AddDepartment("General Medicine").AddService("Check-up", TimeSpan.FromMinutes(30), 300m);

        _doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0001", _clinic.Id, "General Medicine");
        _doctor.AddService(_checkUp);
        // Available all week (UTC), so tests that fail on working hours are not masked by availability.
        _doctor.AddAvailability(Monday, Monday.AddDays(7));

        var doctors = new FakeDoctorRepository();
        doctors.Items.Add(_doctor);
        var patients = new FakePatientRepository();
        patients.Items.Add(_patient);
        var services = new FakeServiceRepository();
        services.Items.Add(_checkUp);
        var clinics = new FakeClinicFacilityRepository();
        clinics.Items.Add(_clinic);

        _service = new AppointmentService(
            _appointments, doctors, patients, services, clinics,
            new StandardPricingStrategy(), new AutoConfirmationPolicy(), _unitOfWork);
    }

    private Task<AppointmentDto> BookAsync(DateTime startUtc, Guid? serviceId = null) =>
        _service.CreateAppointmentAsync(new CreateAppointmentRequest(_patient.Id, _doctor.Id, serviceId ?? _checkUp.Id, startUtc));

    // ---- Inside working hours -------------------------------------------------------------

    [Theory]
    [InlineData(6, 0)]   // 09:00 local, mid-morning
    [InlineData(5, 0)]   // 08:00 local, starts exactly at opening
    [InlineData(13, 30)] // 16:30-17:00 local, ends exactly at closing
    public async Task Create_Succeeds_WhenAppointmentIsInsideWorkingHours(int hourUtc, int minuteUtc)
    {
        var start = Monday.AddHours(hourUtc).AddMinutes(minuteUtc);

        var dto = await BookAsync(start);

        Assert.Equal(start, dto.ScheduledStartUtc);
        Assert.Equal(start.AddMinutes(30), dto.ScheduledEndUtc);
        Assert.Equal(300m, dto.Price);
        Assert.Equal(AppointmentStatus.Confirmed, dto.Status);
        Assert.Single(_appointments.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    // ---- Outside working hours ------------------------------------------------------------

    [Theory]
    [InlineData(15, 0)]  // 18:00 local, after closing
    [InlineData(4, 30)]  // 07:30 local, before opening
    [InlineData(4, 45)]  // 07:45-08:15 local, straddles opening
    public async Task Create_Throws_WhenAppointmentIsOutsideWorkingHours(int hourUtc, int minuteUtc)
    {
        var start = Monday.AddHours(hourUtc).AddMinutes(minuteUtc);

        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookAsync(start));

        Assert.Empty(_appointments.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_Throws_WhenAppointmentIsOnAClosedDay()
    {
        // Saturday 10:00 local: inside the daily hours but the clinic is closed on weekends.
        var start = Saturday.AddHours(7);

        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookAsync(start));
    }

    // ---- UTC / local conversion at the boundary -------------------------------------------

    [Fact]
    public async Task Create_Throws_WhenUtcTimeIsInsideHoursButLocalTimeIsAtClosing()
    {
        // 14:00 UTC looks like mid-afternoon, but it is 17:00 in Turkey: the clinic is closing,
        // so a 30-minute appointment would run 17:00-17:30 local, entirely after closing.
        var start = Monday.AddHours(14);

        await Assert.ThrowsAsync<OutsideWorkingHoursException>(() => BookAsync(start));
    }

    [Fact]
    public async Task Create_Succeeds_WhenAppointmentEndsExactlyAtLocalClosingTime()
    {
        // 13:30-14:00 UTC is 16:30-17:00 local: the last slot of the day.
        var dto = await BookAsync(Monday.AddHours(13).AddMinutes(30));

        Assert.Equal(Monday.AddHours(14), dto.ScheduledEndUtc);
    }

    [Fact]
    public async Task Create_ConvertsOffsetInputToUtc_BeforeCheckingWorkingHours()
    {
        // 09:00+03:00 is 06:00 UTC. DateTimeOffset.LocalDateTime yields Kind=Local, which is
        // how System.Text.Json delivers a value that carries an offset.
        var withOffset = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(3)).LocalDateTime;

        var dto = await BookAsync(withOffset);

        Assert.Equal(Monday.AddHours(6), dto.ScheduledStartUtc);
    }

    [Fact]
    public async Task Create_Throws_WhenDateTimeHasNoZoneInformation()
    {
        var unspecified = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(() => BookAsync(unspecified));

        Assert.Empty(_appointments.Items);
    }

    // ---- Service not offered --------------------------------------------------------------

    [Fact]
    public async Task Create_Throws_WhenDoctorDoesNotOfferTheService()
    {
        var xRay = _clinic.AddDepartment("Radiology").AddService("X-Ray", TimeSpan.FromMinutes(20), 450m);
        var services = new FakeServiceRepository();
        services.Items.AddRange([_checkUp, xRay]);
        var doctors = new FakeDoctorRepository();
        doctors.Items.Add(_doctor);
        var patients = new FakePatientRepository();
        patients.Items.Add(_patient);
        var clinics = new FakeClinicFacilityRepository();
        clinics.Items.Add(_clinic);
        var service = new AppointmentService(
            _appointments, doctors, patients, services, clinics,
            new StandardPricingStrategy(), new AutoConfirmationPolicy(), _unitOfWork);

        // The doctor offers only the check-up, not the X-ray, even at an otherwise valid time.
        await Assert.ThrowsAsync<ServiceNotOfferedException>(() =>
            service.CreateAppointmentAsync(new CreateAppointmentRequest(_patient.Id, _doctor.Id, xRay.Id, Monday.AddHours(6))));

        Assert.Empty(_appointments.Items);
    }

    // ---- Existing rules still apply -------------------------------------------------------

    [Fact]
    public async Task Create_Throws_WhenDoctorHasNoAvailabilityForTheTime()
    {
        var doctor = new Doctor("Dr. Busy", "busy@example.com", "+90-500-000-0002", _clinic.Id, "General Medicine");
        doctor.AddService(_checkUp);
        doctor.AddAvailability(Monday.AddHours(6), Monday.AddHours(7)); // 09:00-10:00 local only
        var doctors = new FakeDoctorRepository();
        doctors.Items.Add(doctor);
        var patients = new FakePatientRepository();
        patients.Items.Add(_patient);
        var services = new FakeServiceRepository();
        services.Items.Add(_checkUp);
        var clinics = new FakeClinicFacilityRepository();
        clinics.Items.Add(_clinic);
        var service = new AppointmentService(
            _appointments, doctors, patients, services, clinics,
            new StandardPricingStrategy(), new AutoConfirmationPolicy(), _unitOfWork);

        // 11:00 local is inside clinic hours but outside this doctor's availability.
        await Assert.ThrowsAsync<NotAvailableException>(() =>
            service.CreateAppointmentAsync(new CreateAppointmentRequest(_patient.Id, doctor.Id, _checkUp.Id, Monday.AddHours(8))));
    }

    [Fact]
    public async Task Create_Throws_WhenDoctorIsAlreadyBookedAtThatTime()
    {
        await BookAsync(Monday.AddHours(6));

        await Assert.ThrowsAsync<AppointmentConflictException>(() => BookAsync(Monday.AddHours(6).AddMinutes(15)));

        Assert.Single(_appointments.Items);
    }
}
