using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Users;

namespace Clinic.Domain.Scheduling;

/// <summary>
/// Builds the grids of bookable slots and decides whether a requested start time sits on a grid.
/// The slot lists (what the patient sees) and the alignment checks (what booking enforces) live
/// together so they cannot drift apart. Everything is UTC; "dates" are clinic-local calendar days.
/// </summary>
public static class SlotGrid
{
    /// <summary>The UTC range [start, end) covered by a clinic-local calendar day.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) LocalDayRangeUtc(DateOnly localDate) => (
        ClinicTimeZone.ToUtc(localDate.ToDateTime(TimeOnly.MinValue)),
        ClinicTimeZone.ToUtc(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue)));

    // ---- Examination: the doctor's availability cut into slots of the doctor's slot duration ----

    /// <summary>
    /// Cuts each of the doctor's availability windows that touch <paramref name="localDate"/> into
    /// slots of the doctor's slot duration, starting at the window's start (a window that began on an
    /// earlier day is cut from local midnight). A trailing piece shorter than one slot is dropped, and
    /// so is any slot not entirely inside the branch's working hours.
    /// </summary>
    public static IReadOnlyList<TimeSlot> BuildExaminationSlots(
        StaffMember doctor,
        ClinicFacility clinic,
        DateOnly localDate,
        IEnumerable<Appointment> appointments,
        DateTime nowUtc)
    {
        var (dayStart, dayEnd) = LocalDayRangeUtc(localDate);
        var step = TimeSpan.FromMinutes(doctor.SlotDurationMinutes);
        var active = appointments.Where(a => a.IsActive).ToList();
        var slots = new List<TimeSlot>();

        foreach (var window in doctor.AvailabilitySlots.OrderBy(w => w.StartUtc))
        {
            if (!window.Overlaps(dayStart, dayEnd))
            {
                continue;
            }

            var anchor = Max(window.StartUtc, dayStart);
            var limit = Min(window.EndUtc, dayEnd);

            for (var start = anchor; start + step <= limit; start += step)
            {
                var end = start + step;
                if (clinic.IsOpenDuring(start, end))
                {
                    slots.Add(new TimeSlot(start, end, StatusOf(start, end, active, nowUtc)));
                }
            }
        }

        return slots;
    }

    /// <summary>
    /// Whether <paramref name="startUtc"/> is the start of one of the doctor's examination slots
    /// (a whole number of slot durations after the start of the availability window that holds it).
    /// False when no availability window covers the slot.
    /// </summary>
    public static bool IsExaminationSlotStart(StaffMember doctor, DateTime startUtc)
    {
        var step = TimeSpan.FromMinutes(doctor.SlotDurationMinutes);
        var window = doctor.AvailabilitySlots.FirstOrDefault(w => w.Contains(startUtc, startUtc + step));
        if (window is null)
        {
            return false;
        }

        var (dayStart, _) = LocalDayRangeUtc(DateOnly.FromDateTime(ClinicTimeZone.ToLocal(startUtc)));
        var anchor = Max(window.StartUtc, dayStart);

        return (startUtc - anchor).Ticks % step.Ticks == 0;
    }

    // ---- Diagnostic: the branch's working hours cut into slots of the service's fixed duration ----

    /// <summary>
    /// Cuts each of the branch's opening ranges on <paramref name="localDate"/> into slots of the
    /// service's fixed duration, starting at opening time. A slot whose end would pass closing is dropped.
    /// </summary>
    public static IReadOnlyList<TimeSlot> BuildDiagnosticSlots(
        Service service,
        ClinicFacility clinic,
        DateOnly localDate,
        IEnumerable<Appointment> appointments,
        DateTime nowUtc)
    {
        var active = appointments.Where(a => a.IsActive).ToList();
        var slots = new List<TimeSlot>();

        foreach (var (opens, closes) in clinic.GetWorkingRangesUtc(localDate))
        {
            for (var start = opens; start + service.Duration <= closes; start += service.Duration)
            {
                var end = start + service.Duration;
                slots.Add(new TimeSlot(start, end, StatusOf(start, end, active, nowUtc)));
            }
        }

        return slots;
    }

    /// <summary>
    /// Whether <paramref name="startUtc"/> is the start of one of the branch's diagnostic slots for
    /// the service: a whole number of service durations after opening, with the slot ending by closing.
    /// </summary>
    public static bool IsDiagnosticSlotStart(Service service, ClinicFacility clinic, DateTime startUtc)
    {
        var localDate = DateOnly.FromDateTime(ClinicTimeZone.ToLocal(startUtc));
        var end = startUtc + service.Duration;

        foreach (var (opens, closes) in clinic.GetWorkingRangesUtc(localDate))
        {
            if (startUtc >= opens && end <= closes)
            {
                return (startUtc - opens).Ticks % service.Duration.Ticks == 0;
            }
        }

        return false;
    }

    // Past wins over Booked: a slot that has already started can no longer be chosen either way.
    private static SlotStatus StatusOf(DateTime start, DateTime end, List<Appointment> active, DateTime nowUtc)
    {
        if (start < nowUtc)
        {
            return SlotStatus.Past;
        }

        return active.Any(a => a.OverlapsWith(start, end)) ? SlotStatus.Booked : SlotStatus.Available;
    }

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

    private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
}
