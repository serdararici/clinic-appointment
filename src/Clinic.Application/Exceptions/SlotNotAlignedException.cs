namespace Clinic.Application.Exceptions;

/// <summary>Thrown when a requested start time is not the start of a bookable slot on the relevant grid.</summary>
public class SlotNotAlignedException(DateTime startUtc)
    : Exception($"{startUtc:O} is not the start of a bookable slot. Use the slots endpoint to list valid start times.")
{
}
