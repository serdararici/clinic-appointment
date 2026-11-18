using Clinic.Application.Auth;
using Clinic.Domain.Repositories;
using Clinic.Domain.Users;

namespace Clinic.Application.Availability;

/// <summary>The automatic work schedule switch of the logged-in doctor.</summary>
public record AutoScheduleDto(Guid DoctorId, bool Enabled);

public record SetAutoScheduleRequest(bool Enabled);

public interface IAutoScheduleService
{
    Task<AutoScheduleDto> GetAsync(Guid doctorId, CancellationToken cancellationToken = default);

    /// <summary>Switches the automatic schedule; switching it on fills the booking window right away.</summary>
    Task<AutoScheduleDto> SetAsync(Guid doctorId, bool enabled, CancellationToken cancellationToken = default);
}

public class AutoScheduleService(
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IDefaultScheduleService defaultSchedule,
    TimeProvider timeProvider,
    AccessGuard guard) : IAutoScheduleService
{
    public async Task<AutoScheduleDto> GetAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId);
        var doctor = await LoadAsync(doctorId, cancellationToken);
        return new AutoScheduleDto(doctor.Id, doctor.AutoScheduleEnabled);
    }

    public async Task<AutoScheduleDto> SetAsync(Guid doctorId, bool enabled, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId);
        var doctor = await LoadAsync(doctorId, cancellationToken);

        doctor.SetAutoSchedule(enabled);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (enabled)
        {
            await defaultSchedule.EnsureDefaultAvailabilityAsync(doctorId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        }

        return new AutoScheduleDto(doctor.Id, doctor.AutoScheduleEnabled);
    }

    private async Task<Doctor> LoadAsync(Guid doctorId, CancellationToken cancellationToken) =>
        await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
        ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");
}
