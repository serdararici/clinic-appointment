using Clinic.Application.Common;
using Clinic.Application.Exceptions;
using Clinic.Domain.Appointments;
using Clinic.Domain.Repositories;
using Clinic.Domain.Strategies;

namespace Clinic.Application.Appointments;

/// <summary>
/// Orchestrates appointment booking: loads the relevant aggregates, enforces the
/// availability and conflict rules, and delegates pricing/approval decisions to
/// the configured strategies (Strategy pattern) before persisting via repositories.
/// </summary>
public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IDoctorRepository doctorRepository,
    IPatientRepository patientRepository,
    IServiceRepository serviceRepository,
    IClinicFacilityRepository clinicFacilityRepository,
    IPricingStrategy pricingStrategy,
    IConfirmationPolicy confirmationPolicy,
    IUnitOfWork unitOfWork) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        // Reject zone-less date-times up front: all stored times are UTC, so guessing would shift bookings.
        var start = UtcDateTime.Require(request.StartUtc, nameof(request.StartUtc));

        var doctor = await doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {request.DoctorId} was not found.");

        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {request.PatientId} was not found.");

        var service = await serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {request.ServiceId} was not found.");

        if (!doctor.OffersService(service.Id))
        {
            throw new ServiceNotOfferedException(doctor.Id, service.Id);
        }

        var end = start + service.Duration;

        // Working hours are clinic wall-clock times; the facility converts the UTC range to local time.
        var facility = await clinicFacilityRepository.GetByIdAsync(doctor.ClinicFacilityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Clinic {doctor.ClinicFacilityId} was not found.");

        if (!facility.IsOpenDuring(start, end))
        {
            throw new OutsideWorkingHoursException(facility.Id, start, end);
        }

        if (!doctor.IsAvailableDuring(start, end))
        {
            throw new NotAvailableException(doctor.Id, start, end);
        }

        if (await appointmentRepository.HasConflictAsync(doctor.Id, start, end, cancellationToken))
        {
            throw new AppointmentConflictException(doctor.Id, start, end);
        }

        var price = pricingStrategy.CalculatePrice(service, patient);
        var initialStatus = confirmationPolicy.DetermineInitialStatus();

        var appointment = new Appointment(patient.Id, doctor.Id, service.Id, start, end, price, initialStatus);

        await appointmentRepository.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(appointment);
    }

    public async Task ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} was not found.");

        appointment.Confirm();
        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} was not found.");

        appointment.Reject();
        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AppointmentDto ToDto(Appointment appointment) => new(
        appointment.Id,
        appointment.PatientId,
        appointment.StaffMemberId,
        appointment.ServiceId,
        appointment.ScheduledStartUtc,
        appointment.ScheduledEndUtc,
        appointment.Price,
        appointment.Status);
}
