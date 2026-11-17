namespace Clinic.Application.Availability;

public interface IDoctorAvailabilityService
{
    Task<IReadOnlyList<AvailabilityDto>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<AvailabilityDto> AddAvailabilityAsync(Guid doctorId, AddAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task RemoveAvailabilityAsync(Guid doctorId, Guid slotId, CancellationToken cancellationToken = default);
}
