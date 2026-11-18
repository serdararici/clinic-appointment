using Clinic.Application.Management;

namespace Clinic.Application.Staff;

/// <summary>Output DTO for a staff member. Title is set for technicians.</summary>
public record StaffDto(
    Guid Id,
    string Role,
    string FullName,
    string Email,
    string PhoneNumber,
    Guid BranchId,
    Guid DepartmentId,
    int SlotDurationMinutes,
    bool IsActive,
    string? Title,
    IReadOnlyList<Guid> ServiceIds);

/// <summary>An active doctor with the services they offer (<paramref name="DepartmentName"/> is their specialty), for choosing a doctor when booking an examination.</summary>
public record DoctorSummaryDto(Guid Id, string FullName, string DepartmentName, int SlotDurationMinutes, IReadOnlyList<ServiceDto> Services);

/// <summary>Input DTO for registering a doctor. The slot duration defaults to 30 minutes when omitted.</summary>
public record CreateDoctorRequest(string FullName, string Email, string? PhoneNumber, int? SlotDurationMinutes = null);

public record CreateTechnicianRequest(string FullName, string Email, string? PhoneNumber, string Title);

/// <summary>
/// Input DTO for editing a staff member. <paramref name="Title"/> is required for a technician and ignored
/// for a doctor (a doctor's specialty is the name of their department).
/// </summary>
public record UpdateStaffRequest(string FullName, string Email, string? PhoneNumber, string? Title = null);

/// <summary>Input DTO that replaces the set of services a staff member offers.</summary>
public record SetStaffServicesRequest(IReadOnlyList<Guid> ServiceIds);
