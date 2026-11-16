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
}
