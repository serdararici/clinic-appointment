using Clinic.Domain.Common;
using Clinic.Domain.Enums;

namespace Clinic.Domain.Appointments;

/// <summary>
/// A booked slot for a patient's service, with its own approval workflow.
/// <see cref="StaffMemberId"/> is the doctor for an Examination, or null (unassigned)
/// for a Diagnostic service, where no staff member is chosen. State transitions are
/// exposed as methods (not public setters) so invalid transitions cannot be expressed.
/// </summary>
public class Appointment : Entity
{
    public Guid PatientId { get; private set; }
    public Guid? StaffMemberId { get; private set; }
    public Guid ServiceId { get; private set; }
    public DateTime ScheduledStartUtc { get; private set; }
    public DateTime ScheduledEndUtc { get; private set; }
    public decimal Price { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Appointment(
        Guid patientId,
        Guid? staffMemberId,
        Guid serviceId,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc,
        decimal price,
        AppointmentStatus initialStatus)
    {
        if (scheduledEndUtc <= scheduledStartUtc)
        {
            throw new ArgumentException("End time must be after start time.", nameof(scheduledEndUtc));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        PatientId = patientId;
        StaffMemberId = staffMemberId;
        ServiceId = serviceId;
        ScheduledStartUtc = scheduledStartUtc;
        ScheduledEndUtc = scheduledEndUtc;
        Price = price;
        Status = initialStatus;
        CreatedAtUtc = DateTime.UtcNow;
    }

    protected Appointment()
    {
    }

    /// <summary>Whether the appointment still holds its time (cancelled and rejected ones release it).</summary>
    public bool IsActive => Status is not (AppointmentStatus.Cancelled or AppointmentStatus.Rejected);

    /// <summary>Whether this appointment's time range overlaps the given range.</summary>
    public bool OverlapsWith(DateTime start, DateTime end) =>
        ScheduledStartUtc < end && start < ScheduledEndUtc;

    public void Confirm()
    {
        if (Status != AppointmentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm an appointment in status '{Status}'.");
        }

        Status = AppointmentStatus.Confirmed;
    }

    public void Reject()
    {
        if (Status != AppointmentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject an appointment in status '{Status}'.");
        }

        Status = AppointmentStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Rejected or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel an appointment in status '{Status}'.");
        }

        Status = AppointmentStatus.Cancelled;
    }
}
