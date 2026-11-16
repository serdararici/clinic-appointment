using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Seed;

/// <summary>Applies pending migrations and seeds sample data, run once on application startup.</summary>
public static class DbInitializer
{
    public static void MigrateAndSeed(ClinicDbContext dbContext)
    {
        dbContext.Database.Migrate();

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

        var doctor = new Doctor("Dr. Mehmet Demir", "mehmet.demir@clinic.example", "+90-500-000-0002", clinic.Id, "General Medicine");
        doctor.AddService(generalCheckUp);

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        doctor.AddAvailability(tomorrow.AddHours(9), tomorrow.AddHours(12));
        doctor.AddAvailability(tomorrow.AddHours(13), tomorrow.AddHours(16));

        var radiologyDoctor = new Doctor("Dr. Elif Kaya", "elif.kaya@clinic.example", "+90-500-000-0003", clinic.Id, "Radiology");
        radiologyDoctor.AddService(xRay);
        radiologyDoctor.AddAvailability(tomorrow.AddHours(10), tomorrow.AddHours(15));

        var patient = new Patient("Ahmet Çelik", "ahmet.celik@example.com", "+90-500-000-0004", new DateOnly(1990, 5, 20), insuranceNumber: "INS-12345");

        dbContext.ClinicManagers.Add(manager);
        dbContext.Doctors.AddRange(doctor, radiologyDoctor);
        dbContext.Patients.Add(patient);

        dbContext.SaveChanges();
    }
}
