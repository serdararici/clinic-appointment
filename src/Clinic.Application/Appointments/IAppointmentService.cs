namespace Clinic.Application.Appointments;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>The amount the logged-in patient would pay for a service, without creating anything.</summary>
    Task<PricePreviewDto> PreviewPriceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    Task ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task RejectAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending or confirmed appointment (InvalidOperationException if it is already rejected or cancelled).</summary>
    Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>A patient's appointments in every status, ordered by start time.</summary>
    Task<IReadOnlyList<AppointmentDetailDto>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>A doctor's appointments in every status, ordered by start time.</summary>
    Task<IReadOnlyList<AppointmentDetailDto>> GetByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
