using Clinic.Application.Staff;

namespace Clinic.Tests.Application;

public class StaffServiceTests
{
    private readonly FakeStaffMemberRepository _staff = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task SetSlotDuration_UpdatesTheStaffMemberAndSaves()
    {
        var doctor = TestData.NewDoctor(slotDurationMinutes: 30);
        _staff.Items.Add(doctor);
        var service = new StaffService(_staff, _unitOfWork);

        var result = await service.SetSlotDurationAsync(doctor.Id, 20);

        Assert.Equal(20, doctor.SlotDurationMinutes);
        Assert.Equal(new StaffSlotDurationDto(doctor.Id, 20), result);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetSlotDuration_Throws_WhenValueIsNotAllowed_AndDoesNotSave()
    {
        var doctor = TestData.NewDoctor(slotDurationMinutes: 30);
        _staff.Items.Add(doctor);
        var service = new StaffService(_staff, _unitOfWork);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SetSlotDurationAsync(doctor.Id, 25));

        Assert.Equal(30, doctor.SlotDurationMinutes);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetSlotDuration_Throws_WhenStaffMemberDoesNotExist()
    {
        var service = new StaffService(_staff, _unitOfWork);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetSlotDurationAsync(Guid.NewGuid(), 20));
    }
}
