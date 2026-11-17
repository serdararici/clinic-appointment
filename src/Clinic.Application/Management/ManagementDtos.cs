using System.Text.Json.Serialization;
using Clinic.Domain.Enums;

namespace Clinic.Application.Management;

// Enums are written as names ("Monday", "Diagnostic") in JSON so the API is readable; numbers are still accepted on input.

public record OrganizationDto(Guid Id, string Name);

public record SaveOrganizationRequest(string Name);

public record BranchDto(Guid Id, Guid OrganizationId, string Name, string Address);

public record SaveBranchRequest(string Name, string Address);

/// <summary>An opening range of a branch on one weekday, in clinic local time ("HH:mm:ss").</summary>
public record WorkingHourDto(
    Guid Id,
    Guid BranchId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public record AddWorkingHourRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public record DepartmentDto(Guid Id, Guid BranchId, string Name);

public record SaveDepartmentRequest(string Name);

public record ServiceDto(
    Guid Id,
    Guid DepartmentId,
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ServiceType ServiceType,
    int DurationMinutes,
    decimal BasePrice);

public record SaveServiceRequest(
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ServiceType ServiceType,
    int DurationMinutes,
    decimal BasePrice);
