using Clinic.Domain.Enums;
using Clinic.Domain.Repositories;
using Clinic.Domain.Scheduling;

namespace Clinic.Application.Slots;

/// <summary>
/// Lists the bookable slots for a day. It only loads the data (doctor or service, branch, the day's
/// active appointments); the grid rules themselves are in <see cref="SlotGrid"/> in the Domain.
/// </summary>
public class SlotService(
    IDoctorRepository doctorRepository,
    IServiceRepository serviceRepository,
    IClinicFacilityRepository clinicFacilityRepository,
    IAppointmentRepository appointmentRepository,
    TimeProvider timeProvider) : ISlotService
{
    public async Task<IReadOnlyList<SlotDto>> GetExaminationSlotsAsync(Guid doctorId, DateOnly localDate, CancellationToken cancellationToken = default)
    {
        var doctor = await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

        // A deactivated doctor cannot be booked, so offering slots would be misleading.
        if (!doctor.IsActive)
        {
            return [];
        }

        var clinic = await clinicFacilityRepository.GetByIdAsync(doctor.ClinicFacilityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Clinic {doctor.ClinicFacilityId} was not found.");

        var (dayStart, dayEnd) = SlotGrid.LocalDayRangeUtc(localDate);
        var appointments = await appointmentRepository.GetActiveByStaffMemberAsync(doctor.Id, dayStart, dayEnd, cancellationToken);

        return ToDtos(SlotGrid.BuildExaminationSlots(doctor, clinic, localDate, appointments, timeProvider.GetUtcNow().UtcDateTime));
    }

    public async Task<IReadOnlyList<SlotDto>> GetDiagnosticSlotsAsync(Guid serviceId, DateOnly localDate, CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {serviceId} was not found.");

        if (service.ServiceType != ServiceType.Diagnostic)
        {
            throw new ArgumentException("Only diagnostic services have a service slot grid; book examinations through a doctor's slots.", nameof(serviceId));
        }

        var clinic = await clinicFacilityRepository.GetByServiceIdAsync(service.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No branch was found for service {serviceId}.");

        var (dayStart, dayEnd) = SlotGrid.LocalDayRangeUtc(localDate);
        var appointments = await appointmentRepository.GetActiveByServiceAsync(service.Id, dayStart, dayEnd, cancellationToken);

        return ToDtos(SlotGrid.BuildDiagnosticSlots(service, clinic, localDate, appointments, timeProvider.GetUtcNow().UtcDateTime));
    }

    // Arithmetic and EF results carry an Unspecified kind; mark them UTC so they serialize with a 'Z'.
    private static List<SlotDto> ToDtos(IEnumerable<TimeSlot> slots) => slots
        .Select(s => new SlotDto(
            DateTime.SpecifyKind(s.StartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(s.EndUtc, DateTimeKind.Utc),
            s.Status))
        .ToList();
}
