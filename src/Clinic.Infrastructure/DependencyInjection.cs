using Clinic.Domain.Repositories;
using Clinic.Domain.Strategies;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Infrastructure;

/// <summary>Registers EF Core, repositories, and the default pattern implementations.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClinicDb")
            ?? throw new InvalidOperationException("Connection string 'ClinicDb' was not found.");

        services.AddDbContext<ClinicDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IStaffMemberRepository, StaffMemberRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IClinicFacilityRepository, ClinicFacilityRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        // Default strategy implementations (Strategy pattern); swap these registrations
        // to change pricing/approval behavior without touching AppointmentService.
        services.AddScoped<IPricingStrategy, InsuranceDiscountPricingStrategy>();
        services.AddScoped<IConfirmationPolicy, ManagerApprovalConfirmationPolicy>();

        return services;
    }
}
