using System.Text.Json.Serialization;
using Clinic.Domain.Enums;

namespace Clinic.Application.Appointments;

/// <summary>
/// An appointment as shown in a patient's or doctor's list: who (patient, staff member), where (branch),
/// what (service), when, the price and the status. <paramref name="StaffMemberId"/> and
/// <paramref name="StaffMemberName"/> are null for unassigned (diagnostic) appointments.
/// </summary>
public record AppointmentDetailDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid? StaffMemberId,
    string? StaffMemberName,
    string BranchName,
    Guid ServiceId,
    string ServiceName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ServiceType ServiceType,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    decimal Price,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AppointmentStatus Status);
