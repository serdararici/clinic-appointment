namespace Clinic.Application.Availability;

public interface IDoctorAvailabilityService
{
    Task<IReadOnlyList<AvailabilityDto>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<AvailabilityDto> AddAvailabilityAsync(Guid doctorId, AddAvailabilityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates one availability range per matching date in the request (clinic local time), skipping dates
    /// whose range would overlap an existing one. Rejects an invalid request (400) before changing anything.
    /// </summary>
    Task<BulkAvailabilityResultDto> AddBulkAvailabilityAsync(Guid doctorId, BulkAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task RemoveAvailabilityAsync(Guid doctorId, Guid slotId, CancellationToken cancellationToken = default);
}
