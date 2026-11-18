using Clinic.Application.Exceptions;
using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Auth;

/// <summary>Role names as written in the token's "role" claim (they equal <c>User.RoleName</c>).</summary>
public static class RoleNames
{
    public const string Patient = "Patient";
    public const string Doctor = "Doctor";
    public const string HealthTechnician = "HealthTechnician";
    public const string ClinicManager = "ClinicManager";
    public const string SystemAdmin = "SystemAdmin";
}

/// <summary>The caller of the current request, read from the validated JWT (the Api layer provides it).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Role { get; }

    /// <summary>The branch in the token; set for staff and clinic managers only.</summary>
    Guid? BranchId { get; }

    /// <summary>The organization in the token; set for staff and clinic managers only.</summary>
    Guid? OrganizationId { get; }
}

/// <summary>
/// Ownership and scope rules, in one place. "May this caller act on this resource?" is answered from the ids in
/// the token: a patient only touches their own data, a doctor their own availability and appointments, a clinic
/// manager only resources of the organization in their token, and a system administrator everything.
/// A failed check throws <see cref="ForbiddenException"/> (403). Role names are checked earlier, by the controllers.
/// </summary>
public class AccessGuard(ICurrentUser user, IClinicFacilityRepository clinics, IDepartmentRepository departments)
{
    public bool IsSystemAdmin => user.Role == RoleNames.SystemAdmin;

    public bool IsClinicManager => user.Role == RoleNames.ClinicManager;

    // ---- Who the caller is ----

    /// <summary>The patient making a booking: taken from the token, never from the request.</summary>
    public Guid RequirePatientId()
    {
        if (user.Role != RoleNames.Patient || user.UserId is not { } id)
        {
            throw new ForbiddenException("Only a patient can book an appointment, and only for themselves.");
        }

        return id;
    }

    public void RequireSystemAdmin()
    {
        if (!IsSystemAdmin)
        {
            throw new ForbiddenException("Only a system administrator can do this.");
        }
    }

    // ---- Own data ----

    public void EnsureOwnPatient(Guid patientId)
    {
        if (user.Role != RoleNames.Patient || user.UserId != patientId)
        {
            throw new ForbiddenException("You can only access your own appointments.");
        }
    }

    public void EnsureOwnDoctor(Guid doctorId)
    {
        if (user.Role != RoleNames.Doctor || user.UserId != doctorId)
        {
            throw new ForbiddenException("You can only manage your own schedule and appointments.");
        }
    }

    // ---- Organization scope ----

    public void EnsureOrganization(Guid organizationId)
    {
        if (IsSystemAdmin)
        {
            return;
        }

        if (!IsClinicManager || user.OrganizationId != organizationId)
        {
            throw new ForbiddenException("This resource belongs to another organization.");
        }
    }

    public async Task EnsureBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (IsSystemAdmin)
        {
            return;
        }

        var organizationId = await clinics.GetOrganizationIdAsync(branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Branch {branchId} was not found.");
        EnsureOrganization(organizationId);
    }

    public Task EnsureDepartmentAsync(Department department, CancellationToken cancellationToken = default) =>
        EnsureBranchAsync(department.ClinicFacilityId, cancellationToken);

    public async Task EnsureServiceAsync(Service service, CancellationToken cancellationToken = default)
    {
        if (IsSystemAdmin)
        {
            return;
        }

        var department = await departments.GetByIdAsync(service.DepartmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Department {service.DepartmentId} was not found.");
        await EnsureDepartmentAsync(department, cancellationToken);
    }

    public Task EnsureStaffAsync(StaffMember staff, CancellationToken cancellationToken = default) =>
        EnsureBranchAsync(staff.ClinicFacilityId, cancellationToken);

    /// <summary>A doctor changes only their own slot duration; a manager or admin any in their scope.</summary>
    public Task EnsureCanChangeSlotDurationAsync(StaffMember staff, CancellationToken cancellationToken = default)
    {
        if (user.Role == RoleNames.Doctor)
        {
            EnsureOwnDoctor(staff.Id);
            return Task.CompletedTask;
        }

        return EnsureStaffAsync(staff, cancellationToken);
    }

    /// <summary>The branches a clinic manager may see (their organization's); a system administrator has no restriction.</summary>
    public async Task<IReadOnlyList<Guid>> OwnBranchIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsClinicManager || user.OrganizationId is not { } organizationId)
        {
            return [];
        }

        return (await clinics.GetByOrganizationIdAsync(organizationId, cancellationToken)).Select(b => b.Id).ToList();
    }

    // ---- Appointments ----

    /// <summary>Who may confirm or reject: the assigned doctor, a manager of the appointment's organization, or an admin.</summary>
    public async Task EnsureCanDecideAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        if (user.Role == RoleNames.Doctor)
        {
            if (appointment.StaffMemberId is null || appointment.StaffMemberId != user.UserId)
            {
                throw new ForbiddenException("You can only confirm or reject appointments assigned to you.");
            }

            return;
        }

        await EnsureAppointmentOrganizationAsync(appointment, cancellationToken);
    }

    /// <summary>Who may cancel: the appointment's patient, its assigned doctor, a manager of its organization, or an admin.</summary>
    public async Task EnsureCanCancelAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        if (user.Role == RoleNames.Doctor)
        {
            if (appointment.StaffMemberId is null || appointment.StaffMemberId != user.UserId)
            {
                throw new ForbiddenException("You can only cancel appointments assigned to you.");
            }

            return;
        }

        if (user.Role == RoleNames.Patient)
        {
            if (appointment.PatientId != user.UserId)
            {
                throw new ForbiddenException("You can only cancel your own appointments.");
            }

            return;
        }

        await EnsureAppointmentOrganizationAsync(appointment, cancellationToken);
    }

    // An appointment belongs to the branch that owns its service's department.
    private async Task EnsureAppointmentOrganizationAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        if (IsSystemAdmin)
        {
            return;
        }

        var branch = await clinics.GetByServiceIdAsync(appointment.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"No branch was found for the appointment's service {appointment.ServiceId}.");
        EnsureOrganization(branch.OrganizationId);
    }
}
