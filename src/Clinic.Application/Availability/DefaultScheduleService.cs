using Clinic.Domain.Common;
using Clinic.Domain.Repositories;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Users;

namespace Clinic.Application.Availability;

public class DefaultScheduleService(IDoctorRepository doctorRepository, IUnitOfWork unitOfWork) : IDefaultScheduleService
{
    public async Task<int> EnsureDefaultAvailabilityAsync(Guid doctorId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var doctor = await doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} was not found.");

        if (!doctor.AutoScheduleEnabled || !doctor.IsActive)
        {
            return 0;
        }

        var added = Extend(doctor, utcNow);
        if (added > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    public async Task<int> EnsureForAllAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var added = 0;
        foreach (var doctor in await doctorRepository.GetAutoScheduleDoctorsAsync(cancellationToken))
        {
            added += Extend(doctor, utcNow);
        }

        if (added > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private int Extend(Doctor doctor, DateTime utcNow)
    {
        var today = BookingRules.Today(utcNow);
        var lastDay = BookingRules.LastBookableDate(utcNow);

        // Start after the doctor's last existing availability day (past ones included), never before today. Gaps
        // before that day are days the doctor closed on purpose, so they are not refilled.
        var firstDay = today;
        if (doctor.AvailabilitySlots.Count > 0)
        {
            // The end is exclusive: a range ending exactly at midnight belongs to the day before.
            var lastExisting = doctor.AvailabilitySlots.Max(slot => DateOnly.FromDateTime(ClinicTimeZone.ToLocal(slot.EndUtc.AddTicks(-1))));
            firstDay = lastExisting.AddDays(1) > today ? lastExisting.AddDays(1) : today;
        }

        var added = 0;
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            foreach (var (startUtc, endUtc) in DefaultSchedule.RangesFor(day, doctor.SlotDurationMinutes))
            {
                if (doctor.AvailabilitySlots.Any(existing => existing.Overlaps(startUtc, endUtc)))
                {
                    continue;
                }

                // Slots get client-side ids, so tell the repository (EF) that they are new.
                doctorRepository.AddAvailability(doctor.AddAvailability(startUtc, endUtc));
                added++;
            }
        }

        return added;
    }
}
