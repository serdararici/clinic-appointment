namespace Clinic.Application.Availability;

/// <summary>Output DTO for one doctor availability range (UTC).</summary>
public record AvailabilityDto(Guid Id, Guid DoctorId, DateTime StartUtc, DateTime EndUtc);
