using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Enums;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;

namespace Clinic.Infrastructure.Seed;

/// <summary>
/// The demo appointments: a history and a calendar for the demo accounts (Ahmet Çelik, Dr. Hakan Yıldız and Dr. Kemal
/// Aslan), planned relative to the day the seed runs. They are written straight through the DbContext, so the rule that
/// forbids booking in the past does not apply to the past ones, but every other rule is kept: times lie on the doctor's
/// slot grid (the default schedule: weekdays 09:00-12:00 and 13:00-16:00, slots anchored at the window start), diagnostic
/// times lie on the service grid, nothing overlaps, prices come from the real pricing strategy, and every upcoming
/// appointment is on a weekday inside the 14-day booking window (so the default availability covers it).
/// </summary>
internal static class DemoAppointments
{
    // The two doctors of the demo and the patient whose history is shown.
    private const string Hakan = "hakan.yildiz@sakaryasehir.example";
    private const string Kemal = "kemal.aslan@sakaryasehir.example";
    private const string SelinAydin = "selin.aydin@sakaryasehir.example";
    private const string CemUysal = "cem.uysal@sakaryasehir.example";
    private const string Ahmet = "ahmet.celik@example.com";
    private const string Fatma = "fatma.yildirim@example.com";

    /// <summary>
    /// One planned appointment. <paramref name="Day"/> counts weekdays: -1 is the last weekday before today, 0 the first
    /// weekday after today, 1 the second, and so on. <paramref name="Provider"/> is a doctor's email, or null for a
    /// diagnostic service (booked without a doctor).
    /// </summary>
    private sealed record Plan(string PatientEmail, string? Provider, string ServiceName, int Day, string Time, AppointmentStatus Status);

    // A plan with this day is for today (Istanbul day), and only when today is a weekday.
    private const int TodayMarker = int.MinValue;

    private static Plan Past(string patient, string? provider, string service, int weekdaysAgo, string time, AppointmentStatus status) =>
        new(patient, provider, service, -weekdaysAgo, time, status);

    private static Plan Upcoming(string patient, string? provider, string service, int nthWeekday, string time, AppointmentStatus status) =>
        new(patient, provider, service, nthWeekday, time, status);

    /// <summary>An appointment today at a fixed Istanbul time, whatever the time of the seed run: some are already past, some later.</summary>
    private static Plan Today(string patient, string provider, string service, string time, AppointmentStatus status) =>
        new(patient, provider, service, TodayMarker, time, status);

    private static readonly Plan[] Plans =
    [
        // ---- Today (weekdays only): the doctors have a day in progress ----
        Today("zeynep.arslan@example.com", Hakan, "Genel Muayene", "09:00", AppointmentStatus.Confirmed),
        Today(Ahmet, Hakan, "Kontrol Muayenesi", "10:30", AppointmentStatus.Confirmed),
        Today("hasan.cetin@example.com", Hakan, "Genel Muayene", "14:00", AppointmentStatus.Confirmed),
        Today("emre.sahin@example.com", Hakan, "Kontrol Muayenesi", "15:30", AppointmentStatus.Cancelled),
        Today("ayse.korkmaz@example.com", Kemal, "Genel Muayene", "10:00", AppointmentStatus.Pending),
        Today("elif.demir@example.com", Kemal, "Genel Muayene", "11:30", AppointmentStatus.Confirmed),
        Today("selin.aydogan@example.com", Kemal, "Kontrol Muayenesi", "14:30", AppointmentStatus.Confirmed),

        // ---- Ahmet Çelik (insured): six past appointments in different departments (one diagnostic) and three upcoming ----
        Past(Ahmet, Hakan, "Genel Muayene", 1, "09:30", AppointmentStatus.Confirmed),
        Past(Ahmet, SelinAydin, "Genel Muayene", 3, "10:20", AppointmentStatus.Confirmed),
        Past(Ahmet, Kemal, "Genel Muayene", 4, "10:00", AppointmentStatus.Rejected),
        Past(Ahmet, null, "Röntgen", 6, "10:00", AppointmentStatus.Confirmed),
        Past(Ahmet, Kemal, "Kontrol Muayenesi", 7, "11:00", AppointmentStatus.Cancelled),
        Past(Ahmet, CemUysal, "Genel Muayene", 8, "11:00", AppointmentStatus.Confirmed),
        Upcoming(Ahmet, Kemal, "Genel Muayene", 1, "11:00", AppointmentStatus.Pending),
        Upcoming(Ahmet, Hakan, "Kontrol Muayenesi", 2, "10:30", AppointmentStatus.Confirmed),
        Upcoming(Ahmet, null, "Ultrasonografi", 3, "14:20", AppointmentStatus.Confirmed),

        // ---- Dr. Hakan Yıldız (confirms at once): only Confirmed and Cancelled ----
        Past("mehmet.yilmaz@example.com", Hakan, "Genel Muayene", 2, "10:00", AppointmentStatus.Confirmed),
        Past("elif.demir@example.com", Hakan, "Kontrol Muayenesi", 3, "14:00", AppointmentStatus.Confirmed),
        Past("mustafa.kaya@example.com", Hakan, "Genel Muayene", 5, "11:00", AppointmentStatus.Cancelled),
        Upcoming("zeynep.arslan@example.com", Hakan, "Genel Muayene", 0, "09:00", AppointmentStatus.Confirmed),
        Upcoming("hasan.cetin@example.com", Hakan, "Genel Muayene", 2, "14:30", AppointmentStatus.Confirmed),
        Upcoming("ayse.korkmaz@example.com", Hakan, "Kontrol Muayenesi", 3, "09:30", AppointmentStatus.Confirmed),
        Upcoming("emre.sahin@example.com", Hakan, "Genel Muayene", 4, "13:00", AppointmentStatus.Cancelled),

        // ---- Dr. Kemal Aslan (approves himself): four Pending upcoming appointments to approve or reject ----
        Past("selin.aydogan@example.com", Kemal, "Genel Muayene", 2, "14:30", AppointmentStatus.Confirmed),
        Past(Fatma, Kemal, "Genel Muayene", 6, "09:00", AppointmentStatus.Confirmed),
        Upcoming("mehmet.yilmaz@example.com", Kemal, "Genel Muayene", 0, "10:00", AppointmentStatus.Pending),
        Upcoming("elif.demir@example.com", Kemal, "Kontrol Muayenesi", 2, "13:30", AppointmentStatus.Pending),
        Upcoming("mustafa.kaya@example.com", Kemal, "Genel Muayene", 3, "15:00", AppointmentStatus.Pending),
        Upcoming("zeynep.arslan@example.com", Kemal, "Genel Muayene", 5, "10:30", AppointmentStatus.Confirmed),
    ];

    public static IReadOnlyList<Appointment> Build(
        DateTime utcNow,
        IEnumerable<Doctor> doctors,
        IEnumerable<Patient> patients,
        Department merkezRadiology)
    {
        var doctorsByEmail = doctors.ToDictionary(d => d.Email, StringComparer.OrdinalIgnoreCase);
        var patientsByEmail = patients.ToDictionary(p => p.Email, StringComparer.OrdinalIgnoreCase);
        var pricing = new InsuranceDiscountPricingStrategy(); // the strategy the API registers

        var today = BookingRules.Today(utcNow);
        var appointments = new List<Appointment>();

        foreach (var plan in Plans)
        {
            // Nothing is booked for a weekend day: the clinics (and the doctors' schedules) are closed.
            if (plan.Day == TodayMarker && !DefaultSchedule.IsWorkingDay(today))
            {
                continue;
            }

            var patient = patientsByEmail[plan.PatientEmail];
            var date = plan.Day == TodayMarker ? today : WeekdayAt(today, plan.Day);
            var start = ClinicTimeZone.ToUtc(date.ToDateTime(TimeOnly.Parse(plan.Time)));

            if (plan.Provider is null)
            {
                var service = merkezRadiology.Services.Single(s => s.Name == plan.ServiceName);
                appointments.Add(new Appointment(patient.Id, null, service.Id, start, start + service.Duration, pricing.CalculatePrice(service, patient), plan.Status));
            }
            else
            {
                var doctor = doctorsByEmail[plan.Provider];
                var service = doctor.ServicesProvided.Single(s => s.Name == plan.ServiceName);
                appointments.Add(new Appointment(
                    patient.Id, doctor.Id, service.Id, start, start.AddMinutes(doctor.SlotDurationMinutes), pricing.CalculatePrice(service, patient), plan.Status));
            }
        }

        return appointments;
    }

    /// <summary>
    /// The weekday that is <paramref name="index"/> weekdays from today: 0 is the first weekday after today, 1 the next one;
    /// -1 is the last weekday before today, -2 the one before. A weekend is skipped, so a seed run on a Saturday starts on Monday.
    /// </summary>
    internal static DateOnly WeekdayAt(DateOnly today, int index)
    {
        var step = index < 0 ? -1 : 1;
        var remaining = index < 0 ? -index : index + 1;
        var date = today;
        while (remaining > 0)
        {
            date = date.AddDays(step);
            if (DefaultSchedule.IsWorkingDay(date))
            {
                remaining--;
            }
        }

        return date;
    }
}
