using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Account;

/// <summary>The logged-in user's own account. <paramref name="InsuranceNumber"/> is set for patients only.</summary>
public record AccountDto(Guid Id, string FullName, string Email, string PhoneNumber, string Role, string? InsuranceNumber);

/// <summary>
/// Input DTO for editing the own profile. The email cannot be changed here, and <paramref name="InsuranceNumber"/>
/// only applies to patients (it is ignored for every other role; blank removes it).
/// </summary>
public record UpdateAccountRequest(string FullName, string? PhoneNumber, string? InsuranceNumber = null);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public interface IAccountService
{
    Task<AccountDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AccountDto> UpdateAsync(UpdateAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>400 if the current password is wrong or the new one breaks the registration rules.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// "My account" for every role. The user is always the one in the token (<see cref="ICurrentUser"/>): there is no id
/// parameter, so nobody can read or change someone else's account through it.
/// </summary>
public class AccountService(
    IUserRepository users,
    IPasswordHashing passwordHashing,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IAccountService
{
    public async Task<AccountDto> GetAsync(CancellationToken cancellationToken = default) =>
        ToDto(await LoadCurrentAsync(cancellationToken));

    public async Task<AccountDto> UpdateAsync(UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentAsync(cancellationToken);

        // The domain validates each change; nothing is saved unless all of them pass.
        user.Rename(request.FullName);
        user.ChangePhoneNumber(request.PhoneNumber);
        if (user is Patient patient)
        {
            patient.ChangeInsuranceNumber(request.InsuranceNumber);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentAsync(cancellationToken);

        // A wrong current password is a 400 with its own message, not a 401: the user is logged in, and a 401 would
        // make the app end the session.
        if (string.IsNullOrEmpty(user.PasswordHash) || !passwordHashing.Verify(user.PasswordHash, request.CurrentPassword ?? string.Empty))
        {
            throw new ArgumentException("The current password is incorrect.", nameof(request));
        }

        AuthService.ValidatePassword(request.NewPassword); // the same rules as registration

        user.SetPasswordHash(passwordHashing.Hash(request.NewPassword)); // stored hashed; nothing else changes
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> LoadCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("You must be logged in to use your account.");

        return await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {userId} was not found.");
    }

    private static AccountDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        user.RoleName,
        (user as Patient)?.InsuranceNumber);
}
