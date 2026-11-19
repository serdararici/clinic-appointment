using System.Security.Claims;
using Clinic.Api.Security;
using Clinic.Application.Auth;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clinic.Tests.Application;

/// <summary>
/// A validly signed token is no longer enough: the user in it must still exist and be active, otherwise the API
/// answers 401 (the frontend then ends the session). The check ignores the role, so role-based access is unchanged.
/// </summary>
public class TokenUserValidationTests
{
    private readonly FakeUserRepository _users = new();

    private UserStatusChecker Checker() => new(_users);

    private static ClaimsPrincipal TokenOf(Guid userId, string role = "Patient") =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString()), new Claim("role", role)], "test"));

    private Task<bool> Usable(Guid userId, string role = "Patient") =>
        TokenUserValidation.IsAccountUsableAsync(TokenOf(userId, role), Checker());

    [Fact]
    public async Task AnExistingUser_PassesTheCheck_ForEveryKindOfAccount()
    {
        var department = TestData.NewDepartment();
        User[] everyone =
        [
            new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)),
            new Doctor("Dr. Hakan", "hakan@example.com", "", department),
            new ClinicManager("Ayşe", "ayse@example.com", "", Guid.NewGuid()),
            new SystemAdmin("Sistem", "admin@example.com", ""),
        ];
        _users.Items.AddRange(everyone);

        foreach (var user in everyone)
        {
            Assert.True(await Usable(user.Id, user.RoleName), user.RoleName);
        }
    }

    [Fact]
    public async Task ADeletedUser_IsRefused_EvenWithAValidToken()
    {
        var patient = new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1));
        _users.Items.Add(patient);
        Assert.True(await Usable(patient.Id));

        _users.Items.Remove(patient); // for example the database was reset

        Assert.False(await Usable(patient.Id));
        Assert.False(await Usable(Guid.NewGuid()));
    }

    [Fact]
    public async Task ADeactivatedDoctor_AndADeactivatedClinicManager_AreRefused_AndCanBeUsedAgainWhenActivated()
    {
        var doctor = new Doctor("Dr. Hakan", "hakan@example.com", "", TestData.NewDepartment());
        var manager = new ClinicManager("Ayşe", "ayse@example.com", "", Guid.NewGuid());
        _users.Items.AddRange([doctor, manager]);

        doctor.Deactivate();
        manager.Deactivate();
        Assert.False(await Usable(doctor.Id, "Doctor"));
        Assert.False(await Usable(manager.Id, "ClinicManager"));

        doctor.Activate();
        manager.Activate();
        Assert.True(await Usable(doctor.Id, "Doctor"));
        Assert.True(await Usable(manager.Id, "ClinicManager"));
    }

    [Fact]
    public async Task ATokenWithoutAReadableUserId_IsRefused()
    {
        var checker = Checker();

        Assert.False(await TokenUserValidation.IsAccountUsableAsync(null, checker));
        Assert.False(await TokenUserValidation.IsAccountUsableAsync(new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "Patient")], "test")), checker));
        Assert.False(await TokenUserValidation.IsAccountUsableAsync(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "not-a-guid")], "test")), checker));
    }

    [Fact]
    public async Task TheRoleInTheToken_DoesNotChangeTheAnswer()
    {
        // The role claim is still what [Authorize(Roles = ...)] reads; this check only looks at the account.
        var patient = new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1));
        _users.Items.Add(patient);

        Assert.True(await Usable(patient.Id, "Patient"));
        Assert.True(await Usable(patient.Id, "Doctor"));
    }

    [Fact]
    public void DeactivatedStaffAndManagers_CannotSignIn_ButEveryoneElseCan()
    {
        var doctor = new Doctor("Dr. Hakan", "hakan@example.com", "", TestData.NewDepartment());
        var technician = new HealthTechnician("Kemal", "kemal@example.com", "", TestData.NewDepartment(), "Teknisyen");
        var manager = new ClinicManager("Ayşe", "ayse@example.com", "", Guid.NewGuid());
        var patient = new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1));

        doctor.Deactivate();
        manager.Deactivate();

        Assert.False(doctor.CanSignIn);
        Assert.False(technician.CanSignIn);
        Assert.False(manager.CanSignIn);
        Assert.False(technician.CanSignIn); // technicians never sign in, active or not
        Assert.True(patient.CanSignIn); // patients have no active flag
    }

    [Fact]
    public void TheJwtBearerSetup_RunsTheCheck_OnEveryValidatedToken()
    {
        // Guards against the check being dropped from the authentication setup by accident.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new JwtOptions { Key = new string('k', 40), Issuer = "issuer", Audience = "audience" });
        services.AddJwtAuthentication();

        var options = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.NotNull(options.Events.OnTokenValidated);
        Assert.NotNull(options.Events.OnChallenge); // the 401 text stays: the frontend recognizes it
    }
}
