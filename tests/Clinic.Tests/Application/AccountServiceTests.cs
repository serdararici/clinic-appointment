using Clinic.Application.Account;
using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Security;

namespace Clinic.Tests.Application;

/// <summary>"My account": the user always comes from the token, every role may use it.</summary>
public class AccountServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AspNetPasswordHasher _hasher = new();

    private AccountService ServiceFor(User? user, string? role = null) =>
        new(_users, _hasher, _unitOfWork, new FakeCurrentUser { UserId = user?.Id, Role = role ?? user?.RoleName });

    private T Add<T>(T user, string password = "Eski1234!") where T : User
    {
        user.SetPasswordHash(_hasher.Hash(password));
        _users.Items.Add(user);
        return user;
    }

    private Patient AddPatient(string? insurance = "SGK-1") =>
        Add(new Patient("Ahmet Çelik", "ahmet@example.com", "+90-532-000-0001", new DateOnly(1990, 5, 20), insurance));

    // ---- Every role may read its own account ----

    [Fact]
    public async Task Get_ReturnsTheOwnAccount_ForEveryRole()
    {
        var department = TestData.NewDepartment();
        User[] everyone =
        [
            AddPatient(),
            Add(new Doctor("Dr. Hakan", "hakan@example.com", "+90-1", department)),
            Add(new HealthTechnician("Kemal", "kemal@example.com", "+90-2", department, "Radyoloji Teknikeri")),
            Add(new ClinicManager("Ayşe", "ayse@example.com", "+90-3", Guid.NewGuid())),
            Add(new SystemAdmin("Sistem Yöneticisi", "admin@example.com", "+90-4")),
        ];

        foreach (var user in everyone)
        {
            var account = await ServiceFor(user).GetAsync();

            Assert.Equal(user.Id, account.Id);
            Assert.Equal(user.FullName, account.FullName);
            Assert.Equal(user.Email, account.Email);
            Assert.Equal(user.PhoneNumber, account.PhoneNumber);
            Assert.Equal(user.RoleName, account.Role);
        }
    }

    [Fact]
    public async Task Get_ShowsTheInsuranceNumber_ForPatientsOnly()
    {
        var patient = AddPatient("SGK-12345");
        var doctor = Add(new Doctor("Dr. Hakan", "hakan@example.com", "", TestData.NewDepartment()));

        Assert.Equal("SGK-12345", (await ServiceFor(patient).GetAsync()).InsuranceNumber);
        Assert.Null((await ServiceFor(doctor).GetAsync()).InsuranceNumber);
    }

    [Fact]
    public async Task Get_NeedsALoggedInUser_AndAKnownOne()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceFor(null, role: null).GetAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => ServiceFor(new Patient("Yok", "yok@example.com", "", new DateOnly(1990, 1, 1))).GetAsync());
    }

    // ---- Edit the profile ----

    [Fact]
    public async Task Update_ChangesNamePhoneAndInsurance_OfAPatient_ButNeverTheEmail()
    {
        var patient = AddPatient();

        var account = await ServiceFor(patient).UpdateAsync(new UpdateAccountRequest("  Ahmet Yılmaz ", " +90-532-000-0009 ", " SGK-999 "));

        Assert.Equal("Ahmet Yılmaz", account.FullName);
        Assert.Equal("+90-532-000-0009", account.PhoneNumber);
        Assert.Equal("SGK-999", account.InsuranceNumber);
        Assert.Equal("ahmet@example.com", patient.Email);   // read-only
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WithABlankInsuranceNumber_RemovesIt_SoThePriceIsTheFullPrice()
    {
        var patient = AddPatient("SGK-1");

        var account = await ServiceFor(patient).UpdateAsync(new UpdateAccountRequest("Ahmet Çelik", "", "  "));

        Assert.Null(account.InsuranceNumber);
        Assert.False(patient.HasInsurance);
    }

    [Fact]
    public async Task Update_IgnoresTheInsuranceNumber_ForEveryRoleExceptPatient()
    {
        var doctor = Add(new Doctor("Dr. Hakan", "hakan@example.com", "", TestData.NewDepartment()));

        var account = await ServiceFor(doctor).UpdateAsync(new UpdateAccountRequest("Dr. Hakan Yıldız", "+90-5", "SGK-1"));

        Assert.Equal("Dr. Hakan Yıldız", account.FullName);
        Assert.Equal("+90-5", account.PhoneNumber);
        Assert.Null(account.InsuranceNumber);
    }

    [Fact]
    public async Task Update_IsAllowedForEveryRole()
    {
        var department = TestData.NewDepartment();
        User[] everyone =
        [
            AddPatient(),
            Add(new Doctor("Dr. Hakan", "hakan@example.com", "", department)),
            Add(new HealthTechnician("Kemal", "kemal@example.com", "", department, "Radyoloji Teknikeri")),
            Add(new ClinicManager("Ayşe", "ayse@example.com", "", Guid.NewGuid())),
            Add(new SystemAdmin("Sistem", "admin@example.com", "")),
        ];

        foreach (var user in everyone)
        {
            var account = await ServiceFor(user).UpdateAsync(new UpdateAccountRequest("Yeni Ad", "+90-555", null));
            Assert.Equal("Yeni Ad", account.FullName);
            Assert.Equal("+90-555", user.PhoneNumber);
        }
    }

    [Fact]
    public async Task Update_Throws_ForABlankNameOrAnOverlongPhone_AndChangesNothing()
    {
        var patient = AddPatient();
        var service = ServiceFor(patient);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(new UpdateAccountRequest(" ", "+90-1", null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(new UpdateAccountRequest("Ahmet", new string('5', 31), null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(new UpdateAccountRequest("Ahmet", "+90-1", new string('9', 51))));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // ---- Change the password ----

    [Fact]
    public async Task ChangePassword_StoresTheNewPasswordHashed_AndChangesNothingElse()
    {
        var patient = AddPatient();
        var oldHash = patient.PasswordHash;

        await ServiceFor(patient).ChangePasswordAsync(new ChangePasswordRequest("Eski1234!", "Yeni-Sifre-1"));

        Assert.NotEqual(oldHash, patient.PasswordHash);
        Assert.NotEqual("Yeni-Sifre-1", patient.PasswordHash);                   // never stored in plain text
        Assert.True(_hasher.Verify(patient.PasswordHash!, "Yeni-Sifre-1"));
        Assert.False(_hasher.Verify(patient.PasswordHash!, "Eski1234!"));
        Assert.Equal("Ahmet Çelik", patient.FullName);                           // nothing else changed
        Assert.Equal("SGK-1", patient.InsuranceNumber);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ChangePassword_WithAWrongCurrentPassword_IsA400WithAClearMessage_AndChangesNothing()
    {
        var patient = AddPatient();
        var oldHash = patient.PasswordHash;

        var error = await Assert.ThrowsAsync<ArgumentException>(() => ServiceFor(patient).ChangePasswordAsync(new ChangePasswordRequest("yanlis", "Yeni-Sifre-1")));

        Assert.Contains("current password is incorrect", error.Message);
        Assert.Equal(oldHash, patient.PasswordHash);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ChangePassword_FollowsTheRegistrationRules_ForTheNewPassword()
    {
        var patient = AddPatient();
        var service = ServiceFor(patient);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(new ChangePasswordRequest("Eski1234!", "kisa")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(new ChangePasswordRequest("Eski1234!", new string('a', AuthService.MaxPasswordLength + 1))));
        Assert.True(_hasher.Verify(patient.PasswordHash!, "Eski1234!"));
    }

    [Fact]
    public async Task ChangePassword_IsAllowedForEveryRole()
    {
        var department = TestData.NewDepartment();
        User[] everyone =
        [
            AddPatient(),
            Add(new Doctor("Dr. Hakan", "hakan@example.com", "", department)),
            Add(new HealthTechnician("Kemal", "kemal@example.com", "", department, "Radyoloji Teknikeri")),
            Add(new ClinicManager("Ayşe", "ayse@example.com", "", Guid.NewGuid())),
            Add(new SystemAdmin("Sistem", "admin@example.com", "")),
        ];

        foreach (var user in everyone)
        {
            await ServiceFor(user).ChangePasswordAsync(new ChangePasswordRequest("Eski1234!", "Yeni-Sifre-1"));
            Assert.True(_hasher.Verify(user.PasswordHash!, "Yeni-Sifre-1"));
        }
    }

    [Fact]
    public async Task OneUser_CannotTouchAnotherUsersAccount_BecauseThereIsNoIdToPass()
    {
        var first = AddPatient();
        var second = Add(new Patient("Fatma", "fatma@example.com", "", new DateOnly(1985, 1, 1)));

        await ServiceFor(first).UpdateAsync(new UpdateAccountRequest("Yeni Ad", "+90-1", null));

        Assert.Equal("Fatma", second.FullName);
    }
}
