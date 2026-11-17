using Clinic.Domain.Clinics;
using Clinic.Domain.Common;
using Clinic.Domain.Enums;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Seed;

/// <summary>Applies pending migrations and seeds sample data, run once on application startup.</summary>
public static class DbInitializer
{
    /// <summary>How many days ahead (starting tomorrow) availability is seeded.</summary>
    public const int AvailabilityHorizonDays = 14;

    // Every seeded doctor works the same two local-time blocks on weekdays; all of them fall
    // inside the seeded branches' working hours.
    private static readonly (TimeOnly From, TimeOnly To)[] DoctorWindows =
    [
        (new TimeOnly(9, 0), new TimeOnly(12, 0)),
        (new TimeOnly(13, 0), new TimeOnly(16, 0)),
    ];

    /// <summary>The password of every demo account (documented in CLAUDE.md; for local demos only).</summary>
    public const string DemoPassword = "Demo123!";

    /// <summary>
    /// The demo accounts that get <see cref="DemoPassword"/>: the patients, every doctor and the clinic managers.
    /// Technicians have no demo login. Only these emails are ever given the demo password.
    /// </summary>
    public static readonly string[] DemoEmails =
    [
        // Patients
        "ahmet.celik@example.com",
        "fatma.yildirim@example.com",
        // Doctors
        "hakan.yildiz@sakaryasehir.example",
        "elif.sahin@sakaryasehir.example",
        "zeynep.celik@sakaryasehir.example",
        "burak.ozturk@sakaryasehir.example",
        "can.aksoy@marmarapoliklinik.example",
        // Clinic managers
        "ayse.yilmaz@sakaryasehir.example",
        "mehmet.kara@sakaryasehir.example",
        "ali.erdogan@marmarapoliklinik.example",
    ];

    public static void MigrateAndSeed(ClinicDbContext dbContext)
    {
        dbContext.Database.Migrate();

        SeedCoreData(dbContext);
        SeedDemoAccounts(dbContext);
        SeedAvailability(dbContext, DateTime.UtcNow);
    }

    /// <summary>
    /// Gives the demo users a login. Runs on every start and only fills in demo users that have no password
    /// hash yet, so it also upgrades a database that was seeded before logins existed, never overwrites a
    /// changed password, and never touches users outside <see cref="DemoEmails"/>.
    /// </summary>
    private static void SeedDemoAccounts(ClinicDbContext dbContext)
    {
        var accounts = dbContext.Users
            .Where(u => DemoEmails.Contains(u.Email) && u.PasswordHash == null)
            .ToList();

        if (accounts.Count == 0)
        {
            return;
        }

        var hasher = new PasswordHasher<object>();
        foreach (var account in accounts)
        {
            // One hash per account: each gets its own random salt.
            account.SetPasswordHash(hasher.HashPassword(new object(), DemoPassword));
        }

        dbContext.SaveChanges();
    }

    /// <summary>Seeds organizations, branches, services, staff and patients once; skipped when data already exists.</summary>
    private static void SeedCoreData(ClinicDbContext dbContext)
    {
        if (dbContext.HealthOrganizations.Any())
        {
            return; // Already seeded.
        }

        SeedSakaryaCityHospital(dbContext);
        SeedMarmaraPoliklinik(dbContext);

        dbContext.Patients.AddRange(
            new Patient("Ahmet Çelik", "ahmet.celik@example.com", "+90-532-000-0001", new DateOnly(1990, 5, 20), insuranceNumber: "SGK-12345"),
            new Patient("Fatma Yıldırım", "fatma.yildirim@example.com", "+90-532-000-0002", new DateOnly(1985, 11, 3)));

        dbContext.SaveChanges();
    }

    /// <summary>A multi-branch organization: Merkez and Adapazarı Şubesi, each with its own hours and departments.</summary>
    private static void SeedSakaryaCityHospital(ClinicDbContext dbContext)
    {
        var organization = new HealthOrganization("Sakarya Şehir Hastanesi");
        dbContext.HealthOrganizations.Add(organization);

        // ---- Merkez: weekdays 08:00-17:00 plus a shorter Saturday ----
        var merkez = new ClinicFacility(organization.Id, "Merkez", "Korucuk Mah. Sağlık Cad. No: 1, Adapazarı / Sakarya");
        AddWeekdayHours(merkez, new TimeOnly(8, 0), new TimeOnly(17, 0));
        merkez.AddWorkingHour(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        var merkezDepartments = AddStandardDepartments(merkez, examinationPrice: 450m);
        dbContext.ClinicFacilities.Add(merkez);

        dbContext.ClinicManagers.Add(new ClinicManager("Ayşe Yılmaz", "ayse.yilmaz@sakaryasehir.example", "+90-264-555-0001", merkez.Id));
        dbContext.Doctors.Add(NewDoctor("Dr. Hakan Yıldız", "hakan.yildiz@sakaryasehir.example", "+90-264-555-0011", merkezDepartments.Dahiliye, merkezDepartments.Examination, "İç Hastalıkları", slotDurationMinutes: 30));
        dbContext.Doctors.Add(NewDoctor("Dr. Elif Şahin", "elif.sahin@sakaryasehir.example", "+90-264-555-0012", merkezDepartments.Dahiliye, merkezDepartments.Examination, "Endokrinoloji", slotDurationMinutes: 20));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Kemal Arslan", "kemal.arslan@sakaryasehir.example", "+90-264-555-0021", merkezDepartments.Radyoloji, "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Gülşen Demirtaş", "gulsen.demirtas@sakaryasehir.example", "+90-264-555-0022", merkezDepartments.Radyoloji, "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Fatma Güneş", "fatma.gunes@sakaryasehir.example", "+90-264-555-0023", merkezDepartments.Laboratuvar, "Tıbbi Laboratuvar Teknisyeni"));

        // ---- Adapazarı Şubesi: weekdays 08:30-16:30 ----
        var adapazari = new ClinicFacility(organization.Id, "Adapazarı Şubesi", "Semerciler Mah. Atatürk Bulvarı No: 48, Adapazarı / Sakarya");
        AddWeekdayHours(adapazari, new TimeOnly(8, 30), new TimeOnly(16, 30));
        var adapazariDepartments = AddStandardDepartments(adapazari, examinationPrice: 400m);
        dbContext.ClinicFacilities.Add(adapazari);

        dbContext.ClinicManagers.Add(new ClinicManager("Mehmet Kara", "mehmet.kara@sakaryasehir.example", "+90-264-555-0002", adapazari.Id));
        dbContext.Doctors.Add(NewDoctor("Dr. Zeynep Çelik", "zeynep.celik@sakaryasehir.example", "+90-264-555-0031", adapazariDepartments.Dahiliye, adapazariDepartments.Examination, "İç Hastalıkları", slotDurationMinutes: 30));
        dbContext.Doctors.Add(NewDoctor("Dr. Burak Öztürk", "burak.ozturk@sakaryasehir.example", "+90-264-555-0032", adapazariDepartments.Dahiliye, adapazariDepartments.Examination, "Gastroenteroloji", slotDurationMinutes: 20));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Mustafa Kılıç", "mustafa.kilic@sakaryasehir.example", "+90-264-555-0041", adapazariDepartments.Radyoloji, "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Ayten Polat", "ayten.polat@sakaryasehir.example", "+90-264-555-0042", adapazariDepartments.Laboratuvar, "Tıbbi Laboratuvar Teknisyeni"));
    }

    /// <summary>A second, independent organization with a single branch and only two departments.</summary>
    private static void SeedMarmaraPoliklinik(ClinicDbContext dbContext)
    {
        var organization = new HealthOrganization("Marmara Poliklinik");
        dbContext.HealthOrganizations.Add(organization);

        var serdivan = new ClinicFacility(organization.Id, "Serdivan Kliniği", "Kemalpaşa Mah. Bahçeli Sok. No: 7, Serdivan / Sakarya");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Saturday; day++)
        {
            serdivan.AddWorkingHour(day, new TimeOnly(9, 0), new TimeOnly(18, 0));
        }

        var dahiliye = serdivan.AddDepartment("Dahiliye");
        var genelMuayene = dahiliye.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), 350m);
        var laboratuvar = serdivan.AddDepartment("Laboratuvar");
        laboratuvar.AddService("Kan Tahlili", ServiceType.Diagnostic, TimeSpan.FromMinutes(15), 200m);
        dbContext.ClinicFacilities.Add(serdivan);

        dbContext.ClinicManagers.Add(new ClinicManager("Ali Rıza Erdoğan", "ali.erdogan@marmarapoliklinik.example", "+90-264-555-0003", serdivan.Id));
        dbContext.Doctors.Add(NewDoctor("Dr. Can Aksoy", "can.aksoy@marmarapoliklinik.example", "+90-264-555-0051", dahiliye, genelMuayene, "İç Hastalıkları", slotDurationMinutes: 15));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Emre Korkmaz", "emre.korkmaz@marmarapoliklinik.example", "+90-264-555-0052", laboratuvar, "Tıbbi Laboratuvar Teknisyeni"));
    }

    private static void AddWeekdayHours(ClinicFacility clinic, TimeOnly opensAt, TimeOnly closesAt)
    {
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            clinic.AddWorkingHour(day, opensAt, closesAt);
        }
    }

    /// <summary>
    /// Dahiliye (one Examination service), Radyoloji (two Diagnostic services) and Laboratuvar
    /// (one Diagnostic service). A department with a single service has it auto-selected when booking;
    /// one with several lets the patient choose.
    /// </summary>
    private static (Department Dahiliye, Service Examination, Department Radyoloji, Department Laboratuvar) AddStandardDepartments(
        ClinicFacility clinic, decimal examinationPrice)
    {
        var dahiliye = clinic.AddDepartment("Dahiliye");
        var genelMuayene = dahiliye.AddService("Genel Muayene", ServiceType.Examination, TimeSpan.FromMinutes(30), examinationPrice);

        var radyoloji = clinic.AddDepartment("Radyoloji");
        radyoloji.AddService("Röntgen", ServiceType.Diagnostic, TimeSpan.FromMinutes(20), 350m);
        radyoloji.AddService("MR", ServiceType.Diagnostic, TimeSpan.FromMinutes(45), 1800m);

        var laboratuvar = clinic.AddDepartment("Laboratuvar");
        laboratuvar.AddService("Kan Tahlili", ServiceType.Diagnostic, TimeSpan.FromMinutes(15), 250m);

        return (dahiliye, genelMuayene, radyoloji, laboratuvar);
    }

    private static Doctor NewDoctor(
        string fullName, string email, string phone, Department department, Service service, string specialty, int slotDurationMinutes)
    {
        var doctor = new Doctor(fullName, email, phone, department, specialty, slotDurationMinutes);
        doctor.AddService(service);
        return doctor;
    }

    /// <summary>
    /// Gives each doctor availability for the next <see cref="AvailabilityHorizonDays"/> days,
    /// but only if that doctor has no future availability at all. Runs on every startup, so it must be
    /// idempotent, and it must not resurrect ranges a manager deleted: once a doctor has any future
    /// range, the seed leaves their schedule alone.
    /// </summary>
    public static void SeedAvailability(ClinicDbContext dbContext, DateTime utcNow)
    {
        var firstDay = DateOnly.FromDateTime(ClinicTimeZone.ToLocal(utcNow)).AddDays(1);

        var doctors = dbContext.Doctors
            .Include(d => d.AvailabilitySlots)
            .ToList();

        foreach (var doctor in doctors)
        {
            // A range that has not ended yet counts as future availability.
            if (doctor.AvailabilitySlots.Any(s => s.EndUtc > utcNow))
            {
                continue;
            }

            foreach (var (from, to) in DoctorWindows)
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
