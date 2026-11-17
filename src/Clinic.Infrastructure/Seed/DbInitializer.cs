using Clinic.Domain.Common;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Seed;

/// <summary>Applies pending migrations and seeds sample data, run once on application startup.</summary>
public static class DbInitializer
{
    /// <summary>How many days ahead (starting tomorrow) availability is seeded.</summary>
    public const int AvailabilityHorizonDays = 14;

    private const string GeneralDoctorEmail = "mehmet.demir@clinic.example";
    private const string RadiologyDoctorEmail = "elif.kaya@clinic.example";

    public static void MigrateAndSeed(ClinicDbContext dbContext)
    {
        dbContext.Database.Migrate();

        SeedCoreData(dbContext);
        SeedAvailability(dbContext, DateTime.UtcNow);
    }

    /// <summary>Seeds the clinic, services, users once; skipped when data already exists.</summary>
    private static void SeedCoreData(ClinicDbContext dbContext)
    {
        if (dbContext.ClinicFacilities.Any())
        {
            return; // Already seeded.
        }

        var clinic = new Domain.Clinics.ClinicFacility("Sakarya City Hospital", "Sakarya, Turkey");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            clinic.AddWorkingHour(day, new TimeOnly(8, 0), new TimeOnly(17, 0));
        }

        var generalMedicine = clinic.AddDepartment("General Medicine");
        var generalCheckUp = generalMedicine.AddService("General Check-up", TimeSpan.FromMinutes(30), 300m);

        var radiology = clinic.AddDepartment("Radiology");
        var xRay = radiology.AddService("X-Ray", TimeSpan.FromMinutes(20), 450m);

        dbContext.ClinicFacilities.Add(clinic);

        var manager = new ClinicManager("Ayşe Yilmaz", "ayse.yilmaz@clinic.example", "+90-500-000-0001", clinic.Id);

        var doctor = new Doctor("Dr. Mehmet Demir", GeneralDoctorEmail, "+90-500-000-0002", clinic.Id, "General Medicine");
        doctor.AddService(generalCheckUp);

        var radiologyDoctor = new Doctor("Dr. Elif Kaya", RadiologyDoctorEmail, "+90-500-000-0003", clinic.Id, "Radiology");
        radiologyDoctor.AddService(xRay);

        var patient = new Patient("Ahmet Çelik", "ahmet.celik@example.com", "+90-500-000-0004", new DateOnly(1990, 5, 20), insuranceNumber: "INS-12345");

        dbContext.ClinicManagers.Add(manager);
        dbContext.Doctors.AddRange(doctor, radiologyDoctor);
        dbContext.Patients.Add(patient);

        dbContext.SaveChanges();
    }

    /// <summary>
    /// Gives each seeded doctor availability for the next <see cref="AvailabilityHorizonDays"/> days,
    /// but only if that doctor has no future availability at all. Runs on every startup, so it must be
    /// idempotent, and it must not resurrect ranges a manager deleted: once a doctor has any future
    /// range, the seed leaves their schedule alone.
    /// </summary>
    public static void SeedAvailability(ClinicDbContext dbContext, DateTime utcNow)
    {
        var firstDay = DateOnly.FromDateTime(ClinicTimeZone.ToLocal(utcNow)).AddDays(1);

        var plans = new (string Email, (TimeOnly From, TimeOnly To)[] Windows)[]
        {
            (GeneralDoctorEmail, [(new TimeOnly(9, 0), new TimeOnly(12, 0)), (new TimeOnly(13, 0), new TimeOnly(16, 0))]),
            (RadiologyDoctorEmail, [(new TimeOnly(10, 0), new TimeOnly(15, 0))]),
        };

        foreach (var (email, windows) in plans)
        {
            var doctor = dbContext.Doctors
                .Include(d => d.AvailabilitySlots)
                .FirstOrDefault(d => d.Email == email);

            if (doctor is null)
            {
                continue;
            }

            // A range that has not ended yet counts as future availability.
            if (doctor.AvailabilitySlots.Any(s => s.EndUtc > utcNow))
            {
                continue;
            }

            foreach (var (from, to) in windows)
            {
                foreach (var (startUtc, endUtc) in BuildWeekdayWindows(firstDay, AvailabilityHorizonDays, from, to))
                {
                    var slot = doctor.AddAvailability(startUtc, endUtc);

                    // Slots have client-generated Guid ids, so tell EF explicitly that they are new.
                    dbContext.Set<DoctorAvailability>().Add(slot);
                }
            }
        }

        dbContext.SaveChanges();
    }

    /// <summary>
    /// Builds UTC ranges for the clinic-local time window [from, to) on each weekday
    /// within <paramref name="days"/> days starting at <paramref name="firstLocalDay"/>.
    /// </summary>
    public static IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> BuildWeekdayWindows(
        DateOnly firstLocalDay, int days, TimeOnly from, TimeOnly to)
    {
        var result = new List<(DateTime, DateTime)>();

        for (var offset = 0; offset < days; offset++)
        {
            var day = firstLocalDay.AddDays(offset);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            // Window times are Turkey wall-clock times; storage stays in UTC.
            result.Add((
                ClinicTimeZone.ToUtc(day.ToDateTime(from)),
                ClinicTimeZone.ToUtc(day.ToDateTime(to))));
        }

        return result;
    }
}
