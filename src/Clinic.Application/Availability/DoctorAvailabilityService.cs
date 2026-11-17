using Clinic.Application.Auth;
using Clinic.Application.Common;
using Clinic.Domain.Common;
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
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    AccessGuard guard) : IDoctorAvailabilityService
{
    public async Task<IReadOnlyList<AvailabilityDto>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId); // a doctor manages only their own availability

        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);

        return doctor.AvailabilitySlots
            .OrderBy(s => s.StartUtc)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AvailabilityDto> AddAvailabilityAsync(Guid doctorId, AddAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId); // a doctor manages only their own availability

        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);

        var start = UtcDateTime.Require(request.StartUtc, nameof(request.StartUtc));
        var end = UtcDateTime.Require(request.EndUtc, nameof(request.EndUtc));

        // Validates the range and rejects overlaps with the doctor's existing ranges.
        var slot = doctor.AddAvailability(start, end);

        doctorRepository.AddAvailability(slot);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(slot);
    }

    /// <summary>Longest date range one bulk request may cover (a year), so a typo cannot create thousands of rows.</summary>
    public const int MaxBulkDays = 366;

    public async Task<BulkAvailabilityResultDto> AddBulkAvailabilityAsync(Guid doctorId, BulkAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId); // a doctor manages only their own availability

        // Validate the whole request before touching the doctor, so a bad request creates nothing.
        if (request.ToDate < request.FromDate)
        {
            throw new ArgumentException("ToDate must not be before FromDate.", nameof(request));
        }

        if (request.ToDate.DayNumber - request.FromDate.DayNumber + 1 > MaxBulkDays)
        {
            throw new ArgumentException($"The date range cannot be longer than {MaxBulkDays} days.", nameof(request));
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException("EndTime must be after StartTime.", nameof(request));
        }

        var weekdays = ParseWeekdays(request.Weekdays);

        var doctor = await LoadDoctorAsync(doctorId, cancellationToken);
        var created = new List<AvailabilityDto>();
        var skipped = new List<DateOnly>();
        var past = new List<DateOnly>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            if (!weekdays.Contains(date.DayOfWeek))
            {
                continue;
            }

            // The window is Turkey wall-clock time; storage stays UTC.
            var start = ClinicTimeZone.ToUtc(date.ToDateTime(request.StartTime));
            var end = ClinicTimeZone.ToUtc(date.ToDateTime(request.EndTime));

            // A range that has already started cannot be booked any more (same rule as a Past slot).
            if (start < now)
            {
                past.Add(date);
                continue;
            }

            if (doctor.AvailabilitySlots.Any(s => s.Overlaps(start, end)))
            {
                skipped.Add(date);
                continue;
            }

            var slot = doctor.AddAvailability(start, end);
            doctorRepository.AddAvailability(slot);
            created.Add(ToDto(slot));
        }

        if (created.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BulkAvailabilityResultDto(created, skipped, past);
    }

    private static HashSet<DayOfWeek> ParseWeekdays(IReadOnlyList<string>? names)
    {
        if (names is null || names.Count == 0)
        {
            throw new ArgumentException("At least one weekday is required.", nameof(names));
        }

        var result = new HashSet<DayOfWeek>();
        foreach (var name in names)
        {
            // Names only ("Monday", case-insensitive): a bare number would be ambiguous (is 1 Monday?).
            if (string.IsNullOrWhiteSpace(name) || int.TryParse(name, out _) || !Enum.TryParse<DayOfWeek>(name, ignoreCase: true, out var day))
            {
                throw new ArgumentException($"'{name}' is not a weekday name (use Monday ... Sunday).", nameof(names));
            }

            result.Add(day);
        }

        return result;
    }

    public async Task RemoveAvailabilityAsync(Guid doctorId, Guid slotId, CancellationToken cancellationToken = default)
    {
        guard.EnsureOwnDoctor(doctorId); // a doctor manages only their own availability

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
