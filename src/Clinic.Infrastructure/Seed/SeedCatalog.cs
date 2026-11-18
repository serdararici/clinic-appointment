using Clinic.Domain.Enums;

namespace Clinic.Infrastructure.Seed;

/// <summary>A doctor of the sample data. The specialty is not stored: it is the name of the department.</summary>
public sealed record SeedDoctor(string FullName, string Email, int SlotDurationMinutes);

/// <summary>A service of the sample data (the type decides whether the patient picks a doctor).</summary>
public sealed record SeedService(string Name, ServiceType Type, int DurationMinutes, decimal Price);

/// <summary>
/// A department with its services and doctors. Every doctor offers the department's Examination service(s);
/// Diagnostic services are booked without a doctor. A department with one service demonstrates the auto-select rule.
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

    private static SeedDoctor Doctor(string name, string email, int slotMinutes) => new(name, email, slotMinutes);

    private const string Sakarya = "@sakaryasehir.example";
    private const string Marmara = "@marmarapoliklinik.example";

    /// <summary>Sakarya Şehir Hastanesi - Merkez.</summary>
    public static readonly SeedDepartment[] Merkez =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 450m)],
        [
            Doctor("Dr. Hakan Yıldız", "hakan.yildiz" + Sakarya, 30),
            Doctor("Dr. Elif Şahin", "elif.sahin" + Sakarya, 20),
        ]),
        new("Gastroenteroloji", [Exam("Gastroenteroloji Muayenesi", 20, 550m)],
        [
            Doctor("Dr. Selin Aydın", "selin.aydin" + Sakarya, 20),
            Doctor("Dr. Murat Tekin", "murat.tekin" + Sakarya, 30),
        ]),
        new("Kardiyoloji", [Exam("Kardiyoloji Muayenesi", 30, 600m), Diagnostic("EKG", 15, 300m), Diagnostic("Ekokardiyografi", 30, 900m)],
        [
            Doctor("Dr. Kemal Aslan", "kemal.aslan" + Sakarya, 30),
            Doctor("Dr. Neslihan Koç", "neslihan.koc" + Sakarya, 15),
        ]),
        // One service only: the booking flow selects it automatically.
        new("Ortopedi ve Travmatoloji", [Exam("Ortopedi Muayenesi", 20, 500m)],
        [
            Doctor("Dr. Oğuz Karaca", "oguz.karaca" + Sakarya, 20),
            Doctor("Dr. Pınar Eren", "pinar.eren" + Sakarya, 30),
        ]),
        new("Göz Hastalıkları", [Exam("Göz Muayenesi", 20, 450m)],
        [
            Doctor("Dr. Deniz Kurt", "deniz.kurt" + Sakarya, 15),
            Doctor("Dr. Gamze Özkan", "gamze.ozkan" + Sakarya, 20),
        ]),
        new("Çocuk Sağlığı ve Hastalıkları", [Exam("Çocuk Muayenesi", 20, 400m)],
        [
            Doctor("Dr. Tuğba Yalçın", "tugba.yalcin" + Sakarya, 20),
            Doctor("Dr. Serkan Bulut", "serkan.bulut" + Sakarya, 15),
        ]),
        new("Nöroloji", [Exam("Nöroloji Muayenesi", 30, 550m), Diagnostic("EMG", 30, 800m)],
        [
            Doctor("Dr. Cem Uysal", "cem.uysal" + Sakarya, 30),
            Doctor("Dr. Aylin Güler", "aylin.guler" + Sakarya, 20),
        ]),
        new("Radyoloji", [Diagnostic("Röntgen", 20, 350m), Diagnostic("MR", 45, 1800m), Diagnostic("Ultrasonografi", 20, 600m)], []),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 250m)], []),
    ];

    /// <summary>Sakarya Şehir Hastanesi - Adapazarı Şubesi.</summary>
    public static readonly SeedDepartment[] Adapazari =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 400m)],
        [
            Doctor("Dr. Zeynep Çelik", "zeynep.celik" + Sakarya, 30),
            Doctor("Dr. Hasan Polat", "hasan.polat" + Sakarya, 20),
        ]),
        new("Gastroenteroloji", [Exam("Gastroenteroloji Muayenesi", 20, 500m)],
        [
            Doctor("Dr. Burak Öztürk", "burak.ozturk" + Sakarya, 20),
            Doctor("Dr. Ece Yavuz", "ece.yavuz" + Sakarya, 30),
        ]),
        new("Kardiyoloji", [Exam("Kardiyoloji Muayenesi", 30, 550m), Diagnostic("EKG", 15, 300m)],
        [
            Doctor("Dr. Volkan Tan", "volkan.tan" + Sakarya, 30),
            Doctor("Dr. Sibel Acar", "sibel.acar" + Sakarya, 15),
        ]),
        new("Çocuk Sağlığı ve Hastalıkları", [Exam("Çocuk Muayenesi", 20, 380m)],
        [
            Doctor("Dr. Merve Sarı", "merve.sari" + Sakarya, 20),
            Doctor("Dr. Burcu Çakır", "burcu.cakir" + Sakarya, 15),
        ]),
        new("Radyoloji", [Diagnostic("Röntgen", 20, 350m), Diagnostic("MR", 45, 1800m)], []),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 250m)], []),
    ];

    /// <summary>Marmara Poliklinik - Serdivan Kliniği.</summary>
    public static readonly SeedDepartment[] Serdivan =
    [
        new("İç Hastalıkları (Dahiliye)", [Exam("Genel Muayene", 30, 350m)],
        [
            Doctor("Dr. Can Aksoy", "can.aksoy" + Marmara, 15),
            Doctor("Dr. Leyla Tunç", "leyla.tunc" + Marmara, 30),
        ]),
        new("Göz Hastalıkları", [Exam("Göz Muayenesi", 20, 400m)],
        [
            Doctor("Dr. Barış Çetin", "baris.cetin" + Marmara, 20),
            Doctor("Dr. Yasemin Doğan", "yasemin.dogan" + Marmara, 15),
        ]),
        new("Ortopedi ve Travmatoloji", [Exam("Ortopedi Muayenesi", 20, 450m)],
        [
            Doctor("Dr. Onur Işık", "onur.isik" + Marmara, 30),
            Doctor("Dr. Selma Kaya", "selma.kaya" + Marmara, 20),
        ]),
        new("Laboratuvar", [Diagnostic("Kan Tahlili", 15, 200m)], []),
    ];

    /// <summary>The emails of every seeded doctor.</summary>
    public static IEnumerable<string> DoctorEmails =>
        Merkez.Concat(Adapazari).Concat(Serdivan).SelectMany(department => department.Doctors).Select(doctor => doctor.Email);
}
