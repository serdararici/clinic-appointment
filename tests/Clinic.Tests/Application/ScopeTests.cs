using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Application.Management;
using Clinic.Application.Staff;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Security;

namespace Clinic.Tests.Application;

/// <summary>
/// Management scope: a clinic manager may only act on resources of the organization in their token, a system
/// administrator on all of them, and everything else is refused with a ForbiddenException (403).
/// Two organizations, A (the manager's own) and B (someone else's), each with a branch, department, service and doctor.
/// </summary>
public class ScopeTests
{
    private readonly ManagementFixture _f = new();

    private readonly HealthOrganization _orgA;
    private readonly HealthOrganization _orgB;
    private readonly ClinicFacility _branchA;
    private readonly ClinicFacility _branchB;
    private readonly Department _deptA;
    private readonly Department _deptB;
    private readonly Service _serviceA;
    private readonly Service _serviceB;
    private readonly Doctor _doctorA;
    private readonly Doctor _doctorB;

    public ScopeTests()
    {
        _orgA = _f.AddOrganization("Sakarya Şehir Hastanesi");
        _orgB = _f.AddOrganization("Marmara Poliklinik");
        _branchA = _f.AddBranch(_orgA, "Merkez");
        _branchB = _f.AddBranch(_orgB, "Serdivan Kliniği");
        _deptA = _f.AddDepartment(_branchA);
        _deptB = _f.AddDepartment(_branchB);
        _serviceA = _f.AddService(_deptA);
        _serviceB = _f.AddService(_deptB);
        _doctorA = _f.AddDoctor(_deptA, "a@example.com", "Dr. A");
        _doctorB = _f.AddDoctor(_deptB, "b@example.com", "Dr. B");

        As(FakeCurrentUser.Manager(_orgA.Id, _branchA.Id)); // default caller: a manager of organization A
    }

    private void As(ICurrentUser user) => _f.CurrentUser = user;

    // =========================================================================================
    // Organizations
    // =========================================================================================

    [Fact]
    public async Task Organizations_AreCreatedAndDeletedOnlyByASystemAdministrator()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.OrganizationService().CreateAsync(new SaveOrganizationRequest("Yeni")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.OrganizationService().DeleteAsync(_orgA.Id));
        Assert.Equal(2, _f.Organizations.Items.Count);

        As(FakeCurrentUser.Admin());
        var created = await _f.OrganizationService().CreateAsync(new SaveOrganizationRequest("Yeni"));
        var emptyOrg = _f.AddOrganization("Bos");
        await _f.OrganizationService().DeleteAsync(emptyOrg.Id);

        Assert.Equal("Yeni", created.Name);
        Assert.DoesNotContain(emptyOrg, _f.Organizations.Items);
    }

    [Fact]
    public async Task Organization_Update_IsAllowedForTheOwnOrganization_ButNotAnotherOne()
    {
        var dto = await _f.OrganizationService().UpdateAsync(_orgA.Id, new SaveOrganizationRequest("Yeni Ad"));
        Assert.Equal("Yeni Ad", dto.Name);

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.OrganizationService().UpdateAsync(_orgB.Id, new SaveOrganizationRequest("Ele Gecirildi")));
        Assert.Equal("Marmara Poliklinik", _orgB.Name);
    }

    // =========================================================================================
    // Branches and working hours
    // =========================================================================================

    [Fact]
    public async Task Branches_CanBeCreatedUnderTheOwnOrganization_ButNotAnotherOne()
    {
        await _f.BranchService().CreateAsync(_orgA.Id, new SaveBranchRequest("Yeni Sube", "Adres"));

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().CreateAsync(_orgB.Id, new SaveBranchRequest("Sizma", "Adres")));
        Assert.Equal(3, _f.Clinics.Items.Count);
    }

    [Fact]
    public async Task Branches_CanBeUpdatedAndDeleted_OnlyWithinTheOwnOrganization()
    {
        await _f.BranchService().UpdateAsync(_branchA.Id, new SaveBranchRequest("Merkez 2", "Adres"));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().UpdateAsync(_branchB.Id, new SaveBranchRequest("X", "Y")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().DeleteAsync(_branchB.Id));

        Assert.Equal("Serdivan Kliniği", _branchB.Name);
        Assert.Contains(_branchB, _f.Clinics.Items);
    }

    [Fact]
    public async Task WorkingHours_CanBeAddedAndRemoved_OnlyOnABranchOfTheOwnOrganization()
    {
        var ownHour = await _f.BranchService().AddWorkingHourAsync(_branchA.Id,
            new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0)));
        var foreignHour = _branchB.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().AddWorkingHourAsync(_branchB.Id,
            new AddWorkingHourRequest(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(17, 0))));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().RemoveWorkingHourAsync(_branchB.Id, foreignHour.Id));
        await _f.BranchService().RemoveWorkingHourAsync(_branchA.Id, ownHour.Id);

        Assert.Single(_branchB.WorkingHours);
    }

    // =========================================================================================
    // Departments and services
    // =========================================================================================

    [Fact]
    public async Task Departments_AreManagedOnlyWithinTheOwnOrganization()
    {
        await _f.DepartmentService().CreateAsync(_branchA.Id, new SaveDepartmentRequest("Radyoloji"));
        await _f.DepartmentService().UpdateAsync(_deptA.Id, new SaveDepartmentRequest("Yeni Ad"));

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.DepartmentService().CreateAsync(_branchB.Id, new SaveDepartmentRequest("Sizma")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.DepartmentService().UpdateAsync(_deptB.Id, new SaveDepartmentRequest("Sizma")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.DepartmentService().DeleteAsync(_deptB.Id));
        Assert.Equal("Dahiliye", _deptB.Name);
    }

    [Fact]
    public async Task Services_AreManagedOnlyWithinTheOwnOrganization()
    {
        await _f.ServiceCatalog().CreateAsync(_deptA.Id, new SaveServiceRequest("MR", ServiceType.Diagnostic, 45, 1800m));
        await _f.ServiceCatalog().UpdateAsync(_serviceA.Id, new SaveServiceRequest("Yeni", ServiceType.Examination, 30, 500m));

        var foreign = new SaveServiceRequest("Sizma", ServiceType.Examination, 30, 1m);
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.ServiceCatalog().CreateAsync(_deptB.Id, foreign));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.ServiceCatalog().UpdateAsync(_serviceB.Id, foreign));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.ServiceCatalog().DeleteAsync(_serviceB.Id));
        Assert.Equal("Genel Muayene", _serviceB.Name);
    }

    // =========================================================================================
    // Staff
    // =========================================================================================

    [Fact]
    public async Task Staff_CanBeCreatedOnlyInADepartmentOfTheOwnOrganization()
    {
        await _f.StaffService().CreateDoctorAsync(_deptA.Id, new CreateDoctorRequest("Dr. Yeni", "yeni@example.com", null, "Gecici123!"));
        await _f.StaffService().CreateTechnicianAsync(_deptA.Id, new CreateTechnicianRequest("Teknisyen", "tekn@example.com", null, "Teknisyen"));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _f.StaffService().CreateDoctorAsync(_deptB.Id, new CreateDoctorRequest("Dr. Sizma", "s@example.com", null, "Gecici123!")));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _f.StaffService().CreateTechnicianAsync(_deptB.Id, new CreateTechnicianRequest("Sizma", "s2@example.com", null, "X")));
    }

    [Fact]
    public async Task Staff_CanBeReadChangedAndRemoved_OnlyWithinTheOwnOrganization()
    {
        var service = _f.StaffService();

        Assert.Equal(_doctorA.Id, (await service.GetAsync(_doctorA.Id)).Id);
        await service.UpdateAsync(_doctorA.Id, new UpdateStaffRequest("Dr. A Yeni", "a@example.com", null, "Endokrinoloji"));
        await service.SetServicesAsync(_doctorA.Id, [_serviceA.Id]);
        await service.DeactivateAsync(_doctorA.Id);
        await service.ActivateAsync(_doctorA.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(_doctorB.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(_doctorB.Id, new UpdateStaffRequest("x", "b@example.com", null, "y")));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.SetServicesAsync(_doctorB.Id, [_serviceB.Id]));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeactivateAsync(_doctorB.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ActivateAsync(_doctorB.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(_doctorB.Id));

        Assert.True(_doctorB.IsActive);
        Assert.Contains(_doctorB, _f.Staff.Items);
    }

    [Fact]
    public async Task StaffList_ShowsAManagerOnlyTheirOwnOrganization_WhateverTheFilter()
    {
        var technicianA = _f.AddTechnician(_deptA, "t@example.com");
        var service = _f.StaffService();

        var all = await service.ListAsync(null, null);
        Assert.Equal(new[] { _doctorA.Id, technicianA.Id }.Order(), all.Select(s => s.Id).Order());

        Assert.Equal(2, (await service.ListAsync(_branchA.Id, null)).Count);
        Assert.Equal(2, (await service.ListAsync(null, _deptA.Id)).Count);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync(_branchB.Id, null));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync(null, _deptB.Id));
    }

    [Fact]
    public async Task SlotDuration_IsChangedByTheDoctorThemselves_OrByAManagerOfTheirOrganization()
    {
        // The doctor changes their own...
        As(FakeCurrentUser.Doctor(_doctorA.Id, _branchA.Id, _orgA.Id));
        await _f.StaffService().SetSlotDurationAsync(_doctorA.Id, 20);
        Assert.Equal(20, _doctorA.SlotDurationMinutes);

        // ...but not a colleague's (even in the same branch).
        var colleague = _f.AddDoctor(_deptA, "colleague@example.com", "Dr. Meslektas");
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().SetSlotDurationAsync(colleague.Id, 15));
        Assert.Equal(30, colleague.SlotDurationMinutes);

        // A manager of the organization may; one of another organization may not.
        As(FakeCurrentUser.Manager(_orgA.Id));
        await _f.StaffService().SetSlotDurationAsync(colleague.Id, 15);
        Assert.Equal(15, colleague.SlotDurationMinutes);

        As(FakeCurrentUser.Manager(_orgB.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().SetSlotDurationAsync(colleague.Id, 60));
    }

    // =========================================================================================
    // Who gets through at all
    // =========================================================================================

    [Fact]
    public async Task ASystemAdministrator_CanActAcrossOrganizations()
    {
        As(FakeCurrentUser.Admin());

        await _f.BranchService().UpdateAsync(_branchB.Id, new SaveBranchRequest("Yeni", "Adres"));
        await _f.DepartmentService().UpdateAsync(_deptA.Id, new SaveDepartmentRequest("A"));
        await _f.ServiceCatalog().UpdateAsync(_serviceB.Id, new SaveServiceRequest("B", ServiceType.Examination, 30, 100m));
        await _f.StaffService().DeactivateAsync(_doctorB.Id);
        await _f.StaffService().SetSlotDurationAsync(_doctorA.Id, 10);

        Assert.Equal(2, (await _f.StaffService().ListAsync(null, null)).Count); // sees the staff of both organizations
    }

    [Fact]
    public async Task AManagerWhoseTokenHasNoOrganization_ReachesNothing()
    {
        As(new FakeCurrentUser { UserId = Guid.NewGuid(), Role = RoleNames.ClinicManager });

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().UpdateAsync(_branchA.Id, new SaveBranchRequest("x", "y")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().GetAsync(_doctorA.Id));
        Assert.Empty(await _f.StaffService().ListAsync(null, null));
    }

    [Fact]
    public async Task OtherRoles_AreNotManagers_EvenWithAnOrganizationInTheToken()
    {
        As(FakeCurrentUser.Doctor(_doctorA.Id, _branchA.Id, _orgA.Id));

        await Assert.ThrowsAsync<ForbiddenException>(() => _f.BranchService().UpdateAsync(_branchA.Id, new SaveBranchRequest("x", "y")));
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.OrganizationService().UpdateAsync(_orgA.Id, new SaveOrganizationRequest("x")));
    }

    // =========================================================================================
    // Manager accounts (system administrator only)
    // =========================================================================================

    private static CreateManagerRequest NewManager(string email = "mudur@example.com", string password = "Password1") =>
        new("Yeni Mudur", email, password, "+90-264-555-0100");

    [Fact]
    public async Task ManagerAccounts_AreCreatedByASystemAdministrator_ForTheGivenBranch()
    {
        As(FakeCurrentUser.Admin());

        var dto = await _f.ManagerService().CreateAsync(_branchA.Id, NewManager());

        Assert.Equal("ClinicManager", dto.Role);
        var manager = Assert.IsType<ClinicManager>(Assert.Single(_f.Users.Items));
        Assert.Equal(_branchA.Id, manager.ClinicFacilityId);
        Assert.NotNull(manager.PasswordHash);
        Assert.True(new AspNetPasswordHasher().Verify(manager.PasswordHash!, "Password1")); // stored hashed, and it verifies
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task ManagerAccounts_CannotBeCreatedByManagersDoctorsOrPatients_NotEvenInTheirOwnBranch()
    {
        foreach (var caller in new ICurrentUser[]
                 {
                     FakeCurrentUser.Manager(_orgA.Id, _branchA.Id),
                     FakeCurrentUser.Doctor(_doctorA.Id, _branchA.Id, _orgA.Id),
                     FakeCurrentUser.Patient(Guid.NewGuid()),
                     FakeCurrentUser.Anonymous()
                 })
        {
            As(caller);
            await Assert.ThrowsAsync<ForbiddenException>(() => _f.ManagerService().CreateAsync(_branchA.Id, NewManager()));
        }

        Assert.Empty(_f.Users.Items);
    }

    [Fact]
    public async Task ManagerAccounts_Throw_ForAnUnknownBranch_ADuplicateEmail_AShortPassword_AndAnInvalidEmail()
    {
        As(FakeCurrentUser.Admin());
        await _f.ManagerService().CreateAsync(_branchA.Id, NewManager());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.ManagerService().CreateAsync(Guid.NewGuid(), NewManager("x@example.com")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.ManagerService().CreateAsync(_branchA.Id, NewManager("MUDUR@example.com"))); // taken, any case
        await Assert.ThrowsAsync<ArgumentException>(() => _f.ManagerService().CreateAsync(_branchA.Id, NewManager("y@example.com", "short")));
        await Assert.ThrowsAsync<ArgumentException>(() => _f.ManagerService().CreateAsync(_branchA.Id, NewManager("not-an-email")));

        Assert.Single(_f.Users.Items);
    }
}
