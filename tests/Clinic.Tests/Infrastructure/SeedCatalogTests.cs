using Clinic.Domain.Enums;
using Clinic.Infrastructure.Seed;

namespace Clinic.Tests.Infrastructure;

/// <summary>The sample data is plain data, so its consistency rules can be checked without a database.</summary>
public class SeedCatalogTests
{
    private static readonly SeedDepartment[][] Branches = [SeedCatalog.Merkez, SeedCatalog.Adapazari, SeedCatalog.Serdivan];

    [Fact]
    public void EveryExaminationDepartment_HasAtLeastTwoDoctorsWithDifferentSlotDurations()
    {
        foreach (var department in Branches.SelectMany(b => b).Where(d => d.Services.Any(s => s.Type == ServiceType.Examination)))
        {
            Assert.True(department.Doctors.Length >= 2, $"{department.Name} needs at least two doctors");
            Assert.True(department.Doctors.Select(d => d.SlotDurationMinutes).Distinct().Count() >= 2, $"{department.Name} needs different slot durations");
        }
    }

    [Fact]
    public void ExaminationServices_AreGenelMuayeneOrKontrolMuayenesi_WhileDiagnosticServicesKeepTheirOwnNames()
    {
        var services = Branches.SelectMany(b => b).SelectMany(d => d.Services).ToList();

        Assert.All(services.Where(s => s.Type == ServiceType.Examination), s => Assert.Contains(s.Name, new[] { "Genel Muayene", "Kontrol Muayenesi" }));
        Assert.All(services.Where(s => s.Type == ServiceType.Diagnostic), s => Assert.DoesNotContain(s.Name, new[] { "Genel Muayene", "Kontrol Muayenesi" }));
        Assert.Contains(services, s => s.Name == "EKG");
        // Every department with a Kontrol Muayenesi also has the Genel Muayene.
        foreach (var department in Branches.SelectMany(b => b).Where(d => d.Services.Any(s => s.Name == "Kontrol Muayenesi")))
        {
            Assert.Contains(department.Services, s => s.Name == "Genel Muayene");
        }
    }

    [Theory]
    [InlineData("Merkez", new[] { "İç Hastalıkları (Dahiliye)", "Kardiyoloji", "Çocuk Sağlığı ve Hastalıkları", "Göz Hastalıkları" })]
    [InlineData("Adapazari", new[] { "İç Hastalıkları (Dahiliye)", "Kardiyoloji", "Çocuk Sağlığı ve Hastalıkları" })]
    [InlineData("Serdivan", new[] { "İç Hastalıkları (Dahiliye)", "Göz Hastalıkları" })]
    public void TheMainClinics_HaveAKontrolMuayenesi_At60PercentOfTheGenelMuayenePrice_WhichOnlyOneDoctorOffers(string branch, string[] departments)
    {
        var catalog = branch switch { "Merkez" => SeedCatalog.Merkez, "Adapazari" => SeedCatalog.Adapazari, _ => SeedCatalog.Serdivan };

        foreach (var name in departments)
        {
            var department = catalog.Single(d => d.Name == name);
            var general = department.Services.Single(s => s.Name == "Genel Muayene");
            var control = department.Services.Single(s => s.Name == "Kontrol Muayenesi");

            Assert.InRange(control.Price / general.Price, 0.55m, 0.65m);
            // Two doctors everywhere; Merkez's Dahiliye and Çocuk Sağlığı have a third one.
            Assert.Equal(branch == "Merkez" && name is "İç Hastalıkları (Dahiliye)" or "Çocuk Sağlığı ve Hastalıkları" ? 3 : 2, department.Doctors.Length);
            Assert.Null(department.Doctors[0].OnlyServices);                                     // the first doctor offers both
            Assert.Equal(["Genel Muayene"], department.Doctors[1].OnlyServices ?? []);                  // the second only Genel Muayene
        }
    }

    [Fact]
    public void Orthopedics_KeepsASingleService_ForTheAutoSelectDemo()
    {
        foreach (var department in Branches.SelectMany(b => b).Where(d => d.Name == "Ortopedi ve Travmatoloji"))
        {
            Assert.Single(department.Services);
        }
    }

    [Fact]
    public void Technicians_AreLinkedToDiagnosticServicesOfTheirOwnDepartment_WithVariedSets()
    {
        var technicians = SeedCatalog.MerkezTechnicians.Concat(SeedCatalog.AdapazariTechnicians).Concat(SeedCatalog.SerdivanTechnicians).ToList();
        var catalogs = new Dictionary<SeedTechnician, SeedDepartment[]>();
        foreach (var technician in SeedCatalog.MerkezTechnicians) catalogs[technician] = SeedCatalog.Merkez;
        foreach (var technician in SeedCatalog.AdapazariTechnicians) catalogs[technician] = SeedCatalog.Adapazari;
        foreach (var technician in SeedCatalog.SerdivanTechnicians) catalogs[technician] = SeedCatalog.Serdivan;

        foreach (var technician in technicians)
        {
            var department = catalogs[technician].Single(d => d.Name == technician.Department);
            Assert.NotEmpty(technician.Services);
            Assert.All(technician.Services, name => Assert.Contains(department.Services, s => s.Name == name && s.Type == ServiceType.Diagnostic));
        }

        // Radyoloji (Merkez): one technician does Röntgen and Ultrasonografi, another MR.
        var radiology = SeedCatalog.MerkezTechnicians.Where(t => t.Department == "Radyoloji").ToList();
        Assert.Contains(radiology, t => t.Services.Contains("Röntgen") && t.Services.Contains("Ultrasonografi") && !t.Services.Contains("MR"));
        Assert.Contains(radiology, t => t.Services.SequenceEqual(["MR"]));

        // Laboratuvar: Kan Tahlili plus one or two more laboratory services.
        foreach (var technician in technicians.Where(t => t.Department == "Laboratuvar"))
        {
            Assert.Contains("Kan Tahlili", technician.Services);
            Assert.InRange(technician.Services.Length, 2, 3);
        }

        var emails = technicians.Select(t => t.Email).Concat(SeedCatalog.DoctorEmails).ToList();
        Assert.Equal(emails.Count, emails.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ExactlyOneSeededDoctor_ApprovesAppointmentsHimself_ForTheApprovalDemo()
    {
        var approvers = Branches.SelectMany(b => b).SelectMany(d => d.Doctors).Where(d => !d.AutoConfirm).ToList();

        var doctor = Assert.Single(approvers);
        Assert.Equal("kemal.aslan@sakaryasehir.example", doctor.Email);
        Assert.Contains(SeedCatalog.Merkez.Single(d => d.Name == "Kardiyoloji").Doctors, d => d.Email == doctor.Email);
    }

    [Fact]
    public void DepartmentsWithoutExaminationServices_HaveNoDoctors()
    {
        foreach (var department in Branches.SelectMany(b => b).Where(d => d.Services.All(s => s.Type == ServiceType.Diagnostic)))
        {
            Assert.Empty(department.Doctors);
        }
    }

    [Fact]
    public void DepartmentNames_AreUniquePerBranch_AndGastroenterologyIsItsOwnDepartment()
    {
        foreach (var branch in Branches)
        {
            Assert.Equal(branch.Length, branch.Select(d => d.Name).Distinct().Count());
        }

        Assert.Contains(SeedCatalog.Merkez, d => d.Name == "İç Hastalıkları (Dahiliye)");
        Assert.Contains(SeedCatalog.Merkez, d => d.Name == "Gastroenteroloji");
        Assert.DoesNotContain(Branches.SelectMany(b => b), d => d.Name == "Dahiliye");
    }

    [Fact]
    public void DoctorEmails_AreUnique_AndAllGetADemoLogin()
    {
        var emails = SeedCatalog.DoctorEmails.ToList();

        Assert.Equal(emails.Count, emails.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(emails, email => Assert.Contains(email, DbInitializer.DemoEmails));
    }

    [Fact]
    public void AtLeastOneDepartmentHasASingleService_SoAutoSelectCanBeDemonstrated()
    {
        Assert.Contains(SeedCatalog.Merkez, d => d.Services.Length == 1 && d.Doctors.Length >= 2);
        Assert.Contains(SeedCatalog.Merkez, d => d.Services.Length > 1);
    }

    [Fact]
    public void EveryService_HasARealisticPriceAndDuration()
    {
        foreach (var service in Branches.SelectMany(b => b).SelectMany(d => d.Services))
        {
            Assert.InRange(service.Price, 100m, 5000m);
            Assert.InRange(service.DurationMinutes, 10, 120);
        }
    }

    [Fact]
    public void MerkezHasAThirdDoctor_InDahiliyeAndCocukSagligi_WithTheAskedSlotsAndServices()
    {
        var dahiliye = SeedCatalog.Merkez.Single(d => d.Name == "İç Hastalıkları (Dahiliye)").Doctors.Single(d => d.Email == "ayhan.demirci@sakaryasehir.example");
        var cocuk = SeedCatalog.Merkez.Single(d => d.Name == "Çocuk Sağlığı ve Hastalıkları").Doctors.Single(d => d.Email == "nilufer.kaplan@sakaryasehir.example");

        Assert.Equal("Dr. Ayhan Demirci", dahiliye.FullName);
        Assert.Equal(20, dahiliye.SlotDurationMinutes);
        Assert.True(dahiliye.AutoConfirm);
        Assert.Null(dahiliye.OnlyServices); // both Genel Muayene and Kontrol Muayenesi

        Assert.Equal("Dr. Nilüfer Kaplan", cocuk.FullName);
        Assert.Equal(15, cocuk.SlotDurationMinutes);
        Assert.True(cocuk.AutoConfirm);
        Assert.Equal(["Genel Muayene"], cocuk.OnlyServices ?? []);

        // The other branches keep their two doctors per department.
        Assert.All(SeedCatalog.Adapazari.Concat(SeedCatalog.Serdivan).Where(d => d.Doctors.Length > 0), d => Assert.Equal(2, d.Doctors.Length));
    }
}
