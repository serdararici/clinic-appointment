using Clinic.Domain.Repositories;

namespace Clinic.Application.Staff;

/// <summary>Maintains staff-level settings. The allowed slot lengths are validated by the domain entity.</summary>
public class StaffService(IStaffMemberRepository staffRepository, IUnitOfWork unitOfWork) : IStaffService
{
    public async Task<StaffSlotDurationDto> SetSlotDurationAsync(Guid staffId, int slotDurationMinutes, CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository.GetByIdAsync(staffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff member {staffId} was not found.");

        // Throws ArgumentOutOfRangeException for a length that is not allowed.
        staff.SetSlotDuration(slotDurationMinutes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StaffSlotDurationDto(staff.Id, staff.SlotDurationMinutes);
    }
}
