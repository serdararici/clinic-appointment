namespace Clinic.Application.Staff;

/// <summary>Output DTO for a staff member. Specialty is set for doctors, Title for technicians.</summary>
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
    string? Specialty,
    string? Title,
    IReadOnlyList<Guid> ServiceIds);

/// <summary>Input DTO for registering a doctor. The slot duration defaults to 30 minutes when omitted.</summary>
public record CreateDoctorRequest(string FullName, string Email, string? PhoneNumber, string Specialty, int? SlotDurationMinutes = null);

public record CreateTechnicianRequest(string FullName, string Email, string? PhoneNumber, string Title);

/// <summary>
/// Input DTO for editing a staff member. <paramref name="Specialty"/> is required for a doctor and
/// <paramref name="Title"/> for a technician; the one that does not apply to the role is ignored.
/// </summary>
public record UpdateStaffRequest(string FullName, string Email, string? PhoneNumber, string? Specialty = null, string? Title = null);

/// <summary>Input DTO that replaces the set of services a staff member offers.</summary>
public record SetStaffServicesRequest(IReadOnlyList<Guid> ServiceIds);
