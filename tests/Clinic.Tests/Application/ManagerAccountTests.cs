using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Application.Management;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

/// <summary>Clinic manager accounts as the system administrator manages them: list, edit, deactivate, activate.</summary>
public class ManagerAccountTests
{
    private readonly ManagementFixture _f = new();

    private ClinicManager AddManager(Guid branchId, string name = "Ayşe Yılmaz", string email = "ayse@example.com")
    {
        var manager = new ClinicManager(name, email, "+90-264-555-0001", branchId);
        _f.Users.Items.Add(manager);
        _f.Clinics.Managers.Add(manager);
        return manager;
    }

    [Fact]
    public async Task List_ShowsEveryManagerWithBranchAndOrganizationNames_IncludingInactiveOnes()
    {
        var organization = _f.AddOrganization("Sakarya Şehir Hastanesi");
        var branch = _f.AddBranch(organization);
        AddManager(branch.Id, "Zeynep Kaya", "zeynep@example.com");
        AddManager(branch.Id).Deactivate();

        var list = await _f.ManagerService().ListAsync();

        Assert.Equal(["Ayşe Yılmaz", "Zeynep Kaya"], list.Select(m => m.FullName)); // by name
        Assert.All(list, m => Assert.Equal("Sakarya Şehir Hastanesi", m.OrganizationName));
        Assert.All(list, m => Assert.Equal(branch.Name, m.BranchName));
        Assert.False(list[0].IsActive);
        Assert.True(list[1].IsActive);
    }

    [Fact]
    public async Task Update_ChangesNameEmailAndPhone_ButNotToAnEmailOfSomeoneElse()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var manager = AddManager(branch.Id);
        _f.Users.Items.Add(new Patient("Var Olan", "var@example.com", "", new DateOnly(1990, 1, 1)));

        var dto = await _f.ManagerService().UpdateAsync(manager.Id, new UpdateManagerRequest("Ayşe Demir", "ayse.demir@example.com", "+90-264-555-0002"));

        Assert.Equal("Ayşe Demir", dto.FullName);
        Assert.Equal("ayse.demir@example.com", dto.Email);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _f.ManagerService().UpdateAsync(manager.Id, new UpdateManagerRequest("Ayşe Demir", "VAR@example.com", null)));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _f.ManagerService().UpdateAsync(manager.Id, new UpdateManagerRequest("Ayşe Demir", "not-an-email", null)));
        // Keeping the own email is fine.
        await _f.ManagerService().UpdateAsync(manager.Id, new UpdateManagerRequest("Ayşe D.", "ayse.demir@example.com", null));
    }

    [Fact]
    public async Task DeactivateAndActivate_SwitchTheAccount_AndUnknownIdsAreNotFound()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var manager = AddManager(branch.Id);

        Assert.False((await _f.ManagerService().DeactivateAsync(manager.Id)).IsActive);
        Assert.False(manager.IsActive);
        Assert.True((await _f.ManagerService().ActivateAsync(manager.Id)).IsActive);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.ManagerService().DeactivateAsync(Guid.NewGuid()));
        // A user that is not a clinic manager is not found here either.
        var patient = new Patient("Hasta", "hasta@example.com", "", new DateOnly(1990, 1, 1));
        _f.Users.Items.Add(patient);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.ManagerService().DeactivateAsync(patient.Id));
    }

    [Fact]
    public async Task EveryManagerOperation_IsForTheSystemAdministratorOnly()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var manager = AddManager(branch.Id);

        foreach (var caller in new ICurrentUser[]
                 {
                     FakeCurrentUser.Manager(branch.OrganizationId, branch.Id),
                     FakeCurrentUser.Doctor(Guid.NewGuid()),
                     FakeCurrentUser.Patient(Guid.NewGuid()),
                     FakeCurrentUser.Anonymous()
                 })
        {
            _f.CurrentUser = caller;
            var service = _f.ManagerService();
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(manager.Id, new UpdateManagerRequest("X", "x@example.com", null)));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeactivateAsync(manager.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ActivateAsync(manager.Id));
        }

        Assert.True(manager.IsActive);
    }
}
