using Clinic.Application.Auth;
using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Management;

/// <summary>Input DTO for creating a clinic manager account for a branch.</summary>
public record CreateManagerRequest(string FullName, string Email, string Password, string? PhoneNumber);

/// <summary>A clinic manager account with the branch and organization they work for.</summary>
public record ManagerDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    Guid BranchId,
    string BranchName,
    Guid OrganizationId,
    string OrganizationName,
    bool IsActive);

public record UpdateManagerRequest(string FullName, string Email, string? PhoneNumber);

public interface IManagerService
{
    /// <summary>System administrators only: every clinic manager, ordered by name.</summary>
    Task<IReadOnlyList<ManagerDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>System administrators only. 404 unknown manager, 400 invalid input, 409 email used by someone else.</summary>
    Task<ManagerDto> UpdateAsync(Guid managerId, UpdateManagerRequest request, CancellationToken cancellationToken = default);

    /// <summary>System administrators only: the manager can no longer log in (their history stays).</summary>
    Task<ManagerDto> DeactivateAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<ManagerDto> ActivateAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>System administrators only. 404 unknown branch, 400 invalid input, 409 email already used.</summary>
    Task<AuthUserDto> CreateAsync(Guid branchId, CreateManagerRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates <see cref="ClinicManager"/> accounts. Managers cannot register themselves: a system administrator
/// creates the account for a branch and hands over the initial password.
/// </summary>
public class ManagerService(
    IClinicFacilityRepository clinics,
    IHealthOrganizationRepository organizations,
    IUserRepository users,
    IPasswordHashing passwordHashing,
    IUnitOfWork unitOfWork,
    AccessGuard guard) : IManagerService
{
    public async Task<AuthUserDto> CreateAsync(Guid branchId, CreateManagerRequest request, CancellationToken cancellationToken = default)
    {
        guard.RequireSystemAdmin();

        var branch = await clinics.GetByIdAsync(branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Branch {branchId} was not found.");

        var email = AuthService.ValidateEmail(request.Email);
        AuthService.ValidatePassword(request.Password);

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var manager = new ClinicManager(request.FullName, email, request.PhoneNumber ?? string.Empty, branch.Id);
        manager.SetPasswordHash(passwordHashing.Hash(request.Password));

        await users.AddAsync(manager, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthUserDto(manager.Id, manager.FullName, manager.Email, manager.RoleName);
    }

    public async Task<IReadOnlyList<ManagerDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        guard.RequireSystemAdmin();

        var branches = (await clinics.GetAllAsync(cancellationToken)).ToDictionary(b => b.Id);
        var organizationNames = (await organizations.GetAllAsync(cancellationToken)).ToDictionary(o => o.Id, o => o.Name);

        return (await users.GetClinicManagersAsync(cancellationToken))
            .Select(manager => ToDto(manager, branches.GetValueOrDefault(manager.ClinicFacilityId), organizationNames))
            .ToList();
    }

    public async Task<ManagerDto> UpdateAsync(Guid managerId, UpdateManagerRequest request, CancellationToken cancellationToken = default)
    {
        guard.RequireSystemAdmin();
        var manager = await LoadAsync(managerId, cancellationToken);

        var email = AuthService.ValidateEmail(request.Email);
        if (!string.Equals(manager.Email, email, StringComparison.OrdinalIgnoreCase) && await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        manager.Rename(request.FullName);
        manager.UpdateContactInfo(email, request.PhoneNumber ?? string.Empty);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(manager, cancellationToken);
    }

    public Task<ManagerDto> DeactivateAsync(Guid managerId, CancellationToken cancellationToken = default) => SetActiveAsync(managerId, false, cancellationToken);

    public Task<ManagerDto> ActivateAsync(Guid managerId, CancellationToken cancellationToken = default) => SetActiveAsync(managerId, true, cancellationToken);

    private async Task<ManagerDto> SetActiveAsync(Guid managerId, bool active, CancellationToken cancellationToken)
    {
        guard.RequireSystemAdmin();
        var manager = await LoadAsync(managerId, cancellationToken);

        if (active)
        {
            manager.Activate();
        }
        else
        {
            manager.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(manager, cancellationToken);
    }

    private async Task<ClinicManager> LoadAsync(Guid managerId, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(managerId, cancellationToken) as ClinicManager
        ?? throw new KeyNotFoundException($"Clinic manager {managerId} was not found.");

    private async Task<ManagerDto> ToDtoAsync(ClinicManager manager, CancellationToken cancellationToken)
    {
        var branch = await clinics.GetByIdAsync(manager.ClinicFacilityId, cancellationToken);
        var organizationNames = (await organizations.GetAllAsync(cancellationToken)).ToDictionary(o => o.Id, o => o.Name);
        return ToDto(manager, branch, organizationNames);
    }

    private static ManagerDto ToDto(ClinicManager manager, ClinicFacility? branch, IReadOnlyDictionary<Guid, string> organizationNames) => new(
        manager.Id,
        manager.FullName,
        manager.Email,
        manager.PhoneNumber,
        manager.ClinicFacilityId,
        branch?.Name ?? string.Empty,
        branch?.OrganizationId ?? Guid.Empty,
        branch is null ? string.Empty : organizationNames.GetValueOrDefault(branch.OrganizationId, string.Empty),
        manager.IsActive);
}
