using Clinic.Domain.Appointments;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.Tests.Domain;

public class AppointmentTests
{
    private static Appointment CreatePendingAppointment(DateTime start, DateTime end) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, end, price: 100m, AppointmentStatus.Pending);

    [Fact]
    public void Confirm_SetsStatusToConfirmed_WhenPending()
    {
        var start = DateTime.UtcNow;
        var appointment = CreatePendingAppointment(start, start.AddMinutes(30));

        appointment.Confirm();

        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void Appointment_CanBeUnassigned_WhenNoStaffMemberIsChosen()
    {
        // Diagnostic services (X-ray, MRI, blood test) are booked without picking a staff member.
        var start = DateTime.UtcNow;

        var appointment = new Appointment(Guid.NewGuid(), null, Guid.NewGuid(), start, start.AddMinutes(20), 350m, AppointmentStatus.Pending);

        Assert.Null(appointment.StaffMemberId);
        appointment.Confirm();
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void Appointment_KeepsTheAssignedStaffMember()
    {
        var staffMemberId = Guid.NewGuid();
        var start = DateTime.UtcNow;

        var appointment = new Appointment(Guid.NewGuid(), staffMemberId, Guid.NewGuid(), start, start.AddMinutes(30), 450m, AppointmentStatus.Pending);

        Assert.Equal(staffMemberId, appointment.StaffMemberId);
    }

    [Fact]
    public void Confirm_Throws_WhenNotPending()
    {
        var start = DateTime.UtcNow;
        var appointment = CreatePendingAppointment(start, start.AddMinutes(30));
        appointment.Confirm();

        Assert.Throws<InvalidOperationException>(() => appointment.Confirm());
    }

    [Theory]
    [InlineData(0, 30, 15, 45, true)]   // overlapping ranges
    [InlineData(0, 30, 30, 60, false)]  // back-to-back, not overlapping
    [InlineData(0, 30, 45, 60, false)]  // disjoint ranges
    public void OverlapsWith_DetectsOverlapCorrectly(int startMin, int endMin, int otherStartMin, int otherEndMin, bool expectedOverlap)
    {
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var appointment = CreatePendingAppointment(baseTime.AddMinutes(startMin), baseTime.AddMinutes(endMin));

        var overlaps = appointment.OverlapsWith(baseTime.AddMinutes(otherStartMin), baseTime.AddMinutes(otherEndMin));

        Assert.Equal(expectedOverlap, overlaps);
    }
}
