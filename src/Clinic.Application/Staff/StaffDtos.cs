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

/// <summary>The logged-in doctor's own profile: name, department (their specialty is the department name) and slot duration.</summary>
public record DoctorProfileDto(Guid Id, string FullName, Guid DepartmentId, string DepartmentName, int SlotDurationMinutes, bool AutoConfirmAppointments);

public record SetAutoConfirmRequest(bool Enabled);

/// <summary>
/// Input DTO for registering a doctor. <paramref name="Password"/> is the temporary password the doctor logs in with
/// (at least 8 characters). The slot duration defaults to 30 minutes when omitted.
/// </summary>
public record CreateDoctorRequest(string FullName, string Email, string? PhoneNumber, string Password, int? SlotDurationMinutes = null);

public record CreateTechnicianRequest(string FullName, string Email, string? PhoneNumber, string Title);

/// <summary>
/// Input DTO for editing a staff member. <paramref name="Title"/> is required for a technician and ignored
/// for a doctor (a doctor's specialty is the name of their department).
/// </summary>
public record UpdateStaffRequest(string FullName, string Email, string? PhoneNumber, string? Title = null);

/// <summary>Input DTO that replaces the set of services a staff member offers.</summary>
public record SetStaffServicesRequest(IReadOnlyList<Guid> ServiceIds);
