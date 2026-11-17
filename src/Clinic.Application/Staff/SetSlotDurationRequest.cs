namespace Clinic.Application.Staff;

/// <summary>Input DTO for changing a staff member's slot duration (10, 15, 20, 30 or 60 minutes).</summary>
public record SetSlotDurationRequest(int SlotDurationMinutes);
