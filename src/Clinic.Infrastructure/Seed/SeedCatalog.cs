using Clinic.Domain.Enums;

namespace Clinic.Infrastructure.Seed;

/// <summary>A doctor of the sample data. The specialty is not stored: it is the name of the department.</summary>
public sealed record SeedDoctor(string FullName, string Email, int SlotDurationMinutes, bool AutoConfirm = true, string[]? OnlyServices = null);

/// <summary>A service of the sample data (the type decides whether the patient picks a doctor).</summary>
public sealed record SeedService(string Name, ServiceType Type, int DurationMinutes, decimal Price);

/// <summary>An additional demo patient (password: the demo password). <paramref name="Insurance"/> is null for an uninsured one.</summary>
public sealed record SeedPatient(string FullName, string Email, string Phone, DateOnly DateOfBirth, string? Insurance);

/// <summary>A health technician of the sample data, linked to some Diagnostic services of their department (a definition only).</summary>
public sealed record SeedTechnician(string FullName, string Email, string Phone, string Title, string Department, string[] Services);

/// <summary>
/// A department with its services and doctors. A doctor offers all of the department's Examination services unless
/// <see cref="SeedDoctor.OnlyServices"/> narrows it; Diagnostic services are booked without a doctor. A department with one service demonstrates the auto-select rule.
/// </summary>
public sealed record SeedDepartment(string Name, SeedService[] Services, SeedDoctor[] Doctors);

/// <summary>
/// The Turkish sample data of the three branches. It lives here as plain data so that the demo password list
/// (<see cref="DbInitializer.DemoEmails"/>) is built from exactly the doctors that get seeded.
/// </summary>
public static class SeedCatalog
{
    private static SeedService Exam(string name, int minutes, decimal price) => new(name, ServiceType.Examination, minutes, price);

    private static SeedService Diagnostic(string name, int minutes, decimal price) => new(name, ServiceType.Diagnostic, minutes, price);

    private static SeedDoctor Doctor(string name, string email, int slotMinutes, bool autoConfirm = true, string[]? onlyServices = null) =>
        new(name, email, slotMinutes, autoConfirm, onlyServices);

    private const string Sakarya = "@sakaryasehir.example";
    private const string Marmara = "@marmarapoliklinik.example";

    /// <summary>Sakarya Şehir Hastanesi - Merkez.</summary>
    public static readonly SeedDepartment[] Merkez =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 450m), Exam("Kontrol Muayenesi", 30, 270m)],
        [
            Doctor("Dr. Hakan Yıldız", "hakan.yildiz" + Sakarya, 30),
            Doctor("Dr. Elif Şahin", "elif.sahin" + Sakarya, 20, onlyServices: ["Genel Muayene"]),
            // A third doctor with a 20-minute slot who offers both services.
            Doctor("Dr. Ayhan Demirci", "ayhan.demirci" + Sakarya, 20),
        ]),
        new("Gastroenteroloji", [Exam("Genel Muayene", 20, 550m)],
        [
            Doctor("Dr. Selin Aydın", "selin.aydin" + Sakarya, 20),
            Doctor("Dr. Murat Tekin", "murat.tekin" + Sakarya, 30),
        ]),
        new("Kardiyoloji", [Exam("Genel Muayene", 30, 600m), Exam("Kontrol Muayenesi", 30, 360m), Diagnostic("EKG", 15, 300m), Diagnostic("Ekokardiyografi", 30, 900m)],
        [
            // The one demo doctor who approves appointments himself: new bookings with him wait as Pending.
            Doctor("Dr. Kemal Aslan", "kemal.aslan" + Sakarya, 30, autoConfirm: false),
            Doctor("Dr. Neslihan Koç", "neslihan.koc" + Sakarya, 15, onlyServices: ["Genel Muayene"]),
        ]),
        // One service only: the booking flow selects it automatically.
        new("Ortopedi ve Travmatoloji", [Exam("Genel Muayene", 20, 500m)],
        [
            Doctor("Dr. Oğuz Karaca", "oguz.karaca" + Sakarya, 20),
            Doctor("Dr. Pınar Eren", "pinar.eren" + Sakarya, 30),
        ]),
        new("Göz Hastalıkları", [Exam("Genel Muayene", 20, 450m), Exam("Kontrol Muayenesi", 20, 270m)],
        [
            Doctor("Dr. Deniz Kurt", "deniz.kurt" + Sakarya, 15),
            Doctor("Dr. Gamze Özkan", "gamze.ozkan" + Sakarya, 20, onlyServices: ["Genel Muayene"]),
        ]),
        new("Çocuk Sağlığı ve Hastalıkları", [Exam("Genel Muayene", 20, 400m), Exam("Kontrol Muayenesi", 20, 240m)],
        [
            Doctor("Dr. Tuğba Yalçın", "tugba.yalcin" + Sakarya, 20),
            Doctor("Dr. Serkan Bulut", "serkan.bulut" + Sakarya, 15, onlyServices: ["Genel Muayene"]),
            // A third doctor with a 15-minute slot who offers only Genel Muayene.
            Doctor("Dr. Nilüfer Kaplan", "nilufer.kaplan" + Sakarya, 15, onlyServices: ["Genel Muayene"]),
        ]),
        new("Nöroloji", [Exam("Genel Muayene", 30, 550m), Diagnostic("EMG", 30, 800m)],
        [
            Doctor("Dr. Cem Uysal", "cem.uysal" + Sakarya, 30),
            Doctor("Dr. Aylin Güler", "aylin.guler" + Sakarya, 20),
        ]),
        new("Radyoloji", [Diagnostic("Röntgen", 20, 350m), Diagnostic("MR", 45, 1800m), Diagnostic("Ultrasonografi", 20, 600m)], []),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 250m), Diagnostic("İdrar Tahlili", 15, 150m), Diagnostic("Tam Kan Sayımı", 15, 180m)], []),
    ];

    /// <summary>Sakarya Şehir Hastanesi - Adapazarı Şubesi.</summary>
    public static readonly SeedDepartment[] Adapazari =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 400m), Exam("Kontrol Muayenesi", 30, 240m)],
        [
            Doctor("Dr. Zeynep Çelik", "zeynep.celik" + Sakarya, 30),
            Doctor("Dr. Hasan Polat", "hasan.polat" + Sakarya, 20, onlyServices: ["Genel Muayene"]),
        ]),
        new("Gastroenteroloji", [Exam("Genel Muayene", 20, 500m)],
        [
            Doctor("Dr. Burak Öztürk", "burak.ozturk" + Sakarya, 20),
            Doctor("Dr. Ece Yavuz", "ece.yavuz" + Sakarya, 30),
        ]),
        new("Kardiyoloji", [Exam("Genel Muayene", 30, 550m), Exam("Kontrol Muayenesi", 30, 330m), Diagnostic("EKG", 15, 300m)],
        [
            Doctor("Dr. Volkan Tan", "volkan.tan" + Sakarya, 30),
            Doctor("Dr. Sibel Acar", "sibel.acar" + Sakarya, 15, onlyServices: ["Genel Muayene"]),
        ]),
        new("Çocuk Sağlığı ve Hastalıkları", [Exam("Genel Muayene", 20, 380m), Exam("Kontrol Muayenesi", 20, 230m)],
        [
            Doctor("Dr. Merve Sarı", "merve.sari" + Sakarya, 20),
            Doctor("Dr. Burcu Çakır", "burcu.cakir" + Sakarya, 15, onlyServices: ["Genel Muayene"]),
        ]),
        new("Radyoloji", [Diagnostic("Röntgen", 20, 350m), Diagnostic("MR", 45, 1800m)], []),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 250m), Diagnostic("İdrar Tahlili", 15, 140m), Diagnostic("Tam Kan Sayımı", 15, 170m)], []),
    ];

    /// <summary>Marmara Poliklinik - Serdivan Kliniği.</summary>
    public static readonly SeedDepartment[] Serdivan =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 350m), Exam("Kontrol Muayenesi", 30, 210m)],
        [
            Doctor("Dr. Can Aksoy", "can.aksoy" + Marmara, 15),
            Doctor("Dr. Leyla Tunç", "leyla.tunc" + Marmara, 30, onlyServices: ["Genel Muayene"]),
        ]),
        new("Göz Hastalıkları", [Exam("Genel Muayene", 20, 400m), Exam("Kontrol Muayenesi", 20, 240m)],
        [
            Doctor("Dr. Barış Çetin", "baris.cetin" + Marmara, 20),
            Doctor("Dr. Yasemin Doğan", "yasemin.dogan" + Marmara, 15, onlyServices: ["Genel Muayene"]),
        ]),
        new("Ortopedi ve Travmatoloji", [Exam("Genel Muayene", 20, 450m)],
        [
            Doctor("Dr. Onur Işık", "onur.isik" + Marmara, 30),
            Doctor("Dr. Selma Kaya", "selma.kaya" + Marmara, 20),
        ]),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 200m), Diagnostic("İdrar Tahlili", 15, 120m), Diagnostic("Tam Kan Sayımı", 15, 150m)], []),
    ];

    private const string RadiologyTitle = "Radyoloji Teknikeri";
    private const string LaboratoryTitle = "Tıbbi Laboratuvar Teknisyeni";

    private static SeedTechnician Technician(string name, string email, string phone, string title, string department, params string[] services) =>
        new(name, email, phone, title, department, services);

    /// <summary>The technicians of Merkez: in Radyoloji one does Röntgen and Ultrasonografi, the other MR.</summary>
    public static readonly SeedTechnician[] MerkezTechnicians =
    [
        Technician("Kemal Arslan", "kemal.arslan" + Sakarya, "+90-264-555-0021", RadiologyTitle, "Radyoloji", "Röntgen", "Ultrasonografi"),
        Technician("Gülşen Demirtaş", "gulsen.demirtas" + Sakarya, "+90-264-555-0022", RadiologyTitle, "Radyoloji", "MR"),
        Technician("Fatma Güneş", "fatma.gunes" + Sakarya, "+90-264-555-0023", LaboratoryTitle, "Laboratuvar", "Kan Tahlili", "Tam Kan Sayımı"),
    ];

    public static readonly SeedTechnician[] AdapazariTechnicians =
    [
        Technician("Mustafa Kılıç", "mustafa.kilic" + Sakarya, "+90-264-555-0041", RadiologyTitle, "Radyoloji", "Röntgen", "MR"),
        Technician("Ayten Polat", "ayten.polat" + Sakarya, "+90-264-555-0042", LaboratoryTitle, "Laboratuvar", "Kan Tahlili", "İdrar Tahlili"),
    ];

    public static readonly SeedTechnician[] SerdivanTechnicians =
    [
        Technician("Emre Korkmaz", "emre.korkmaz" + Marmara, "+90-264-555-0052", LaboratoryTitle, "Laboratuvar", "Kan Tahlili", "İdrar Tahlili", "Tam Kan Sayımı"),
    ];

    /// <summary>Eight more Turkish-named patients: four insured and four not, so the price difference can be shown.</summary>
    public static readonly SeedPatient[] ExtraPatients =
    [
        new("Mehmet Yılmaz", "mehmet.yilmaz@example.com", "+90-532-000-0003", new DateOnly(1978, 3, 14), "SGK-20031"),
        new("Elif Demir", "elif.demir@example.com", "+90-532-000-0004", new DateOnly(1992, 8, 2), null),
        new("Mustafa Kaya", "mustafa.kaya@example.com", "+90-532-000-0005", new DateOnly(1965, 12, 27), "SGK-20033"),
        new("Zeynep Arslan", "zeynep.arslan@example.com", "+90-532-000-0006", new DateOnly(2001, 5, 9), null),
        new("Hasan Çetin", "hasan.cetin@example.com", "+90-532-000-0007", new DateOnly(1983, 1, 30), "SGK-20035"),
        new("Ayşe Korkmaz", "ayse.korkmaz@example.com", "+90-532-000-0008", new DateOnly(1995, 10, 21), null),
        new("Emre Şahin", "emre.sahin@example.com", "+90-532-000-0009", new DateOnly(1988, 6, 5), "SGK-20037"),
        new("Selin Aydoğan", "selin.aydogan@example.com", "+90-532-000-0010", new DateOnly(1999, 2, 17), null),
    ];

    /// <summary>The emails of every seeded doctor.</summary>
    public static IEnumerable<string> DoctorEmails =>
        Merkez.Concat(Adapazari).Concat(Serdivan).SelectMany(department => department.Doctors).Select(doctor => doctor.Email);
}
