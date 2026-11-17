namespace Clinic.Domain.Scheduling;

/// <summary>State of a bookable slot as shown to the person choosing a time.</summary>
public enum SlotStatus
{
    /// <summary>Free and in the future: can be booked.</summary>
    Available,

    /// <summary>Overlaps an active (not cancelled, not rejected) appointment.</summary>
    Booked,

    /// <summary>Already started; it can no longer be chosen, whether or not it was booked.</summary>
    Past
}
