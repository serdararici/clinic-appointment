using Clinic.Application.Availability;
using Clinic.Domain.Appointments;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

public class DoctorAvailabilityServiceTests
{
    private static readonly DateTime Day = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeDoctorRepository _doctors = new();
    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Doctor _doctor = TestData.NewDoctor();

    private DoctorAvailabilityService CreateService()
    {
        _doctors.Items.Add(_doctor);
        return new DoctorAvailabilityService(_doctors, _appointments, _unitOfWork);
    }

    [Fact]
    public async Task AddAvailability_PersistsSlotAndReturnsIt()
    {
        var service = CreateService();

        var dto = await service.AddAvailabilityAsync(
            _doctor.Id, new AddAvailabilityRequest(Day.AddHours(9), Day.AddHours(12)));

        Assert.Equal(Day.AddHours(9), dto.StartUtc);
        Assert.Single(_doctor.AvailabilitySlots);
        Assert.Single(_doctors.AddedSlots);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddAvailability_Throws_WhenRangeOverlapsExistingOne()
    {
        var service = CreateService();
        _doctor.AddAvailability(Day.AddHours(9), Day.AddHours(12));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAvailabilityAsync(_doctor.Id, new AddAvailabilityRequest(Day.AddHours(11), Day.AddHours(13))));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddAvailability_Throws_WhenDateTimeHasNoZoneInformation()
    {
        var service = CreateService();
        var unspecified = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAvailabilityAsync(_doctor.Id, new AddAvailabilityRequest(unspecified, unspecified.AddHours(3))));
    }

    [Fact]
    public async Task AddAvailability_Throws_WhenDoctorDoesNotExist()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddAvailabilityAsync(Guid.NewGuid(), new AddAvailabilityRequest(Day.AddHours(9), Day.AddHours(12))));
    }

    [Fact]
    public async Task RemoveAvailability_DeletesSlot()
    {
        var service = CreateService();
        var slot = _doctor.AddAvailability(Day.AddHours(9), Day.AddHours(12));

        await service.RemoveAvailabilityAsync(_doctor.Id, slot.Id);

        Assert.Empty(_doctor.AvailabilitySlots);
        Assert.Single(_doctors.RemovedSlots);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemoveAvailability_Throws_WhenSlotDoesNotExist()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RemoveAvailabilityAsync(_doctor.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveAvailability_Throws_WhenSlotHasActiveAppointment()
    {
        var service = CreateService();
        var slot = _doctor.AddAvailability(Day.AddHours(9), Day.AddHours(12));
        _appointments.Items.Add(new Appointment(
            Guid.NewGuid(), _doctor.Id, Guid.NewGuid(), Day.AddHours(10), Day.AddHours(10.5), 300m, AppointmentStatus.Pending));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RemoveAvailabilityAsync(_doctor.Id, slot.Id));

        Assert.Single(_doctor.AvailabilitySlots);
    }

    [Fact]
    public async Task RemoveAvailability_Succeeds_WhenOnlyCancelledAppointmentsExist()
    {
        var service = CreateService();
        var slot = _doctor.AddAvailability(Day.AddHours(9), Day.AddHours(12));
        _appointments.Items.Add(new Appointment(
            Guid.NewGuid(), _doctor.Id, Guid.NewGuid(), Day.AddHours(10), Day.AddHours(10.5), 300m, AppointmentStatus.Cancelled));

        await service.RemoveAvailabilityAsync(_doctor.Id, slot.Id);

        Assert.Empty(_doctor.AvailabilitySlots);
    }

    [Fact]
    public async Task GetAvailability_ReturnsSlotsOrderedByStart()
    {
        var service = CreateService();
        _doctor.AddAvailability(Day.AddHours(13), Day.AddHours(16));
        _doctor.AddAvailability(Day.AddHours(9), Day.AddHours(12));

        var result = await service.GetAvailabilityAsync(_doctor.Id);

        Assert.Equal([Day.AddHours(9), Day.AddHours(13)], result.Select(r => r.StartUtc));
    }
}
