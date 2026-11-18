using Clinic.Application.Auth;
using Clinic.Domain.Repositories;
using Clinic.Domain.Strategies;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Repositories;
using Clinic.Infrastructure.Security;
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

        // JWT settings come from configuration (Jwt:Key, Jwt:Issuer, Jwt:Audience); a missing or too-short key
        // stops the app at startup instead of issuing weakly signed tokens.
        var jwtOptions = new JwtOptions
        {
            Key = configuration["Jwt:Key"] ?? string.Empty,
            Issuer = configuration["Jwt:Issuer"] ?? string.Empty,
            Audience = configuration["Jwt:Audience"] ?? string.Empty
        };
        jwtOptions.Validate();
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IPasswordHashing, AspNetPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IStaffMemberRepository, StaffMemberRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IClinicFacilityRepository, ClinicFacilityRepository>();
        services.AddScoped<IHealthOrganizationRepository, HealthOrganizationRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        // Default strategy implementations (Strategy pattern); swap these registrations
        // to change pricing/approval behavior without touching AppointmentService.
        services.AddScoped<IPricingStrategy, InsuranceDiscountPricingStrategy>();
        services.AddScoped<IConfirmationPolicy, AutoConfirmationPolicy>();

        return services;
    }
}
