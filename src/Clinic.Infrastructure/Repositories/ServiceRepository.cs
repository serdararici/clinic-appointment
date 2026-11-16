using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;
using Clinic.Infrastructure.Persistence;

namespace Clinic.Infrastructure.Repositories;

public class ServiceRepository(ClinicDbContext dbContext)
    : Repository<Service>(dbContext), IServiceRepository
{
}
