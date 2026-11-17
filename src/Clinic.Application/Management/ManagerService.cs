using Clinic.Application.Auth;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Management;

/// <summary>Input DTO for creating a clinic manager account for a branch.</summary>
public record CreateManagerRequest(string FullName, string Email, string Password, string? PhoneNumber);

public interface IManagerService
{
    /// <summary>System administrators only. 404 unknown branch, 400 invalid input, 409 email already used.</summary>
    Task<AuthUserDto> CreateAsync(Guid branchId, CreateManagerRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates <see cref="ClinicManager"/> accounts. Managers cannot register themselves: a system administrator
/// creates the account for a branch and hands over the initial password.
/// </summary>
public class ManagerService(
    IClinicFacilityRepository clinics,
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
}
