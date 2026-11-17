using Clinic.Application.Common;
using Clinic.Application.Exceptions;
using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Enums;
using Clinic.Domain.Repositories;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;

namespace Clinic.Application.Appointments;

/// <summary>
/// Orchestrates appointment booking: loads the relevant aggregates, enforces the slot,
/// working-hours, availability and conflict rules, and delegates pricing/approval decisions
/// to the configured strategies (Strategy pattern) before persisting via repositories.
/// Examination and Diagnostic services follow different flows (see the two private builders).
/// </summary>
public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IDoctorRepository doctorRepository,
    IPatientRepository patientRepository,
    IServiceRepository serviceRepository,
    IClinicFacilityRepository clinicFacilityRepository,
    IPricingStrategy pricingStrategy,
    IConfirmationPolicy confirmationPolicy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        // Reject zone-less date-times up front: all stored times are UTC, so guessing would shift bookings.
        var start = UtcDateTime.Require(request.StartUtc, nameof(request.StartUtc));

        // Same definition as the slot list: a slot that has already started is Past. A start exactly
        // at "now" is still allowed, so the list and the booking agree.
        if (start < timeProvider.GetUtcNow().UtcDateTime)
        {
            throw new PastBookingException(start);
        }

        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {request.PatientId} was not found.");

        var service = await serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {request.ServiceId} was not found.");

        // The service type decides the flow: examinations go through a doctor's slot grid,
        // diagnostics through the branch's service grid with no staff member.
        var appointment = service.ServiceType == ServiceType.Examination
            ? await BuildExaminationAppointmentAsync(request, patient, service, start, cancellationToken)
            : await BuildDiagnosticAppointmentAsync(patient, service, start, cancellationToken);

        await appointmentRepository.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(appointment);
    }

    /// <summary>Examination: a chosen doctor, exactly one of that doctor's slots.</summary>
    private async Task<Appointment> BuildExaminationAppointmentAsync(
        CreateAppointmentRequest request, Patient patient, Service service, DateTime start, CancellationToken cancellationToken)
    {
        var doctorId = request.DoctorId
            ?? throw new ArgumentException("A doctor is required to book an examination.", nameof(request));

        var doctor = await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

        // A deactivated doctor keeps their history but can no longer be booked.
        if (!doctor.IsActive)
        {
            throw new StaffInactiveException(doctor.Id);
        }

        if (!doctor.OffersService(service.Id))
        {
            throw new ServiceNotOfferedException(doctor.Id, service.Id);
        }

        // The appointment occupies exactly one slot, so its length is the doctor's slot duration
        // (the service's own duration is not used for examinations).
        var end = start + TimeSpan.FromMinutes(doctor.SlotDurationMinutes);

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

        // After the availability check on purpose: a time outside any availability window gets the
        // more specific NotAvailable error, and alignment is measured from the window that holds the slot.
        if (!SlotGrid.IsExaminationSlotStart(doctor, start))
        {
            throw new SlotNotAlignedException(start);
        }

        if (await appointmentRepository.HasConflictAsync(doctor.Id, start, end, cancellationToken))
        {
            throw new AppointmentConflictException(doctor.Id, start, end);
        }

        return new Appointment(
            patient.Id, doctor.Id, service.Id, start, end,
            pricingStrategy.CalculatePrice(service, patient),
            confirmationPolicy.DetermineInitialStatus());
    }

    /// <summary>Diagnostic: no staff member; a slot of the service's fixed duration inside the branch's hours.</summary>
    private async Task<Appointment> BuildDiagnosticAppointmentAsync(
        Patient patient, Service service, DateTime start, CancellationToken cancellationToken)
    {
        // The branch is the one that owns the service's department.
        var facility = await clinicFacilityRepository.GetByServiceIdAsync(service.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No branch was found for service {service.Id}.");

        var end = start + service.Duration;

        if (!facility.IsOpenDuring(start, end))
        {
            throw new OutsideWorkingHoursException(facility.Id, start, end);
        }

        if (!SlotGrid.IsDiagnosticSlotStart(service, facility, start))
        {
            throw new SlotNotAlignedException(start);
        }

        if (await appointmentRepository.HasServiceConflictAsync(service.Id, start, end, cancellationToken))
        {
            throw new ServiceSlotConflictException(service.Id, start, end);
        }

        // StaffMemberId stays null: the appointment is unassigned.
        return new Appointment(
            patient.Id, null, service.Id, start, end,
            pricingStrategy.CalculatePrice(service, patient),
            confirmationPolicy.DetermineInitialStatus());
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
