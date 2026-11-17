namespace Clinic.Application.Staff;

public interface IStaffService
{
    Task<StaffSlotDurationDto> SetSlotDurationAsync(Guid staffId, int slotDurationMinutes, CancellationToken cancellationToken = default);
}
