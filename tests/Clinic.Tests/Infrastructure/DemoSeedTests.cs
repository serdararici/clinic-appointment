using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Enums;
using Clinic.Domain.Scheduling;
using Clinic.Domain.Strategies;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Tests.Infrastructure;

/// <summary>
/// The demo appointments of the seed. The seed is built in memory (the context tracks the entities, nothing is saved), for
/// several run times, including a Saturday and a Sunday, because the appointments are planned relative to the run time.
/// </summary>
public class DemoSeedTests
{
    public static TheoryData<DateTime> RunTimes => new()
    {
        new DateTime(2026, 11, 18, 9, 30, 0, DateTimeKind.Utc),  // a Wednesday
        new DateTime(2026, 11, 20, 20, 0, 0, DateTimeKind.Utc),  // Friday evening UTC = Saturday 23:00 in Turkey
        new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc),  // a Saturday
        new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc),  // a Sunday
        new DateTime(2026, 11, 23, 0, 30, 0, DateTimeKind.Utc),  // Monday just after midnight UTC
    };

    private sealed record Seeded(ClinicDbContext Db, DateTime Now)
    {
        public IReadOnlyList<Appointment> Appointments => Db.ChangeTracker.Entries<Appointment>().Select(e => e.Entity).ToList();

        public Patient Patient(string email) => Db.ChangeTracker.Entries<Patient>().Select(e => e.Entity).Single(p => p.Email == email);

        public Doctor Doctor(string email) => Db.ChangeTracker.Entries<Doctor>().Select(e => e.Entity).Single(d => d.Email == email);

        public Service Service(Guid id) => Db.ChangeTracker.Entries<Service>().Select(e => e.Entity).Single(s => s.Id == id);

        public ClinicFacility FacilityOf(Service service) =>
            Db.ChangeTracker.Entries<ClinicFacility>().Select(e => e.Entity).Single(f => f.Departments.Any(d => d.Id == service.DepartmentId));

        public DateOnly Today => BookingRules.Today(Now);

        public DateOnly DayOf(Appointment appointment) => DateOnly.FromDateTime(ClinicTimeZone.ToLocal(appointment.ScheduledStartUtc));

        public bool IsUpcoming(Appointment appointment) => appointment.ScheduledStartUtc > Now;
    }

    private static Seeded Seed(DateTime now)
    {
        // No connection is opened: the context is only used to track the entities.
        var options = new DbContextOptionsBuilder<ClinicDbContext>().UseSqlServer("Server=(localdb)\\\\NotUsed;Database=NotUsed").Options;
        var db = new ClinicDbContext(options);
        DbInitializer.AddSeedData(db, now);
        return new Seeded(db, now);
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void NoAppointmentOverlapsAnother_ForADoctor_AServiceOrAPatient(DateTime now)
    {
        var seeded = Seed(now);
        var active = seeded.Appointments.Where(a => a.IsActive).ToList();

        foreach (var group in active.Where(a => a.StaffMemberId is not null).GroupBy(a => a.StaffMemberId))
        {
            AssertNoOverlap(group.ToList(), "doctor");
        }

        foreach (var group in active.Where(a => a.StaffMemberId is null).GroupBy(a => a.ServiceId))
        {
            AssertNoOverlap(group.ToList(), "diagnostic service");
        }

        foreach (var group in active.GroupBy(a => a.PatientId))
        {
            AssertNoOverlap(group.ToList(), "patient");
        }
    }

    private static void AssertNoOverlap(List<Appointment> appointments, string what)
    {
        for (var i = 0; i < appointments.Count; i++)
        {
            for (var j = i + 1; j < appointments.Count; j++)
            {
                Assert.False(appointments[i].OverlapsWith(appointments[j].ScheduledStartUtc, appointments[j].ScheduledEndUtc), $"two {what} appointments overlap");
            }
        }
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void EveryPrice_IsWhatTheRealPricingStrategyGives_SoInsuredPatientsPay20PercentLess(DateTime now)
    {
        var seeded = Seed(now);
        var pricing = new InsuranceDiscountPricingStrategy();

        foreach (var appointment in seeded.Appointments)
        {
            var patient = seeded.Db.ChangeTracker.Entries<Patient>().Select(e => e.Entity).Single(p => p.Id == appointment.PatientId);
            var service = seeded.Service(appointment.ServiceId);

            Assert.Equal(pricing.CalculatePrice(service, patient), appointment.Price);
            Assert.Equal(patient.HasInsurance ? Math.Round(service.BasePrice * 0.8m, 2) : service.BasePrice, appointment.Price);
        }
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void TimesLieOnTheDoctorsSlotGridOrTheServiceGrid_OnWeekdays_InsideTheBranchHours(DateTime now)
    {
        var seeded = Seed(now);

        foreach (var appointment in seeded.Appointments)
        {
            var start = ClinicTimeZone.ToLocal(appointment.ScheduledStartUtc);
            var service = seeded.Service(appointment.ServiceId);
            var facility = seeded.FacilityOf(service);

            Assert.True(DefaultSchedule.IsWorkingDay(DateOnly.FromDateTime(start)), "a weekday");
            Assert.True(facility.IsOpenDuring(appointment.ScheduledStartUtc, appointment.ScheduledEndUtc), "inside the branch hours");

            if (appointment.StaffMemberId is { } doctorId)
            {
                var doctor = seeded.Db.ChangeTracker.Entries<Doctor>().Select(e => e.Entity).Single(d => d.Id == doctorId);
                Assert.Equal(TimeSpan.FromMinutes(doctor.SlotDurationMinutes), appointment.ScheduledEndUtc - appointment.ScheduledStartUtc);

                // The default schedule: 09:00-12:00 and 13:00-16:00, slots anchored at the start of each window.
                var windows = DefaultSchedule.RangesFor(DateOnly.FromDateTime(start), doctor.SlotDurationMinutes);
                Assert.Contains(windows, w =>
                    w.StartUtc <= appointment.ScheduledStartUtc && appointment.ScheduledEndUtc <= w.EndUtc
                    && (appointment.ScheduledStartUtc - w.StartUtc).TotalMinutes % doctor.SlotDurationMinutes == 0);
                Assert.True(doctor.ServicesProvided.Any(s => s.Id == service.Id), "the doctor offers the service");
            }
            else
            {
                Assert.True(SlotGrid.IsDiagnosticSlotStart(service, facility, appointment.ScheduledStartUtc));
                Assert.Equal(service.Duration, appointment.ScheduledEndUtc - appointment.ScheduledStartUtc);
            }
        }
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void UpcomingAppointments_SitInsideTheFourteenDayWindow_AndPastOnesInThePast(DateTime now)
    {
        var seeded = Seed(now);

        foreach (var appointment in seeded.Appointments)
        {
            var day = seeded.DayOf(appointment);
            if (seeded.IsUpcoming(appointment))
            {
                Assert.True(BookingRules.IsWithinWindow(day, now) && day > seeded.Today, $"{day} is not tomorrow or later inside the window");
            }
            else
            {
                Assert.True(day < seeded.Today, $"{day} is not before today");
            }
        }
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void AhmetCelik_HasAMixedHistoryInSeveralDepartments_AndUpcomingAppointmentsWithAPendingOne(DateTime now)
    {
        var seeded = Seed(now);
        var ahmet = seeded.Patient("ahmet.celik@example.com");
        Assert.True(ahmet.HasInsurance);
        var mine = seeded.Appointments.Where(a => a.PatientId == ahmet.Id).ToList();

        var past = mine.Where(a => !seeded.IsUpcoming(a)).ToList();
        var upcoming = mine.Where(seeded.IsUpcoming).ToList();

        Assert.InRange(past.Count, 5, 6);
        Assert.InRange(upcoming.Count, 2, 3);
        Assert.Contains(past, a => a.Status == AppointmentStatus.Confirmed);
        Assert.Contains(past, a => a.Status == AppointmentStatus.Cancelled);
        Assert.Contains(past, a => a.Status == AppointmentStatus.Rejected);
        Assert.Contains(past, a => a.StaffMemberId is null); // one diagnostic service
        Assert.Contains(upcoming, a => a.Status == AppointmentStatus.Pending);

        var departments = past.Where(a => a.StaffMemberId is not null)
            .Select(a => seeded.Doctor(seeded.Db.ChangeTracker.Entries<Doctor>().Select(e => e.Entity).Single(d => d.Id == a.StaffMemberId).Email).DepartmentId)
            .Distinct().Count();
        Assert.True(departments >= 3, "different departments");
    }

    [Theory]
    [MemberData(nameof(RunTimes))]
    public void TheTwoDemoDoctors_HaveEightToTenAppointments_WithTheStatusesTheirSettingsAllow(DateTime now)
    {
        var seeded = Seed(now);
        var hakan = seeded.Doctor("hakan.yildiz@sakaryasehir.example");
        var kemal = seeded.Doctor("kemal.aslan@sakaryasehir.example");
        Assert.True(hakan.AutoConfirmAppointments);
        Assert.False(kemal.AutoConfirmAppointments);

        var hakans = seeded.Appointments.Where(a => a.StaffMemberId == hakan.Id).ToList();
        var kemals = seeded.Appointments.Where(a => a.StaffMemberId == kemal.Id).ToList();

        Assert.InRange(hakans.Count, 8, 10);
        Assert.InRange(kemals.Count, 8, 10);
        Assert.True(hakans.Select(a => a.PatientId).Distinct().Count() >= 6, "different patients");
        Assert.True(kemals.Select(a => a.PatientId).Distinct().Count() >= 5, "different patients");

        // The doctor who confirms at once has no Pending or Rejected appointment, and has both past and upcoming ones.
        Assert.All(hakans, a => Assert.Contains(a.Status, new[] { AppointmentStatus.Confirmed, AppointmentStatus.Cancelled }));
        Assert.Contains(hakans, seeded.IsUpcoming);
        Assert.Contains(hakans, a => !seeded.IsUpcoming(a));

        // The doctor who approves has 3-4 Pending upcoming appointments to decide, and a history.
        var pending = kemals.Where(a => a.Status == AppointmentStatus.Pending).ToList();
        Assert.InRange(pending.Count, 3, 4);
        Assert.All(pending, a => Assert.True(seeded.IsUpcoming(a)));
        Assert.Contains(kemals, a => !seeded.IsUpcoming(a) && a.Status == AppointmentStatus.Confirmed);
    }

    [Fact]
    public void TheAdditionalPatients_AreTurkishNamed_SomeInsuredAndSomeNot_AndAllHaveTheDemoLogin()
    {
        var seeded = Seed(new DateTime(2026, 11, 18, 9, 30, 0, DateTimeKind.Utc));
        var extra = SeedCatalog.ExtraPatients;

        Assert.InRange(extra.Length, 6, 8);
        Assert.Contains(extra, p => p.Insurance is not null);
        Assert.Contains(extra, p => p.Insurance is null);
        Assert.All(extra, p => Assert.Contains(p.Email, DbInitializer.DemoEmails));
        Assert.All(extra, p => Assert.Equal(p.Insurance is not null, seeded.Patient(p.Email).HasInsurance));

        var emails = seeded.Db.ChangeTracker.Entries<Patient>().Select(e => e.Entity.Email).ToList();
        Assert.Equal(emails.Count, emails.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
