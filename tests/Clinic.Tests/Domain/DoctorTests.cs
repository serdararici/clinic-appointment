using Clinic.Domain.Users;
using Xunit;

namespace Clinic.Tests.Domain;

public class DoctorTests
{
    [Fact]
    public void IsAvailableDuring_ReturnsTrue_WhenRequestFitsInsideAnAvailabilitySlot()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", Guid.NewGuid(), "General Medicine");
        var start = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        doctor.AddAvailability(start, start.AddHours(3));

        var isAvailable = doctor.IsAvailableDuring(start.AddMinutes(30), start.AddHours(1));

        Assert.True(isAvailable);
    }

    [Fact]
    public void IsAvailableDuring_ReturnsFalse_WhenRequestFallsOutsideAllSlots()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", Guid.NewGuid(), "General Medicine");
        var start = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        doctor.AddAvailability(start, start.AddHours(3));

        var isAvailable = doctor.IsAvailableDuring(start.AddHours(4), start.AddHours(5));

        Assert.False(isAvailable);
    }

    [Fact]
    public void OffersService_ReturnsTrueOnlyForServicesTheDoctorProvides()
    {
        var clinicId = Guid.NewGuid();
        var department = new Clinic.Domain.Clinics.Department(clinicId, "General Medicine");
        var offered = department.AddService("Check-up", TimeSpan.FromMinutes(30), 300m);
        var notOffered = department.AddService("X-Ray", TimeSpan.FromMinutes(20), 450m);
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", clinicId, "General Medicine");
        doctor.AddService(offered);

        Assert.True(doctor.OffersService(offered.Id));
        Assert.False(doctor.OffersService(notOffered.Id));
    }

    [Fact]
    public void AddAvailability_Throws_WhenRangeOverlapsAnExistingSlot()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", Guid.NewGuid(), "General Medicine");
        var start = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        doctor.AddAvailability(start, start.AddHours(3));

        Assert.Throws<InvalidOperationException>(() => doctor.AddAvailability(start.AddHours(2), start.AddHours(4)));
        Assert.Single(doctor.AvailabilitySlots);
    }

    [Fact]
    public void AddAvailability_Allows_BackToBackRanges()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", Guid.NewGuid(), "General Medicine");
        var start = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        doctor.AddAvailability(start, start.AddHours(3));

        doctor.AddAvailability(start.AddHours(3), start.AddHours(5));

        Assert.Equal(2, doctor.AvailabilitySlots.Count);
    }

    [Fact]
    public void RemoveAvailability_RemovesTheSlot_AndReturnsNullForUnknownId()
    {
        var doctor = new Doctor("Dr. Test", "doctor@example.com", "+90-500-000-0000", Guid.NewGuid(), "General Medicine");
        var start = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        var slot = doctor.AddAvailability(start, start.AddHours(3));

        Assert.Null(doctor.RemoveAvailability(Guid.NewGuid()));
        Assert.Same(slot, doctor.RemoveAvailability(slot.Id));
        Assert.Empty(doctor.AvailabilitySlots);
    }
}
