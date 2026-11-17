namespace Clinic.Application.Slots;

public interface ISlotService
{
    /// <summary>All examination slots of a doctor on a clinic-local date (Europe/Istanbul).</summary>
    Task<IReadOnlyList<SlotDto>> GetExaminationSlotsAsync(Guid doctorId, DateOnly localDate, CancellationToken cancellationToken = default);

    /// <summary>All diagnostic slots of a service on a clinic-local date; the service must be Diagnostic.</summary>
    Task<IReadOnlyList<SlotDto>> GetDiagnosticSlotsAsync(Guid serviceId, DateOnly localDate, CancellationToken cancellationToken = default);
}
