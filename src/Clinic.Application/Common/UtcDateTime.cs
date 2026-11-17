namespace Clinic.Application.Common;

/// <summary>
/// Normalizes incoming date-times to UTC. Everything stored is UTC, so every API input
/// must say which zone it is in; guessing would silently shift appointments by hours.
/// </summary>
public static class UtcDateTime
{
    /// <summary>
    /// JSON values ending in "Z" arrive as Utc and values with an offset (e.g. +03:00) as Local;
    /// both are converted to UTC. A value with no zone information (Unspecified) is rejected.
    /// </summary>
    public static DateTime Require(DateTime value, string paramName) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => throw new ArgumentException("Date-time must be UTC (ending in 'Z') or include a time zone offset.", paramName)
    };
}
