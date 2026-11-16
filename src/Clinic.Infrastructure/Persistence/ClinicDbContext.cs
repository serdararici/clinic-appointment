using Clinic.Domain.Appointments;
using Clinic.Domain.Clinics;
using Clinic.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Persistence;

/// <summary>EF Core database context for the clinic appointment domain.</summary>
public class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<ClinicManager> ClinicManagers => Set<ClinicManager>();

    public DbSet<ClinicFacility> ClinicFacilities => Set<ClinicFacility>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users are mapped as a single table with a discriminator column (TPH),
        // modeling the abstract User -> Patient/Doctor/ClinicManager inheritance.
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Patient>("Patient")
            .HasValue<Doctor>("Doctor")
            .HasValue<ClinicManager>("ClinicManager");

        modelBuilder.Entity<ClinicFacility>(builder =>
        {
            builder.HasMany(c => c.Departments).WithOne().HasForeignKey(d => d.ClinicFacilityId);
            builder.Navigation(c => c.Departments).HasField("_departments").UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(c => c.WorkingHours).WithOne().HasForeignKey(w => w.ClinicFacilityId);
            builder.Navigation(c => c.WorkingHours).HasField("_workingHours").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Department>(builder =>
        {
            builder.HasMany(d => d.Services).WithOne().HasForeignKey(s => s.DepartmentId);
            builder.Navigation(d => d.Services).HasField("_services").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Service>(builder =>
        {
            builder.Property(s => s.BasePrice).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Doctor>(builder =>
        {
            builder.HasMany(d => d.AvailabilitySlots).WithOne().HasForeignKey(a => a.DoctorId);
            builder.Navigation(d => d.AvailabilitySlots).HasField("_availabilitySlots").UsePropertyAccessMode(PropertyAccessMode.Field);

            // A doctor providing a service is a plain many-to-many relation.
            builder.HasMany(d => d.ServicesProvided).WithMany();
            builder.Navigation(d => d.ServicesProvided).HasField("_servicesProvided").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Appointment>(builder =>
        {
            builder.Property(a => a.Price).HasColumnType("decimal(10,2)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
