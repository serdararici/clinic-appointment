using Clinic.Application.Auth;
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
    IHealthOrganizationRepository organizationRepository,
    IDepartmentRepository departmentRepository,
    IPricingStrategy pricingStrategy,
    IConfirmationPolicy confirmationPolicy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    AccessGuard guard) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        // The patient is the logged-in patient: taken from the token, never from the request.
        var patientId = guard.RequirePatientId();

        // Reject zone-less date-times up front: all stored times are UTC, so guessing would shift bookings.
        var start = UtcDateTime.Require(request.StartUtc, nameof(request.StartUtc));

        // Same definition as the slot list: a slot that has already started is Past. A start exactly
        // at "now" is still allowed, so the list and the booking agree.
        if (start < timeProvider.GetUtcNow().UtcDateTime)
        {
            throw new PastBookingException(start);
        }

        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {patientId} was not found.");

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

    public async Task<PricePreviewDto> PreviewPriceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        // The patient is the logged-in patient (from the token), exactly as when booking.
        var patientId = guard.RequirePatientId();

        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {patientId} was not found.");

        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {serviceId} was not found.");

        // The same strategy instance that prices a booking, so the preview cannot drift from the stored price.
        return new PricePreviewDto(service.Id, service.BasePrice, pricingStrategy.CalculatePrice(service, patient), patient.HasInsurance);
    }

    public async Task ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} was not found.");

        await guard.EnsureCanDecideAsync(appointment, cancellationToken);

        appointment.Confirm();
        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} was not found.");

        await guard.EnsureCanDecideAsync(appointment, cancellationToken);

        appointment.Reject();
        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} was not found.");

        await guard.EnsureCanCancelAsync(appointment, cancellationToken);

        appointment.Cancel();
        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDetailDto>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnPatient(patientId); // a patient reads only their own list

        _ = await patientRepository.GetByIdAsync(patientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Patient {patientId} was not found.");

        return await ToDetailsAsync(await appointmentRepository.GetByPatientIdAsync(patientId, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDetailDto>> GetByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId); // a doctor reads only their own list

        _ = await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

        return await ToDetailsAsync(await appointmentRepository.GetByStaffMemberIdAsync(doctorId, cancellationToken), cancellationToken);
    }

    // Looks up everything the list needs once per distinct id (not once per appointment).
    private async Task<IReadOnlyList<AppointmentDetailDto>> ToDetailsAsync(IReadOnlyList<Appointment> appointments, CancellationToken cancellationToken)
    {
        var services = (await serviceRepository.GetByIdsAsync(
            appointments.Select(a => a.ServiceId).Distinct().ToList(), cancellationToken)).ToDictionary(s => s.Id);

        var patients = (await patientRepository.GetByIdsAsync(
            appointments.Select(a => a.PatientId).Distinct().ToList(), cancellationToken)).ToDictionary(p => p.Id);

        // A staff member who has since been deleted simply has no name any more.
        var staffNames = new Dictionary<Guid, string>();
        foreach (var staffId in appointments.Where(a => a.StaffMemberId is not null).Select(a => a.StaffMemberId!.Value).Distinct())
        {
            var staff = await doctorRepository.GetByIdAsync(staffId, cancellationToken);
            if (staff is not null)
            {
                staffNames[staffId] = staff.FullName;
            }
        }

        // The branch is the one that owns the service's department (also true for examinations).
        var branchNames = new Dictionary<Guid, string>();
        var departmentNames = new Dictionary<Guid, string>();
        var organizationNames = new Dictionary<Guid, string>();
        var organizationNamesByService = new Dictionary<Guid, string>();
        foreach (var service in services.Values)
        {
            var branch = await clinicFacilityRepository.GetByServiceIdAsync(service.Id, cancellationToken);
            if (branch is null)
            {
                continue;
            }

            branchNames[service.Id] = branch.Name;
            var department = await departmentRepository.GetByIdAsync(service.DepartmentId, cancellationToken);
            departmentNames[service.Id] = department?.Name ?? string.Empty;

            // Look each organization up once, however many of its branches and appointments the list has.
            if (!organizationNames.ContainsKey(branch.OrganizationId))
            {
                var organization = await organizationRepository.GetByIdAsync(branch.OrganizationId, cancellationToken);
                organizationNames[branch.OrganizationId] = organization?.Name ?? string.Empty;
            }

            organizationNamesByService[service.Id] = organizationNames[branch.OrganizationId];
        }

        return appointments
            .OrderBy(a => a.ScheduledStartUtc)
            .Select(a =>
            {
                var service = services.GetValueOrDefault(a.ServiceId);
                return new AppointmentDetailDto(
                    a.Id,
                    a.PatientId,
                    patients.TryGetValue(a.PatientId, out var patient) ? patient.FullName : string.Empty,
                    a.StaffMemberId,
                    a.StaffMemberId is { } staffId && staffNames.TryGetValue(staffId, out var staffName) ? staffName : null,
                    organizationNamesByService.GetValueOrDefault(a.ServiceId, string.Empty),
                    branchNames.GetValueOrDefault(a.ServiceId, string.Empty),
                    departmentNames.GetValueOrDefault(a.ServiceId, string.Empty),
                    a.ServiceId,
                    service?.Name ?? string.Empty,
                    service?.ServiceType ?? ServiceType.Examination,
                    // Stored values come back with an Unspecified kind; mark them UTC so they serialize with a 'Z'.
                    DateTime.SpecifyKind(a.ScheduledStartUtc, DateTimeKind.Utc),
                    DateTime.SpecifyKind(a.ScheduledEndUtc, DateTimeKind.Utc),
                    a.Price,
                    a.Status);
            })
            .ToList();
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
