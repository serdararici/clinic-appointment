using Clinic.Application.Common;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Availability;

/// <summary>
/// Lets a doctor or clinic manager maintain a doctor's availability ranges.
/// Ranges stay date-time ranges (UTC); overlap rules live on the <see cref="Doctor"/> entity.
/// </summary>
public class DoctorAvailabilityService(
    IDoctorRepository doctorRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork) : IDoctorAvailabilityService
{
    public async Task<IReadOnlyList<AvailabilityDto>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);

        return doctor.AvailabilitySlots
            .OrderBy(s => s.StartUtc)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AvailabilityDto> AddAvailabilityAsync(Guid doctorId, AddAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);

        var start = UtcDateTime.Require(request.StartUtc, nameof(request.StartUtc));
        var end = UtcDateTime.Require(request.EndUtc, nameof(request.EndUtc));

        // Validates the range and rejects overlaps with the doctor's existing ranges.
        var slot = doctor.AddAvailability(start, end);

        doctorRepository.AddAvailability(slot);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(slot);
    }

    public async Task RemoveAvailabilityAsync(Guid doctorId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);

        var slot = doctor.AvailabilitySlots.FirstOrDefault(s => s.Id == slotId)
            ?? throw new KeyNotFoundException($"Availability {slotId} was not found for doctor {doctorId}.");

        // Removing a range that still holds a live (pending/confirmed) appointment would
        // leave that appointment outside any availability window.
        if (await appointmentRepository.HasConflictAsync(doctorId, slot.StartUtc, slot.EndUtc, cancellationToken))
        {
            throw new InvalidOperationException("Cannot remove an availability range that still has active appointments.");
        }

        doctor.RemoveAvailability(slotId);
        doctorRepository.RemoveAvailability(slot);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Doctor> LoadDoctorAsync(Guid doctorId, CancellationToken cancellationToken) =>
        await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
        ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

    private static AvailabilityDto ToDto(DoctorAvailability slot) => new(
        slot.Id,
        slot.StaffMemberId,
        // EF returns Unspecified kind; mark as UTC so it serializes with a 'Z'.
        DateTime.SpecifyKind(slot.StartUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(slot.EndUtc, DateTimeKind.Utc));
}
