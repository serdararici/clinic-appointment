using Clinic.Application.Management;

namespace Clinic.Tests.Application;

public class OrganizationAndBranchServiceTests
{
    private readonly ManagementFixture _f = new();

    // =========================================================================================
    // Organizations
    // =========================================================================================

    [Fact]
    public async Task Organization_Create_StoresItAndReturnsADto()
    {
        var dto = await _f.OrganizationService().CreateAsync(new SaveOrganizationRequest("Sakarya Şehir Hastanesi"));

        Assert.Equal("Sakarya Şehir Hastanesi", dto.Name);
        Assert.Equal(dto.Id, _f.Organizations.Items.Single().Id);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Branch_Create_StartsWithTheDefaultWorkingHours_MondayToFriday_0800To1700()
    {
        var organization = _f.AddOrganization();

        var dto = await _f.BranchService().CreateAsync(organization.Id, new SaveBranchRequest("Yeni Şube", "Adres"));

        var branch = _f.Clinics.Items.Single(b => b.Id == dto.Id);
        Assert.Equal(5, branch.WorkingHours.Count);
        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            branch.WorkingHours.Select(w => w.DayOfWeek).Order());
        Assert.All(branch.WorkingHours, w => Assert.Equal(new TimeOnly(8, 0), w.OpensAt));
        Assert.All(branch.WorkingHours, w => Assert.Equal(new TimeOnly(17, 0), w.ClosesAt));

        // ... and the working hours endpoint returns them, Monday first.
        var hours = await _f.BranchService().GetWorkingHoursAsync(dto.Id, null);
        Assert.Equal(DayOfWeek.Monday, hours[0].DayOfWeek);
        Assert.Equal(5, hours.Count);
    }

    [Fact]
    public async Task Organization_Create_Throws_WhenTheNameIsBlank_AndSavesNothing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _f.OrganizationService().CreateAsync(new SaveOrganizationRequest(" ")));

        Assert.Empty(_f.Organizations.Items);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Organization_List_IsOrderedByName_AndGetThrowsForAnUnknownId()
    {
        _f.AddOrganization("Marmara Poliklinik");
        _f.AddOrganization("Anadolu Sağlık");

        var list = await _f.OrganizationService().ListAsync();

        Assert.Equal(["Anadolu Sağlık", "Marmara Poliklinik"], list.Select(o => o.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.OrganizationService().GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Organization_Update_RenamesIt_OrThrowsForUnknownIdOrBlankName()
    {
        var organization = _f.AddOrganization("Eski Ad");
        var service = _f.OrganizationService();

        var dto = await service.UpdateAsync(organization.Id, new SaveOrganizationRequest("Yeni Ad"));
        Assert.Equal("Yeni Ad", dto.Name);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(organization.Id, new SaveOrganizationRequest("")));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new SaveOrganizationRequest("X")));
    }

    [Fact]
    public async Task Organization_Delete_RemovesAnOrganizationWithoutBranches()
    {
        var organization = _f.AddOrganization();

        await _f.OrganizationService().DeleteAsync(organization.Id);

        Assert.Empty(_f.Organizations.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Organization_Delete_IsRefused_WhileItStillHasBranches()
    {
        var organization = _f.AddOrganization();
        _f.AddBranch(organization);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.OrganizationService().DeleteAsync(organization.Id));

        Assert.Single(_f.Organizations.Items);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Organization_Delete_Throws_ForAnUnknownId()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.OrganizationService().DeleteAsync(Guid.NewGuid()));
    }

    // =========================================================================================
    // Branches
    // =========================================================================================

    [Fact]
    public async Task Branch_Create_AddsItUnderTheOrganization()
    {
        var organization = _f.AddOrganization();

        var dto = await _f.BranchService().CreateAsync(organization.Id, new SaveBranchRequest("Adapazarı Şubesi", "Semerciler Mah."));

        Assert.Equal(organization.Id, dto.OrganizationId);
        Assert.Equal("Adapazarı Şubesi", dto.Name);
        Assert.Equal("Semerciler Mah.", dto.Address);
        Assert.Single(_f.Clinics.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Branch_Create_Throws_WhenTheOrganizationDoesNotExist_SoItMustBeCreatedFirst()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _f.BranchService().CreateAsync(Guid.NewGuid(), new SaveBranchRequest("Merkez", "Sakarya")));

        Assert.Empty(_f.Clinics.Items);
    }

    [Theory]
    [InlineData("", "Sakarya")]
    [InlineData("Merkez", " ")]
    public async Task Branch_Create_Throws_ForBlankNameOrAddress(string name, string address)
    {
        var organization = _f.AddOrganization();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _f.BranchService().CreateAsync(organization.Id, new SaveBranchRequest(name, address)));

        Assert.Empty(_f.Clinics.Items);
    }

    [Fact]
    public async Task Branch_List_ReturnsOnlyTheBranchesOfThatOrganization_ByName()
    {
        var sakarya = _f.AddOrganization("Sakarya Şehir Hastanesi");
        var marmara = _f.AddOrganization("Marmara Poliklinik");
        _f.AddBranch(sakarya, "Merkez");
        _f.AddBranch(sakarya, "Adapazarı Şubesi");
        _f.AddBranch(marmara, "Serdivan Kliniği");

        var branches = await _f.BranchService().ListByOrganizationAsync(sakarya.Id);

        Assert.Equal(["Adapazarı Şubesi", "Merkez"], branches.Select(b => b.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.BranchService().ListByOrganizationAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Branch_ListAll_IncludesTheOrganizationName_OrderedByOrganizationThenBranch()
    {
        var sakarya = _f.AddOrganization("Sakarya Şehir Hastanesi");
        var marmara = _f.AddOrganization("Marmara Poliklinik");
        _f.AddBranch(sakarya, "Merkez");
        _f.AddBranch(sakarya, "Adapazarı Şubesi");
        _f.AddBranch(marmara, "Serdivan Kliniği");

        var branches = await _f.BranchService().ListAllAsync();

        Assert.Equal(
            [("Marmara Poliklinik", "Serdivan Kliniği"), ("Sakarya Şehir Hastanesi", "Adapazarı Şubesi"), ("Sakarya Şehir Hastanesi", "Merkez")],
            branches.Select(b => (b.OrganizationName, b.Name)));
        Assert.Equal(sakarya.Id, branches.First(b => b.Name == "Merkez").OrganizationId);
    }

    [Fact]
    public async Task Branch_Update_ChangesNameAndAddress_OrThrows()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var service = _f.BranchService();

        var dto = await service.UpdateAsync(branch.Id, new SaveBranchRequest("Yeni Şube", "Yeni adres"));

        Assert.Equal("Yeni Şube", dto.Name);
        Assert.Equal(branch.OrganizationId, dto.OrganizationId);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(branch.Id, new SaveBranchRequest("", "x")));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new SaveBranchRequest("x", "y")));
    }

    [Fact]
    public async Task Branch_Delete_RemovesABranchWithOnlyWorkingHours()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0)); // owned config, deleted with the branch

        await _f.BranchService().DeleteAsync(branch.Id);

        Assert.Empty(_f.Clinics.Items);
    }

    [Fact]
    public async Task Branch_Delete_IsRefused_WhileItHasDepartments()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        _f.AddDepartment(branch);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.BranchService().DeleteAsync(branch.Id));

        Assert.Single(_f.Clinics.Items);
    }

    [Fact]
    public async Task Branch_Delete_IsRefused_WhileAManagerIsAssigned()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        _f.Clinics.Managers.Add(new Clinic.Domain.Users.ClinicManager("Ayşe Yılmaz", "ayse@example.com", "+90-500-000-0003", branch.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.BranchService().DeleteAsync(branch.Id));

        Assert.Single(_f.Clinics.Items);
    }

    [Fact]
    public async Task Branch_Delete_Throws_ForAnUnknownId()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.BranchService().DeleteAsync(Guid.NewGuid()));
    }

    // =========================================================================================
    // Working hours
    // =========================================================================================

    [Fact]
    public async Task WorkingHours_Add_StoresTheRange()
    {
        var branch = _f.AddBranch(_f.AddOrganization());

        var dto = await _f.BranchService().AddWorkingHourAsync(branch.Id,
            new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0)));

        Assert.Equal(branch.Id, dto.BranchId);
        Assert.Equal(DayOfWeek.Monday, dto.DayOfWeek);
        Assert.Equal(new TimeOnly(8, 0), dto.OpensAt);
        Assert.Single(_f.Clinics.AddedWorkingHours);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task WorkingHours_Add_IsRefused_WhenItOverlapsARangeOnTheSameDay()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.BranchService().AddWorkingHourAsync(branch.Id,
            new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(15, 0))));

        Assert.Empty(_f.Clinics.AddedWorkingHours);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task WorkingHours_Add_AllowsALaterRangeTheSameDay_AndTheSameTimesOnAnotherDay()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var service = _f.BranchService();

        await service.AddWorkingHourAsync(branch.Id, new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)));
        await service.AddWorkingHourAsync(branch.Id, new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(17, 0)));
        await service.AddWorkingHourAsync(branch.Id, new AddWorkingHourRequest(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(12, 0)));

        Assert.Equal(3, branch.WorkingHours.Count);
    }

    [Fact]
    public async Task WorkingHours_Add_Throws_ForAnInvalidRangeOrUnknownBranch()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var service = _f.BranchService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddWorkingHourAsync(branch.Id,
            new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(8, 0))));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddWorkingHourAsync(Guid.NewGuid(),
            new AddWorkingHourRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0))));
    }

    [Fact]
    public async Task WorkingHours_List_IsOrderedMondayFirst_AndCanBeFilteredToOneWeekday()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        branch.AddWorkingHour(DayOfWeek.Sunday, new TimeOnly(10, 0), new TimeOnly(12, 0));
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(17, 0));
        branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        branch.AddWorkingHour(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0));

        var all = await _f.BranchService().GetWorkingHoursAsync(branch.Id, null);
        var monday = await _f.BranchService().GetWorkingHoursAsync(branch.Id, DayOfWeek.Monday);
        var tuesday = await _f.BranchService().GetWorkingHoursAsync(branch.Id, DayOfWeek.Tuesday);

        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Saturday, DayOfWeek.Sunday],
            all.Select(w => w.DayOfWeek));
        Assert.Equal([new TimeOnly(8, 0), new TimeOnly(13, 0)], monday.Select(w => w.OpensAt)); // by opening time
        Assert.Empty(tuesday);
    }

    [Fact]
    public async Task WorkingHours_Remove_DeletesTheRange_OrThrowsForUnknownIds()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var monday = branch.AddWorkingHour(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var service = _f.BranchService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RemoveWorkingHourAsync(branch.Id, Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RemoveWorkingHourAsync(Guid.NewGuid(), monday.Id));

        await service.RemoveWorkingHourAsync(branch.Id, monday.Id);

        Assert.Empty(branch.WorkingHours);
        Assert.Single(_f.Clinics.RemovedWorkingHours);
    }
}
