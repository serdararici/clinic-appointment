namespace Clinic.Application.Staff;

/// <summary>Output DTO confirming a staff member's slot duration.</summary>
public record StaffSlotDurationDto(Guid StaffId, int SlotDurationMinutes);
