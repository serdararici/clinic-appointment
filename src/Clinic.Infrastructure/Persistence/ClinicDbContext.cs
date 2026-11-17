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
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<HealthTechnician> HealthTechnicians => Set<HealthTechnician>();
    public DbSet<ClinicManager> ClinicManagers => Set<ClinicManager>();

    public DbSet<HealthOrganization> HealthOrganizations => Set<HealthOrganization>();
    public DbSet<ClinicFacility> ClinicFacilities => Set<ClinicFacility>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users are mapped as a single table with a discriminator column (TPH), modeling
        // User -> Patient / ClinicManager / StaffMember -> Doctor / HealthTechnician.
        // StaffMember is abstract: it has no discriminator value of its own, but it must be an
        // explicit entity type so its columns are shared by Doctor and HealthTechnician.
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Patient>("Patient")
            .HasValue<Doctor>("Doctor")
            .HasValue<HealthTechnician>("HealthTechnician")
            .HasValue<ClinicManager>("ClinicManager");

        modelBuilder.Entity<ClinicFacility>(builder =>
        {
            // A branch always belongs to an organization, which must exist first.
            builder.HasOne<HealthOrganization>().WithMany().HasForeignKey(c => c.OrganizationId)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<StaffMember>(builder =>
        {
            // A real link to the department (and branch). Restrict avoids multiple cascade paths in SQL Server.
            builder.HasOne<ClinicFacility>().WithMany().HasForeignKey(s => s.ClinicFacilityId)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Department>().WithMany().HasForeignKey(s => s.DepartmentId)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.AvailabilitySlots).WithOne().HasForeignKey(a => a.StaffMemberId);
            builder.Navigation(s => s.AvailabilitySlots).HasField("_availabilitySlots").UsePropertyAccessMode(PropertyAccessMode.Field);

            // A staff member providing a service is a plain many-to-many relation.
            builder.HasMany(s => s.ServicesProvided).WithMany();
            builder.Navigation(s => s.ServicesProvided).HasField("_servicesProvided").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Appointment>(builder =>
        {
            builder.Property(a => a.Price).HasColumnType("decimal(10,2)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
