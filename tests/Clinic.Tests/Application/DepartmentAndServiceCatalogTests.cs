using Clinic.Application.Management;
using Clinic.Domain.Enums;

namespace Clinic.Tests.Application;

public class DepartmentAndServiceCatalogTests
{
    private readonly ManagementFixture _f = new();

    // =========================================================================================
    // Departments
    // =========================================================================================

    [Fact]
    public async Task Department_Create_AddsItUnderTheBranch()
    {
        var branch = _f.AddBranch(_f.AddOrganization());

        var dto = await _f.DepartmentService().CreateAsync(branch.Id, new SaveDepartmentRequest("Radyoloji"));

        Assert.Equal(branch.Id, dto.BranchId);
        Assert.Equal("Radyoloji", dto.Name);
        Assert.Single(_f.Departments.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Department_Create_Throws_ForAnUnknownBranchOrBlankName()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var service = _f.DepartmentService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(Guid.NewGuid(), new SaveDepartmentRequest("Radyoloji")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(branch.Id, new SaveDepartmentRequest(" ")));
        Assert.Empty(_f.Departments.Items);
    }

    [Fact]
    public async Task Department_List_ReturnsOnlyThatBranchsDepartments_ByName()
    {
        var organization = _f.AddOrganization();
        var merkez = _f.AddBranch(organization, "Merkez");
        var other = _f.AddBranch(organization, "Adapazarı Şubesi");
        _f.AddDepartment(merkez, "Radyoloji");
        _f.AddDepartment(merkez, "Dahiliye");
        _f.AddDepartment(other, "Laboratuvar");

        var list = await _f.DepartmentService().ListByBranchAsync(merkez.Id);

        Assert.Equal(["Dahiliye", "Radyoloji"], list.Select(d => d.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.DepartmentService().ListByBranchAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Department_Update_RenamesIt_OrThrows()
    {
        var department = _f.AddDepartment(_f.AddBranch(_f.AddOrganization()));
        var service = _f.DepartmentService();

        var dto = await service.UpdateAsync(department.Id, new SaveDepartmentRequest("İç Hastalıkları"));

        Assert.Equal("İç Hastalıkları", dto.Name);
        Assert.Equal(department.ClinicFacilityId, dto.BranchId);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(department.Id, new SaveDepartmentRequest("")));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new SaveDepartmentRequest("x")));
    }

    [Fact]
    public async Task Department_Delete_RemovesAnEmptyDepartment()
    {
        var department = _f.AddDepartment(_f.AddBranch(_f.AddOrganization()));

        await _f.DepartmentService().DeleteAsync(department.Id);

        Assert.Empty(_f.Departments.Items);
    }

    [Fact]
    public async Task Department_Delete_IsRefused_WhileItHasServices()
    {
        var department = _f.AddDepartment(_f.AddBranch(_f.AddOrganization()));
        _f.AddService(department);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.DepartmentService().DeleteAsync(department.Id));

        Assert.Single(_f.Departments.Items);
    }

    [Fact]
    public async Task Department_Delete_IsRefused_WhileItHasStaff()
    {
        var department = _f.AddDepartment(_f.AddBranch(_f.AddOrganization()));
        _f.AddTechnician(department); // staff even without services

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.DepartmentService().DeleteAsync(department.Id));

        Assert.Single(_f.Departments.Items);
    }

    [Fact]
    public async Task Department_Delete_Throws_ForAnUnknownId()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.DepartmentService().DeleteAsync(Guid.NewGuid()));
    }

    // =========================================================================================
    // Services
    // =========================================================================================

    private Clinic.Domain.Clinics.Department NewDepartment() => _f.AddDepartment(_f.AddBranch(_f.AddOrganization()));

    [Theory]
    [InlineData(ServiceType.Examination)]
    [InlineData(ServiceType.Diagnostic)]
    public async Task Service_Create_StoresTypeDurationAndPrice(ServiceType type)
    {
        var department = NewDepartment();

        var dto = await _f.ServiceCatalog().CreateAsync(department.Id, new SaveServiceRequest("MR", type, 45, 1800.50m));

        Assert.Equal(department.Id, dto.DepartmentId);
        Assert.Equal("MR", dto.Name);
        Assert.Equal(type, dto.ServiceType);
        Assert.Equal(45, dto.DurationMinutes);
        Assert.Equal(1800.50m, dto.BasePrice);
        Assert.Single(_f.Services.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("", 20, 10)]       // blank name
    [InlineData("Test", 0, 10)]    // zero duration
    [InlineData("Test", -5, 10)]   // negative duration
    [InlineData("Test", 1441, 10)] // longer than a day
    [InlineData("Test", 20, -1)]   // negative price
    [InlineData("Test", 20, 10.001)] // three decimals
    [InlineData("Test", 20, 100000000)] // too large to store
    public async Task Service_Create_Throws_ForInvalidValues_AndSavesNothing(string name, int minutes, double price)
    {
        var department = NewDepartment();

        // ArgumentException or its ArgumentOutOfRangeException subtype, depending on which value is wrong.
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _f.ServiceCatalog().CreateAsync(
            department.Id, new SaveServiceRequest(name, ServiceType.Diagnostic, minutes, (decimal)price)));

        Assert.Empty(_f.Services.Items);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Service_Create_Throws_ForAnUnknownTypeOrDepartment()
    {
        var department = NewDepartment();
        var service = _f.ServiceCatalog();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateAsync(department.Id, new SaveServiceRequest("MR", (ServiceType)99, 45, 100m)));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new SaveServiceRequest("MR", ServiceType.Diagnostic, 45, 100m)));
    }

    [Fact]
    public async Task Service_List_ReturnsOnlyThatDepartmentsServices_ByName()
    {
        var radiology = NewDepartment();
        var other = _f.AddDepartment(_f.AddBranch(_f.AddOrganization()), "Laboratuvar");
        _f.AddService(radiology, "Röntgen", ServiceType.Diagnostic, 20, 350m);
        _f.AddService(radiology, "MR", ServiceType.Diagnostic, 45, 1800m);
        _f.AddService(other, "Kan Tahlili", ServiceType.Diagnostic, 15, 250m);

        var list = await _f.ServiceCatalog().ListByDepartmentAsync(radiology.Id);

        Assert.Equal(["MR", "Röntgen"], list.Select(s => s.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.ServiceCatalog().ListByDepartmentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Service_Update_ChangesTheDetails()
    {
        var service = _f.AddService(NewDepartment(), "Röntgen", ServiceType.Diagnostic, 20, 350m);

        var dto = await _f.ServiceCatalog().UpdateAsync(service.Id, new SaveServiceRequest("Röntgen (iki yönlü)", ServiceType.Diagnostic, 30, 400m));

        Assert.Equal("Röntgen (iki yönlü)", dto.Name);
        Assert.Equal(30, dto.DurationMinutes);
        Assert.Equal(400m, dto.BasePrice);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Service_Update_WithInvalidValues_ChangesNothing()
    {
        var service = _f.AddService(NewDepartment(), "Röntgen", ServiceType.Diagnostic, 20, 350m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _f.ServiceCatalog().UpdateAsync(service.Id, new SaveServiceRequest("MR", ServiceType.Diagnostic, 45, -1m)));

        Assert.Equal("Röntgen", service.Name);
        Assert.Equal(350m, service.BasePrice);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Service_Update_CannotChangeTheType_OnceAppointmentsExist()
    {
        var service = _f.AddService(NewDepartment(), "Röntgen", ServiceType.Diagnostic, 20, 350m);
        _f.AddAppointment(null, service.Id, ManagementFixture.Now.AddDays(1), ManagementFixture.Now.AddDays(1).AddMinutes(20));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _f.ServiceCatalog().UpdateAsync(service.Id, new SaveServiceRequest("Röntgen", ServiceType.Examination, 20, 350m)));
        Assert.Equal(ServiceType.Diagnostic, service.ServiceType);

        // The other details stay editable, even with appointments.
        var dto = await _f.ServiceCatalog().UpdateAsync(service.Id, new SaveServiceRequest("Röntgen", ServiceType.Diagnostic, 20, 375m));
        Assert.Equal(375m, dto.BasePrice);
    }

    [Fact]
    public async Task Service_Update_CanChangeTheType_WhenNoAppointmentsExist()
    {
        var service = _f.AddService(NewDepartment(), "Muayene", ServiceType.Examination, 30, 450m);

        var dto = await _f.ServiceCatalog().UpdateAsync(service.Id, new SaveServiceRequest("Muayene", ServiceType.Diagnostic, 30, 450m));

        Assert.Equal(ServiceType.Diagnostic, dto.ServiceType);
    }

    [Fact]
    public async Task Service_Update_Throws_ForAnUnknownId()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _f.ServiceCatalog().UpdateAsync(Guid.NewGuid(), new SaveServiceRequest("x", ServiceType.Diagnostic, 10, 1m)));
    }

    [Fact]
    public async Task Service_Delete_RemovesAServiceWithNoDependents()
    {
        var service = _f.AddService(NewDepartment());

        await _f.ServiceCatalog().DeleteAsync(service.Id);

        Assert.Empty(_f.Services.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.Cancelled)] // history still refers to the service, so it blocks too
    [InlineData(AppointmentStatus.Rejected)]
    public async Task Service_Delete_IsRefused_WhileAnyAppointmentRefersToIt(AppointmentStatus status)
    {
        var service = _f.AddService(NewDepartment());
        _f.AddAppointment(null, service.Id, ManagementFixture.Now.AddDays(1), ManagementFixture.Now.AddDays(1).AddMinutes(30), status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.ServiceCatalog().DeleteAsync(service.Id));

        Assert.Single(_f.Services.Items);
    }

    [Fact]
    public async Task Service_Delete_IsRefused_WhileStaffOfferIt()
    {
        var department = NewDepartment();
        var service = _f.AddService(department);
        _f.AddDoctor(department).AddService(service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.ServiceCatalog().DeleteAsync(service.Id));

        Assert.Single(_f.Services.Items);
    }

    [Fact]
    public async Task Service_Delete_Throws_ForAnUnknownId()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.ServiceCatalog().DeleteAsync(Guid.NewGuid()));
    }
}
