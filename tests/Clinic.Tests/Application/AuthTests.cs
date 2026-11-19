using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Tests.Application;

internal class FakeUserRepository : FakeRepository<User>, IUserRepository
{
    // Case-insensitive like SQL Server's default collation.
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<ClinicManager>> GetClinicManagersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClinicManager>>(Items.OfType<ClinicManager>().OrderBy(m => m.FullName).ToList());
}

public class PasswordHashingTests
{
    private readonly AspNetPasswordHasher _hasher = new();

    [Fact]
    public void Hash_IsNotThePlainPassword_AndVerifiesForTheSamePassword()
    {
        var hash = _hasher.Hash("Demo123!");

        Assert.NotEqual("Demo123!", hash);
        Assert.DoesNotContain("Demo123!", hash);
        Assert.True(_hasher.Verify(hash, "Demo123!"));
    }

    [Theory]
    [InlineData("demo123!")]    // case matters
    [InlineData("Demo123")]
    [InlineData("")]
    public void Verify_IsFalse_ForAnyOtherPassword(string wrong)
    {
        Assert.False(_hasher.Verify(_hasher.Hash("Demo123!"), wrong));
    }

    [Fact]
    public void HashingTheSamePasswordTwice_GivesDifferentHashes_BecauseOfTheSalt()
    {
        var first = _hasher.Hash("Demo123!");
        var second = _hasher.Hash("Demo123!");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify(first, "Demo123!"));
        Assert.True(_hasher.Verify(second, "Demo123!"));
    }

    [Fact]
    public void Verify_IsFalse_ForAMalformedStoredHash_InsteadOfThrowing()
    {
        Assert.False(_hasher.Verify("this is not a hash", "Demo123!"));
    }

    [Fact]
    public void User_RejectsABlankPasswordHash()
    {
        var patient = new Patient("Pat Ient", "pat@example.com", "", new DateOnly(1990, 1, 1));

        Assert.Throws<ArgumentException>(() => patient.SetPasswordHash(" "));
        Assert.Null(patient.PasswordHash);
    }
}

public class JwtTokenTests
{
    private const string Key = "unit-test-signing-key-that-is-long-enough-0123456789";
    private static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly JwtOptions Options = new() { Key = Key, Issuer = "TestIssuer", Audience = "TestAudience" };

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void Token_CarriesUserRoleBranchAndOrganization_ForStaff()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var generator = new JwtTokenGenerator(Options, new FixedTimeProvider(Now));

        var issued = generator.Create(new TokenSubject(userId, "Dr. Test", "doctor@example.com", "Doctor", branchId, organizationId));
        var token = Read(issued.Token);

        Assert.Equal(userId.ToString(), token.Subject);
        Assert.Equal("Doctor", token.Claims.Single(c => c.Type == AuthClaims.Role).Value);
        Assert.Equal(branchId.ToString(), token.Claims.Single(c => c.Type == AuthClaims.BranchId).Value);
        Assert.Equal(organizationId.ToString(), token.Claims.Single(c => c.Type == AuthClaims.OrganizationId).Value);
        Assert.Equal("doctor@example.com", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
    }

    [Fact]
    public void Token_HasNoBranchOrOrganization_ForAPatient()
    {
        var generator = new JwtTokenGenerator(Options, new FixedTimeProvider(Now));

        var token = Read(generator.Create(new TokenSubject(Guid.NewGuid(), "Pat", "pat@example.com", "Patient", null, null)).Token);

        Assert.Equal("Patient", token.Claims.Single(c => c.Type == AuthClaims.Role).Value);
        Assert.DoesNotContain(token.Claims, c => c.Type == AuthClaims.BranchId);
        Assert.DoesNotContain(token.Claims, c => c.Type == AuthClaims.OrganizationId);
    }

    [Fact]
    public void Token_IsValidForEightHours()
    {
        var generator = new JwtTokenGenerator(Options, new FixedTimeProvider(Now));

        var issued = generator.Create(new TokenSubject(Guid.NewGuid(), "Pat", "pat@example.com", "Patient", null, null));
        var token = Read(issued.Token);

        Assert.Equal(Now.AddHours(8), issued.ExpiresAtUtc);
        Assert.Equal(Now.AddHours(8), token.ValidTo);
        Assert.Equal(Now, token.ValidFrom);
    }

    [Fact]
    public void Token_IsSignedWithTheConfiguredKey_AndOnlyThatKeyValidatesIt()
    {
        var token = new JwtTokenGenerator(Options, new FixedTimeProvider(Now))
            .Create(new TokenSubject(Guid.NewGuid(), "Pat", "pat@example.com", "Patient", null, null)).Token;

        TokenValidationParameters Parameters(string key) => new()
        {
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, Parameters(Key), out _); // does not throw
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token, Parameters("a-completely-different-signing-key-0123456789"), out _));
    }

    [Fact]
    public void Options_RejectAShortKeyOrAMissingIssuerOrAudience()
    {
        Assert.Throws<InvalidOperationException>(() => new JwtOptions { Key = "too-short", Issuer = "i", Audience = "a" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new JwtOptions { Key = Key, Issuer = "", Audience = "a" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new JwtOptions { Key = Key, Issuer = "i", Audience = " " }.Validate());
        new JwtOptions { Key = Key, Issuer = "i", Audience = "a" }.Validate(); // valid: does not throw
    }
}

public class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeUserRepository _users = new();
    private readonly FakeClinicFacilityRepository _clinics = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AspNetPasswordHasher _hasher = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        _auth = new AuthService(
            _users, _clinics, _hasher,
            new JwtTokenGenerator(new JwtOptions { Key = "unit-test-signing-key-that-is-long-enough-0123456789", Issuer = "i", Audience = "a" }, timeProvider),
            _unitOfWork, timeProvider);
    }

    private static RegisterRequest Registration(string email = "new@example.com", string password = "Password1", string name = "Yeni Hasta") =>
        new(name, email, password, "+90-500-000-0000", new DateOnly(1990, 5, 20));

    private User AddUserWithPassword(User user, string password = "Demo123!")
    {
        user.SetPasswordHash(_hasher.Hash(password));
        _users.Items.Add(user);
        return user;
    }

    // =========================================================================================
    // Register
    // =========================================================================================

    [Fact]
    public async Task Register_CreatesAPatient_WithAHashedPassword()
    {
        var dto = await _auth.RegisterAsync(Registration());

        Assert.Equal("Patient", dto.Role);
        var patient = Assert.IsType<Patient>(Assert.Single(_users.Items));
        Assert.Equal("new@example.com", patient.Email);
        Assert.NotNull(patient.PasswordHash);
        Assert.NotEqual("Password1", patient.PasswordHash);
        Assert.True(_hasher.Verify(patient.PasswordHash!, "Password1"));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("new@example.com")]
    [InlineData("NEW@EXAMPLE.COM")]   // emails are unique regardless of case
    public async Task Register_Throws_WhenTheEmailIsAlreadyTaken(string email)
    {
        _users.Items.Add(new Patient("Mevcut", "new@example.com", "", new DateOnly(1980, 1, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _auth.RegisterAsync(Registration(email)));

        Assert.Single(_users.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]   // seven characters
    public async Task Register_Throws_WhenThePasswordIsShorterThanEightCharacters(string password)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _auth.RegisterAsync(Registration(password: password)));

        Assert.Empty(_users.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Register_AcceptsAnEightCharacterPassword_ButNotAnAbsurdlyLongOne()
    {
        await _auth.RegisterAsync(Registration(password: "12345678"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _auth.RegisterAsync(Registration(email: "other@example.com", password: new string('a', AuthService.MaxPasswordLength + 1))));
        Assert.Single(_users.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("two words@example.com")]
    public async Task Register_Throws_ForAnInvalidEmail(string email)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _auth.RegisterAsync(Registration(email)));
        Assert.Empty(_users.Items);
    }

    [Fact]
    public async Task Register_Throws_ForABlankNameOrAnImpossibleBirthDate()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _auth.RegisterAsync(Registration(name: " ")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _auth.RegisterAsync(Registration() with { DateOfBirth = DateOnly.FromDateTime(Now).AddDays(1) }));   // in the future
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _auth.RegisterAsync(Registration() with { DateOfBirth = default }));                                   // not provided
        Assert.Empty(_users.Items);
    }

    // =========================================================================================
    // Login
    // =========================================================================================

    [Fact]
    public async Task Login_ReturnsAToken_ForAPatient_WithoutBranchOrOrganization()
    {
        var patient = AddUserWithPassword(new Patient("Ahmet Çelik", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));

        var response = await _auth.LoginAsync(new LoginRequest("ahmet@example.com", "Demo123!"));

        Assert.Equal(patient.Id, response.UserId);
        Assert.Equal("Patient", response.Role);
        Assert.Null(response.BranchId);
        Assert.Null(response.OrganizationId);
        Assert.Equal(Now.AddHours(8), response.ExpiresAtUtc);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.Equal(patient.Id.ToString(), token.Subject);
    }

    [Fact]
    public async Task Login_IsRefusedWithTheWrongPasswordAnswer_ForADeactivatedClinicManager()
    {
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");
        _clinics.Items.Add(branch);
        var manager = (ClinicManager)AddUserWithPassword(new ClinicManager("Ayşe", "ayse@example.com", "", branch.Id));

        await _auth.LoginAsync(new LoginRequest("ayse@example.com", "Demo123!")); // works while active
        manager.Deactivate();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("ayse@example.com", "Demo123!")));
    }

    [Fact]
    public async Task Login_PutsBranchAndOrganizationInTheToken_ForADoctorAndAClinicManager()
    {
        var organizationId = Guid.NewGuid();
        var branch = new ClinicFacility(organizationId, "Merkez", "Sakarya");
        var department = branch.AddDepartment("Dahiliye");
        _clinics.Items.Add(branch);
        AddUserWithPassword(new Doctor("Dr. Hakan", "hakan@example.com", "", department));
        AddUserWithPassword(new ClinicManager("Ayşe", "ayse@example.com", "", branch.Id));

        var doctor = await _auth.LoginAsync(new LoginRequest("hakan@example.com", "Demo123!"));
        var manager = await _auth.LoginAsync(new LoginRequest("ayse@example.com", "Demo123!"));

        foreach (var (response, role) in new[] { (doctor, "Doctor"), (manager, "ClinicManager") })
        {
            Assert.Equal(role, response.Role);
            Assert.Equal(branch.Id, response.BranchId);
            Assert.Equal(organizationId, response.OrganizationId);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
            Assert.Equal(role, token.Claims.Single(c => c.Type == AuthClaims.Role).Value);
            Assert.Equal(branch.Id.ToString(), token.Claims.Single(c => c.Type == AuthClaims.BranchId).Value);
            Assert.Equal(organizationId.ToString(), token.Claims.Single(c => c.Type == AuthClaims.OrganizationId).Value);
        }
    }

    [Fact]
    public async Task Login_ForASystemAdmin_HasTheSystemAdminRole_AndNoBranchOrOrganization()
    {
        AddUserWithPassword(new SystemAdmin("Sistem Yöneticisi", "admin@example.com", ""));

        var response = await _auth.LoginAsync(new LoginRequest("admin@example.com", "Demo123!"));

        Assert.Equal("SystemAdmin", response.Role);
        Assert.Null(response.BranchId);
        Assert.Null(response.OrganizationId);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.Equal("SystemAdmin", token.Claims.Single(c => c.Type == AuthClaims.Role).Value);
        Assert.DoesNotContain(token.Claims, c => c.Type == AuthClaims.BranchId);
    }

    [Fact]
    public async Task Login_IsRefusedLikeAWrongPassword_ForAHealthTechnician_EvenWithAPasswordHash()
    {
        // Technicians are staff records only: they cannot sign in (User.CanSignIn is false for them).
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");
        _clinics.Items.Add(branch);
        AddUserWithPassword(new HealthTechnician("Kemal", "kemal@example.com", "", branch.AddDepartment("Radyoloji"), "Teknikeri"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("kemal@example.com", "Demo123!")));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("kemal@example.com", "wrong")));
    }

    [Fact]
    public async Task Login_Throws_ForAWrongPassword()
    {
        AddUserWithPassword(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("ahmet@example.com", "Wrong123!")));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("ahmet@example.com", "demo123!"))); // case matters
    }

    [Fact]
    public async Task Login_Throws_ForAnUnknownEmail_WithTheSameMessageAsAWrongPassword()
    {
        AddUserWithPassword(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));

        var unknown = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("nobody@example.com", "Demo123!")));
        var wrong = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("ahmet@example.com", "Wrong123!")));

        Assert.Equal(wrong.Message, unknown.Message); // the answer does not reveal which emails have accounts
    }

    [Theory]
    [InlineData("", "Demo123!")]
    [InlineData("ahmet@example.com", "")]
    public async Task Login_Throws_ForABlankEmailOrPassword(string email, string password)
    {
        AddUserWithPassword(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest(email, password)));
    }

    [Fact]
    public async Task Login_Throws_ForADeactivatedStaffMember_WithTheSameMessageAsAWrongPassword_UntilReactivated()
    {
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");
        _clinics.Items.Add(branch);
        var doctor = (Doctor)AddUserWithPassword(new Doctor("Dr. Hakan", "hakan@example.com", "", branch.AddDepartment("Dahiliye")));
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("hakan@example.com", "Wrong123!")));

        doctor.Deactivate();
        var deactivated = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("hakan@example.com", "Demo123!")));

        Assert.Equal(wrongPassword.Message, deactivated.Message); // not distinguishable from a wrong password
        doctor.Activate();
        await _auth.LoginAsync(new LoginRequest("hakan@example.com", "Demo123!")); // works again
    }

    [Fact]
    public async Task Login_Throws_ForADeactivatedTechnician_ButPatientsAndManagersAreUnaffected()
    {
        var branch = new ClinicFacility(Guid.NewGuid(), "Merkez", "Sakarya");
        _clinics.Items.Add(branch);
        var technician = (HealthTechnician)AddUserWithPassword(new HealthTechnician("Kemal", "kemal@example.com", "", branch.AddDepartment("Radyoloji"), "Teknikeri"));
        AddUserWithPassword(new ClinicManager("Ayşe", "ayse@example.com", "", branch.Id));
        AddUserWithPassword(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));
        technician.Deactivate();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("kemal@example.com", "Demo123!")));
        await _auth.LoginAsync(new LoginRequest("ayse@example.com", "Demo123!"));
        await _auth.LoginAsync(new LoginRequest("ahmet@example.com", "Demo123!"));
    }

    [Fact]
    public async Task Login_Throws_ForAnAccountThatHasNoPasswordYet()
    {
        _users.Items.Add(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1))); // created without a hash

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("ahmet@example.com", "Demo123!")));
    }

    [Fact]
    public async Task Login_IgnoresTheCaseOfTheEmail()
    {
        AddUserWithPassword(new Patient("Ahmet", "ahmet@example.com", "", new DateOnly(1990, 1, 1)));

        var response = await _auth.LoginAsync(new LoginRequest("  AHMET@Example.com ", "Demo123!"));

        Assert.Equal("ahmet@example.com", response.Email);
    }

    [Fact]
    public async Task ARegisteredPatient_CanLogInWithTheirPassword()
    {
        await _auth.RegisterAsync(Registration(password: "MySecret99"));

        var response = await _auth.LoginAsync(new LoginRequest("new@example.com", "MySecret99"));

        Assert.Equal("Patient", response.Role);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(new LoginRequest("new@example.com", "MySecret98")));
    }
}
