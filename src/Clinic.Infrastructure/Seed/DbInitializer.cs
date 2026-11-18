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

    public const string AdminEmail = "admin@clinic.example";

    /// <summary>The password of every demo account (documented in CLAUDE.md; for local demos only).</summary>
    public const string DemoPassword = "Demo123!";

    /// <summary>
    /// The demo accounts that get <see cref="DemoPassword"/>: the patients, every seeded doctor and the clinic managers.
    /// Technicians have no demo login. Only these emails are ever given the demo password.
    /// </summary>
    public static readonly string[] DemoEmails =
    [
        // System administrator
        AdminEmail,
        // Patients
        "ahmet.celik@example.com",
        "fatma.yildirim@example.com",
        // Clinic managers
        "ayse.yilmaz@sakaryasehir.example",
        "mehmet.kara@sakaryasehir.example",
        "ali.erdogan@marmarapoliklinik.example",
        // Doctors
        .. SeedCatalog.DoctorEmails,
    ];

    public static void MigrateAndSeed(ClinicDbContext dbContext)
    {
        dbContext.Database.Migrate();

        SeedCoreData(dbContext);
        SeedSystemAdmin(dbContext);
        SeedDemoAccounts(dbContext);
        SeedAvailability(dbContext, DateTime.UtcNow);
    }

    /// <summary>Makes sure the demo system administrator exists (runs on every start; creates it only once).</summary>
    private static void SeedSystemAdmin(ClinicDbContext dbContext)
    {
        if (dbContext.Users.Any(u => u.Email == AdminEmail))
        {
            return;
        }

        dbContext.SystemAdmins.Add(new SystemAdmin("Sistem Yöneticisi", AdminEmail, "+90-264-555-0000"));
        dbContext.SaveChanges();
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
        var merkezDepartments = AddDepartments(dbContext, merkez, SeedCatalog.Merkez);
        dbContext.ClinicFacilities.Add(merkez);

        dbContext.ClinicManagers.Add(new ClinicManager("Ayşe Yılmaz", "ayse.yilmaz@sakaryasehir.example", "+90-264-555-0001", merkez.Id));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Kemal Arslan", "kemal.arslan@sakaryasehir.example", "+90-264-555-0021", merkezDepartments["Radyoloji"], "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Gülşen Demirtaş", "gulsen.demirtas@sakaryasehir.example", "+90-264-555-0022", merkezDepartments["Radyoloji"], "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Fatma Güneş", "fatma.gunes@sakaryasehir.example", "+90-264-555-0023", merkezDepartments["Laboratuvar"], "Tıbbi Laboratuvar Teknisyeni"));

        // ---- Adapazarı Şubesi: weekdays 08:30-16:30 ----
        var adapazari = new ClinicFacility(organization.Id, "Adapazarı Şubesi", "Semerciler Mah. Atatürk Bulvarı No: 48, Adapazarı / Sakarya");
        AddWeekdayHours(adapazari, new TimeOnly(8, 30), new TimeOnly(16, 30));
        var adapazariDepartments = AddDepartments(dbContext, adapazari, SeedCatalog.Adapazari);
        dbContext.ClinicFacilities.Add(adapazari);

        dbContext.ClinicManagers.Add(new ClinicManager("Mehmet Kara", "mehmet.kara@sakaryasehir.example", "+90-264-555-0002", adapazari.Id));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Mustafa Kılıç", "mustafa.kilic@sakaryasehir.example", "+90-264-555-0041", adapazariDepartments["Radyoloji"], "Radyoloji Teknikeri"));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Ayten Polat", "ayten.polat@sakaryasehir.example", "+90-264-555-0042", adapazariDepartments["Laboratuvar"], "Tıbbi Laboratuvar Teknisyeni"));
    }

    /// <summary>A second, independent organization with a single branch.</summary>
    private static void SeedMarmaraPoliklinik(ClinicDbContext dbContext)
    {
        var organization = new HealthOrganization("Marmara Poliklinik");
        dbContext.HealthOrganizations.Add(organization);

        var serdivan = new ClinicFacility(organization.Id, "Serdivan Kliniği", "Kemalpaşa Mah. Bahçeli Sok. No: 7, Serdivan / Sakarya");
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Saturday; day++)
        {
            serdivan.AddWorkingHour(day, new TimeOnly(9, 0), new TimeOnly(18, 0));
        }

        var departments = AddDepartments(dbContext, serdivan, SeedCatalog.Serdivan);
        dbContext.ClinicFacilities.Add(serdivan);

        dbContext.ClinicManagers.Add(new ClinicManager("Ali Rıza Erdoğan", "ali.erdogan@marmarapoliklinik.example", "+90-264-555-0003", serdivan.Id));
        dbContext.HealthTechnicians.Add(new HealthTechnician("Emre Korkmaz", "emre.korkmaz@marmarapoliklinik.example", "+90-264-555-0052", departments["Laboratuvar"], "Tıbbi Laboratuvar Teknisyeni"));
    }

    private static void AddWeekdayHours(ClinicFacility clinic, TimeOnly opensAt, TimeOnly closesAt)
    {
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
        {
            clinic.AddWorkingHour(day, opensAt, closesAt);
        }
    }

    /// <summary>
    /// Creates the departments, services and doctors of a branch from the catalog. Each doctor offers the
    /// department's Examination services. Returns the departments by name.
    /// </summary>
    private static Dictionary<string, Department> AddDepartments(ClinicDbContext dbContext, ClinicFacility clinic, SeedDepartment[] catalog)
    {
        var departments = new Dictionary<string, Department>();
        var phoneNumber = 100 + dbContext.ChangeTracker.Entries<Doctor>().Count();

        foreach (var entry in catalog)
        {
            var department = clinic.AddDepartment(entry.Name);
            departments[entry.Name] = department;

            var examinations = new List<Service>();
            foreach (var service in entry.Services)
            {
                var added = department.AddService(service.Name, service.Type, TimeSpan.FromMinutes(service.DurationMinutes), service.Price);
                if (service.Type == ServiceType.Examination)
                {
                    examinations.Add(added);
                }
            }

            foreach (var seedDoctor in entry.Doctors)
            {
                var doctor = new Doctor(seedDoctor.FullName, seedDoctor.Email, $"+90-264-555-{phoneNumber++:D4}", department, seedDoctor.SlotDurationMinutes);
                foreach (var examination in examinations)
                {
                    doctor.AddService(examination);
                }

                dbContext.Doctors.Add(doctor);
            }
        }

        return departments;
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
