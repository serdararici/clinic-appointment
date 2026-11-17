namespace Clinic.Domain.Scheduling;

/// <summary>One bookable time range (UTC) with its status.</summary>
public record TimeSlot(DateTime StartUtc, DateTime EndUtc, SlotStatus Status);
