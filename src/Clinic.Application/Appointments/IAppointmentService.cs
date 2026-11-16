namespace Clinic.Application.Appointments;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    Task ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task RejectAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
