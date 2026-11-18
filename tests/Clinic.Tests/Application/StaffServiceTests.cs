using Clinic.Application.Auth;
using Clinic.Application.Exceptions;
using Clinic.Application.Staff;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;

namespace Clinic.Tests.Application;

public class StaffServiceTests
{
    private static readonly DateTime Now = ManagementFixture.Now;

    private readonly ManagementFixture _f = new();

    private Clinic.Domain.Clinics.Department NewDepartment(string name = "Dahiliye") =>
        _f.AddDepartment(_f.AddBranch(_f.AddOrganization()), name);

    // =========================================================================================
    // Slot duration (step 1 behavior, kept)
    // =========================================================================================

    [Fact]
    public async Task SetSlotDuration_UpdatesTheStaffMemberAndSaves()
    {
        var doctor = _f.AddDoctor(NewDepartment(), slotMinutes: 30);

        var result = await _f.StaffService().SetSlotDurationAsync(doctor.Id, 20);

        Assert.Equal(20, doctor.SlotDurationMinutes);
        Assert.Equal(new StaffSlotDurationDto(doctor.Id, 20), result);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetSlotDuration_Throws_WhenValueIsNotAllowed_AndDoesNotSave()
    {
        var doctor = _f.AddDoctor(NewDepartment(), slotMinutes: 30);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _f.StaffService().SetSlotDurationAsync(doctor.Id, 25));

        Assert.Equal(30, doctor.SlotDurationMinutes);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetSlotDuration_Throws_WhenStaffMemberDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().SetSlotDurationAsync(Guid.NewGuid(), 20));
    }

    // =========================================================================================
    // Create
    // =========================================================================================

    [Fact]
    public async Task CreateDoctor_RegistersADoctorInTheDepartment_WithTheDefaultSlotDuration()
    {
        var department = NewDepartment();

        var dto = await _f.StaffService().CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. Hakan Yıldız", "hakan@example.com", "+90-264-555-0011", "Gecici123!"));

        Assert.Equal("Doctor", dto.Role);
        Assert.Null(dto.Title);
        Assert.Equal(department.Id, dto.DepartmentId);
        Assert.Equal(department.ClinicFacilityId, dto.BranchId); // the branch comes from the department
        Assert.Equal(30, dto.SlotDurationMinutes);
        Assert.True(dto.IsActive);
        Assert.Empty(dto.ServiceIds);
        Assert.IsType<Doctor>(Assert.Single(_f.Staff.Items));
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDoctor_AcceptsAnAllowedSlotDuration_AndAMissingPhone()
    {
        var dto = await _f.StaffService().CreateDoctorAsync(NewDepartment().Id,
            new CreateDoctorRequest("Dr. Elif Şahin", "elif@example.com", null, "Gecici123!", 20));

        Assert.Equal(20, dto.SlotDurationMinutes);
        Assert.Equal(string.Empty, dto.PhoneNumber);
    }

    [Fact]
    public async Task CreateDoctor_Throws_ForInvalidInput_AndSavesNothing()
    {
        var department = NewDepartment();
        var service = _f.StaffService();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("", "x@example.com", null, "Gecici123!")));          // no name
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. X", " ", null, "Gecici123!")));                 // no email
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. X", "x@example.com", null, "Gecici123!", 25))); // slot not allowed

        Assert.Empty(_f.Staff.Items);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDoctor_FillsTheDefaultScheduleRightAway_SoTheDoctorIsBookableImmediately()
    {
        var dto = await _f.StaffService().CreateDoctorAsync(NewDepartment().Id,
            new CreateDoctorRequest("Dr. Yeni", "yeni@example.com", null, "Gecici123!"));

        Assert.Equal([dto.Id], _f.DefaultSchedule.Requested);
    }

    [Fact]
    public async Task CreateDoctor_GivesALogin_AndOffersTheExaminationServicesOfTheDepartment()
    {
        var department = NewDepartment();
        var examination = _f.AddService(department, "Genel Muayene", ServiceType.Examination, 30, 450m);
        _f.AddService(department, "EKG", ServiceType.Diagnostic, 15, 300m);

        var dto = await _f.StaffService().CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. Yeni", "Yeni@Example.com", null, "Gecici123!"));

        var doctor = Assert.IsType<Doctor>(Assert.Single(_f.Staff.Items));
        Assert.NotNull(doctor.PasswordHash);
        Assert.NotEqual("Gecici123!", doctor.PasswordHash);   // stored hashed
        Assert.Equal([examination.Id], dto.ServiceIds);        // the diagnostic service is booked without a doctor
    }

    [Fact]
    public async Task CreateDoctor_Throws_ForAShortPassword_OrAnEmailThatIsAlreadyUsed_AndSavesNothing()
    {
        var department = NewDepartment();
        _f.Users.Items.Add(new Patient("Var Olan", "var@example.com", "", new DateOnly(1990, 1, 1)));
        var service = _f.StaffService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. X", "x@example.com", null, "kisa")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDoctorAsync(department.Id,
            new CreateDoctorRequest("Dr. X", "VAR@example.com", null, "Gecici123!")));

        Assert.Empty(_f.Staff.Items);
        Assert.Empty(_f.DefaultSchedule.Requested);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_Throws_WhenTheNewEmailBelongsToSomeoneElse_ButAllowsKeepingTheOwnEmail()
    {
        var doctor = _f.AddDoctor(NewDepartment(), "own@example.com");
        _f.Users.Items.Add(new Patient("Var Olan", "var@example.com", "", new DateOnly(1990, 1, 1)));
        _f.Users.Items.Add(doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _f.StaffService().UpdateAsync(doctor.Id, new UpdateStaffRequest("Dr. Test", "var@example.com", null)));

        var dto = await _f.StaffService().UpdateAsync(doctor.Id, new UpdateStaffRequest("Dr. Yeni Ad", "own@example.com", null));
        Assert.Equal("Dr. Yeni Ad", dto.FullName);
    }

    [Fact]
    public async Task CreateDoctor_Throws_WhenTheDepartmentDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().CreateDoctorAsync(Guid.NewGuid(),
            new CreateDoctorRequest("Dr. X", "x@example.com", null, "Gecici123!")));
    }

    [Fact]
    public async Task CreateTechnician_RegistersATechnicianWithATitle()
    {
        var department = NewDepartment("Radyoloji");

        var dto = await _f.StaffService().CreateTechnicianAsync(department.Id,
            new CreateTechnicianRequest("Kemal Arslan", "kemal@example.com", "+90-264-555-0021", "Radyoloji Teknikeri"));

        Assert.Equal("HealthTechnician", dto.Role);
        Assert.Equal("Radyoloji Teknikeri", dto.Title);
        Assert.Equal(department.Id, dto.DepartmentId);
        Assert.IsType<HealthTechnician>(Assert.Single(_f.Staff.Items));
    }

    [Fact]
    public async Task CreateTechnician_Throws_WhenTheTitleIsMissingOrTheDepartmentUnknown()
    {
        var department = NewDepartment();

        await Assert.ThrowsAsync<ArgumentException>(() => _f.StaffService().CreateTechnicianAsync(department.Id,
            new CreateTechnicianRequest("Kemal Arslan", "kemal@example.com", null, " ")));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().CreateTechnicianAsync(Guid.NewGuid(),
            new CreateTechnicianRequest("Kemal Arslan", "kemal@example.com", null, "Teknisyen")));
        Assert.Empty(_f.Staff.Items);
    }

    // =========================================================================================
    // Update
    // =========================================================================================

    [Fact]
    public async Task Update_ChangesADoctorsNameAndContact()
    {
        var doctor = _f.AddDoctor(NewDepartment());

        var dto = await _f.StaffService().UpdateAsync(doctor.Id,
            new UpdateStaffRequest("Dr. Yeni Ad", "yeni@example.com", "+90-264-555-0099"));

        Assert.Equal("Dr. Yeni Ad", dto.FullName);
        Assert.Equal("yeni@example.com", dto.Email);
        Assert.Equal("+90-264-555-0099", dto.PhoneNumber);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_ChangesATechniciansTitle()
    {
        var technician = _f.AddTechnician(NewDepartment("Radyoloji"));

        var dto = await _f.StaffService().UpdateAsync(technician.Id,
            new UpdateStaffRequest("Kemal Arslan", "kemal@example.com", null, Title: "Kıdemli Teknisyen"));

        Assert.Equal("Kıdemli Teknisyen", dto.Title);
    }

    [Fact]
    public async Task Update_Throws_WhenTheRoleSpecificFieldIsMissing_OrTheStaffMemberUnknown()
    {
        var doctor = _f.AddDoctor(NewDepartment());
        var technician = _f.AddTechnician(NewDepartment("Radyoloji"), email: "t2@example.com");
        var service = _f.StaffService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(technician.Id,
            new UpdateStaffRequest("Kemal", "t2@example.com", null, Title: null)));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(),
            new UpdateStaffRequest("x", "x@example.com", null, "y")));
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    // =========================================================================================
    // Offered services (only services of the same department)
    // =========================================================================================

    [Fact]
    public async Task SetServices_SetsServicesOfTheStaffMembersOwnDepartment()
    {
        var department = NewDepartment();
        var checkUp = _f.AddService(department, "Genel Muayene");
        var followUp = _f.AddService(department, "Kontrol Muayenesi", minutes: 15, price: 250m);
        var doctor = _f.AddDoctor(department);

        var dto = await _f.StaffService().SetServicesAsync(doctor.Id, [checkUp.Id, followUp.Id]);

        // Ids are random, so compare both sides in the same (sorted) order rather than insertion order.
        Assert.Equal(new[] { checkUp.Id, followUp.Id }.Order(), dto.ServiceIds.Order());
        Assert.True(doctor.OffersService(checkUp.Id));
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetServices_ReplacesThePreviousSet_AndAnEmptyListClearsIt()
    {
        var department = NewDepartment();
        var checkUp = _f.AddService(department, "Genel Muayene");
        var followUp = _f.AddService(department, "Kontrol Muayenesi", minutes: 15, price: 250m);
        var doctor = _f.AddDoctor(department);
        doctor.AddService(checkUp);

        await _f.StaffService().SetServicesAsync(doctor.Id, [followUp.Id]);
        Assert.Equal([followUp.Id], doctor.ServicesProvided.Select(s => s.Id));

        await _f.StaffService().SetServicesAsync(doctor.Id, []);
        Assert.Empty(doctor.ServicesProvided);
    }

    [Fact]
    public async Task SetServices_IsRefused_ForAServiceOfAnotherDepartment_AndChangesNothing()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var internalMedicine = _f.AddDepartment(branch, "Dahiliye");
        var radiology = _f.AddDepartment(branch, "Radyoloji");
        var own = _f.AddService(internalMedicine, "Genel Muayene");
        var foreign = _f.AddService(radiology, "Röntgen", ServiceType.Diagnostic, 20, 350m);
        var doctor = _f.AddDoctor(internalMedicine);
        doctor.AddService(own);

        await Assert.ThrowsAsync<ArgumentException>(() => _f.StaffService().SetServicesAsync(doctor.Id, [own.Id, foreign.Id]));

        Assert.Equal([own.Id], doctor.ServicesProvided.Select(s => s.Id));
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetServices_Throws_ForAnUnknownServiceOrStaffMember()
    {
        var department = NewDepartment();
        var doctor = _f.AddDoctor(department);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().SetServicesAsync(doctor.Id, [Guid.NewGuid()]));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().SetServicesAsync(Guid.NewGuid(), []));
        Assert.Empty(doctor.ServicesProvided);
    }

    // =========================================================================================
    // List by branch or department
    // =========================================================================================

    [Fact]
    public async Task List_FiltersByBranchAndByDepartment_AndOrdersByName()
    {
        var organization = _f.AddOrganization();
        var merkez = _f.AddBranch(organization, "Merkez");
        var adapazari = _f.AddBranch(organization, "Adapazarı Şubesi");
        var merkezDahiliye = _f.AddDepartment(merkez, "Dahiliye");
        var merkezRadyoloji = _f.AddDepartment(merkez, "Radyoloji");
        var adapazariDahiliye = _f.AddDepartment(adapazari, "Dahiliye");
        _f.AddDoctor(merkezDahiliye, "z@example.com", "Dr. Zeynep");
        _f.AddDoctor(merkezDahiliye, "a@example.com", "Dr. Ayşe");
        _f.AddTechnician(merkezRadyoloji, "k@example.com", "Kemal");
        _f.AddDoctor(adapazariDahiliye, "b@example.com", "Dr. Burak");
        var service = _f.StaffService();

        var byBranch = await service.ListAsync(merkez.Id, null);
        var byDepartment = await service.ListAsync(null, merkezDahiliye.Id);
        var byBoth = await service.ListAsync(merkez.Id, merkezRadyoloji.Id);
        var everyone = await service.ListAsync(null, null);
        var nobody = await service.ListAsync(adapazari.Id, merkezRadyoloji.Id); // department is not in that branch

        Assert.Equal(["Dr. Ayşe", "Dr. Zeynep", "Kemal"], byBranch.Select(s => s.FullName));
        Assert.Equal(["Dr. Ayşe", "Dr. Zeynep"], byDepartment.Select(s => s.FullName));
        Assert.Equal(["Kemal"], byBoth.Select(s => s.FullName));
        Assert.Equal(4, everyone.Count);
        Assert.Empty(nobody);
    }

    [Fact]
    public async Task ActiveDoctors_OfADepartment_NeverIncludeDoctorsOfAnotherDepartment_AndCarryTheDepartmentNameAsSpecialty()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var internalMedicine = _f.AddDepartment(branch, "İç Hastalıkları (Dahiliye)");
        var gastro = _f.AddDepartment(branch, "Gastroenteroloji");
        var internalDoctor = _f.AddDoctor(internalMedicine, "i@example.com", "Dr. Dahiliye");
        var gastroDoctor = _f.AddDoctor(gastro, "g@example.com", "Dr. Gastro");

        var internalList = await _f.StaffService().ListActiveDoctorsAsync(internalMedicine.Id);
        var gastroList = await _f.StaffService().ListActiveDoctorsAsync(gastro.Id);

        Assert.Equal(internalDoctor.Id, Assert.Single(internalList).Id);
        Assert.Equal("İç Hastalıkları (Dahiliye)", Assert.Single(internalList).DepartmentName);
        Assert.Equal(gastroDoctor.Id, Assert.Single(gastroList).Id);
        Assert.Equal("Gastroenteroloji", Assert.Single(gastroList).DepartmentName);
    }

    [Fact]
    public async Task ActiveDoctors_ListOnlyActiveDoctorsOfTheDepartment_WithTheirServicesAndSlotDuration()
    {
        var branch = _f.AddBranch(_f.AddOrganization());
        var internalMedicine = _f.AddDepartment(branch, "Dahiliye");
        var radiology = _f.AddDepartment(branch, "Radyoloji");
        var checkUp = _f.AddService(internalMedicine, "Genel Muayene", ServiceType.Examination, 30, 450m);
        var followUp = _f.AddService(internalMedicine, "Kontrol Muayenesi", ServiceType.Examination, 15, 250m);
        var active = _f.AddDoctor(internalMedicine, "a@example.com", "Dr. Aktif", slotMinutes: 20);
        active.AddService(followUp);
        active.AddService(checkUp);
        _f.AddDoctor(internalMedicine, "b@example.com", "Dr. Pasif").Deactivate();   // inactive: hidden
        _f.AddTechnician(internalMedicine, "t@example.com");                         // not a doctor: hidden
        _f.AddDoctor(radiology, "r@example.com", "Dr. Baska Bolum");                 // other department: hidden

        var doctors = await _f.StaffService().ListActiveDoctorsAsync(internalMedicine.Id);

        var doctor = Assert.Single(doctors);
        Assert.Equal(active.Id, doctor.Id);
        Assert.Equal(internalMedicine.Name, doctor.DepartmentName); // the specialty is the department name
        Assert.Equal(20, doctor.SlotDurationMinutes);
        Assert.Equal(["Genel Muayene", "Kontrol Muayenesi"], doctor.Services.Select(s => s.Name)); // by name
        Assert.Equal((ServiceType.Examination, 30, 450m),
            (doctor.Services[0].ServiceType, doctor.Services[0].DurationMinutes, doctor.Services[0].BasePrice));
    }

    [Fact]
    public async Task ActiveDoctors_Throw_ForAnUnknownDepartment_AndAreEmptyForOneWithoutDoctors()
    {
        var department = NewDepartment();

        Assert.Empty(await _f.StaffService().ListActiveDoctorsAsync(department.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().ListActiveDoctorsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_IncludesInactiveStaff_WithTheirFlag()
    {
        var department = NewDepartment();
        var active = _f.AddDoctor(department, "a@example.com", "Dr. Aktif");
        var inactive = _f.AddDoctor(department, "b@example.com", "Dr. Pasif");
        inactive.Deactivate();

        var list = await _f.StaffService().ListAsync(null, department.Id);

        Assert.Equal([true, false], list.Select(s => s.IsActive));
        Assert.Equal(active.Id, list[0].Id);
    }

    [Fact]
    public async Task Get_ReturnsTheStaffMember_OrThrowsForAnUnknownId()
    {
        var doctor = _f.AddDoctor(NewDepartment());

        Assert.Equal(doctor.Id, (await _f.StaffService().GetAsync(doctor.Id)).Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AutoConfirm_IsOnByDefault_CanBeSwitchedByTheOwnDoctor_AndShowsInTheProfile()
    {
        var doctor = _f.AddDoctor(NewDepartment(), "own@example.com", "Dr. Kendi");
        _f.CurrentUser = FakeCurrentUser.Doctor(doctor.Id);
        var service = _f.StaffService();

        Assert.True((await service.GetDoctorProfileAsync(doctor.Id)).AutoConfirmAppointments);

        var off = await service.SetAutoConfirmAsync(doctor.Id, false);
        Assert.False(off.AutoConfirmAppointments);
        Assert.False(doctor.AutoConfirmAppointments);
        Assert.False((await service.GetDoctorProfileAsync(doctor.Id)).AutoConfirmAppointments);

        Assert.True((await service.SetAutoConfirmAsync(doctor.Id, true)).AutoConfirmAppointments);
    }

    [Fact]
    public async Task AutoConfirm_TurningItOn_DoesNotTouchAppointmentsThatAreAlreadyPending()
    {
        var doctor = _f.AddDoctor(NewDepartment(), "own@example.com", "Dr. Kendi");
        doctor.SetAutoConfirm(false);
        var pending = new Clinic.Domain.Appointments.Appointment(
            Guid.NewGuid(), doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30), 100m, AppointmentStatus.Pending);
        _f.Appointments.Items.Add(pending);
        _f.CurrentUser = FakeCurrentUser.Doctor(doctor.Id);

        await _f.StaffService().SetAutoConfirmAsync(doctor.Id, true);

        Assert.Equal(AppointmentStatus.Pending, pending.Status);
    }

    [Fact]
    public async Task AutoConfirm_IsOwnDoctorOnly()
    {
        var department = NewDepartment();
        var doctor = _f.AddDoctor(department, "own@example.com", "Dr. Kendi");
        var other = _f.AddDoctor(department, "other@example.com", "Dr. Baska");

        foreach (var caller in new ICurrentUser[] { FakeCurrentUser.Doctor(other.Id), FakeCurrentUser.Patient(Guid.NewGuid()), FakeCurrentUser.Manager(department.ClinicFacilityId), FakeCurrentUser.Admin(), FakeCurrentUser.Anonymous() })
        {
            _f.CurrentUser = caller;
            await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().SetAutoConfirmAsync(doctor.Id, false));
        }

        Assert.True(doctor.AutoConfirmAppointments);
    }

    [Fact]
    public async Task DoctorProfile_ReturnsTheOwnNameAndDepartment_AndRefusesEveryoneElse()
    {
        var department = NewDepartment("Gastroenteroloji");
        var doctor = _f.AddDoctor(department, "own@example.com", "Dr. Kendi");
        var other = _f.AddDoctor(department, "other@example.com", "Dr. Baska");

        _f.CurrentUser = FakeCurrentUser.Doctor(doctor.Id);
        var profile = await _f.StaffService().GetDoctorProfileAsync(doctor.Id);

        Assert.Equal("Dr. Kendi", profile.FullName);
        Assert.Equal("Gastroenteroloji", profile.DepartmentName);
        Assert.Equal(doctor.SlotDurationMinutes, profile.SlotDurationMinutes);
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().GetDoctorProfileAsync(other.Id));

        _f.CurrentUser = FakeCurrentUser.Patient(Guid.NewGuid());
        await Assert.ThrowsAsync<ForbiddenException>(() => _f.StaffService().GetDoctorProfileAsync(doctor.Id));
    }

    // =========================================================================================
    // Deactivate / delete: refused with upcoming non-cancelled appointments
    // =========================================================================================

    [Fact]
    public async Task Deactivate_MarksTheStaffMemberInactive_WhenNothingIsUpcoming()
    {
        var doctor = _f.AddDoctor(NewDepartment());

        var dto = await _f.StaffService().DeactivateAsync(doctor.Id);

        Assert.False(dto.IsActive);
        Assert.False(doctor.IsActive);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Confirmed)]
    public async Task Deactivate_AndDelete_AreRefused_WhenAnActiveAppointmentIsAhead(AppointmentStatus status)
    {
        var doctor = _f.AddDoctor(NewDepartment());
        _f.AddAppointment(doctor.Id, Guid.NewGuid(), Now.AddDays(1), Now.AddDays(1).AddMinutes(30), status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.StaffService().DeactivateAsync(doctor.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.StaffService().DeleteAsync(doctor.Id));

        Assert.True(doctor.IsActive);
        Assert.Single(_f.Staff.Items);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_AndDelete_AreRefused_WhileAnAppointmentIsInProgress()
    {
        var doctor = _f.AddDoctor(NewDepartment());
        // Started ten minutes ago and ends in twenty: not finished yet.
        _f.AddAppointment(doctor.Id, Guid.NewGuid(), Now.AddMinutes(-10), Now.AddMinutes(20));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.StaffService().DeactivateAsync(doctor.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.StaffService().DeleteAsync(doctor.Id));
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Rejected)]
    public async Task Deactivate_AndDelete_AreAllowed_WhenTheFutureAppointmentIsCancelledOrRejected(AppointmentStatus status)
    {
        var doctor = _f.AddDoctor(NewDepartment());
        var other = _f.AddDoctor(NewDepartment(), "other@example.com");
        _f.AddAppointment(doctor.Id, Guid.NewGuid(), Now.AddDays(1), Now.AddDays(1).AddMinutes(30), status);
        _f.AddAppointment(other.Id, Guid.NewGuid(), Now.AddDays(1), Now.AddDays(1).AddMinutes(30), status);

        await _f.StaffService().DeactivateAsync(doctor.Id);
        await _f.StaffService().DeleteAsync(other.Id);

        Assert.False(doctor.IsActive);
        Assert.DoesNotContain(other, _f.Staff.Items);
    }

    [Fact]
    public async Task Deactivate_AndDelete_AreAllowed_WhenOnlyPastAppointmentsExist_OrAnotherDoctorHasFutureOnes()
    {
        var doctor = _f.AddDoctor(NewDepartment());
        var busyColleague = _f.AddDoctor(NewDepartment(), "busy@example.com");
        _f.AddAppointment(doctor.Id, Guid.NewGuid(), Now.AddDays(-1), Now.AddDays(-1).AddMinutes(30), AppointmentStatus.Confirmed);
        _f.AddAppointment(busyColleague.Id, Guid.NewGuid(), Now.AddDays(1), Now.AddDays(1).AddMinutes(30));

        await _f.StaffService().DeactivateAsync(doctor.Id);
        await _f.StaffService().DeleteAsync(doctor.Id);

        Assert.DoesNotContain(doctor, _f.Staff.Items);
    }

    [Fact]
    public async Task Deactivate_OnAnAlreadyInactiveStaffMember_DoesNothing()
    {
        var doctor = _f.AddDoctor(NewDepartment());
        doctor.Deactivate();

        var dto = await _f.StaffService().DeactivateAsync(doctor.Id);

        Assert.False(dto.IsActive);
        Assert.Equal(0, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Activate_ReactivatesAnInactiveStaffMember()
    {
        var doctor = _f.AddDoctor(NewDepartment());
        doctor.Deactivate();

        var dto = await _f.StaffService().ActivateAsync(doctor.Id);

        Assert.True(dto.IsActive);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Delete_RemovesAStaffMemberWithNoUpcomingAppointments()
    {
        var technician = _f.AddTechnician(NewDepartment("Radyoloji"));

        await _f.StaffService().DeleteAsync(technician.Id);

        Assert.Empty(_f.Staff.Items);
        Assert.Equal(1, _f.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deactivate_Activate_AndDelete_Throw_ForAnUnknownStaffMember()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().DeactivateAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().ActivateAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.StaffService().DeleteAsync(Guid.NewGuid()));
    }
}
