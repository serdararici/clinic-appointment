using System.Text.Json.Serialization;
using Clinic.Domain.Enums;

namespace Clinic.Application.Appointments;

/// <summary>
/// An appointment as shown in a patient's or doctor's list: the service (name, type), the time, the
/// price and the status. <paramref name="StaffMemberId"/> is null for unassigned (diagnostic) appointments.
/// </summary>
public record AppointmentDetailDto(
    Guid Id,
    Guid PatientId,
    Guid? StaffMemberId,
    Guid ServiceId,
    string ServiceName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ServiceType ServiceType,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    decimal Price,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AppointmentStatus Status);
