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
}
