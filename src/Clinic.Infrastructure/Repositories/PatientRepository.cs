using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;

namespace Clinic.Infrastructure.Repositories;

public class PatientRepository(ClinicDbContext dbContext)
    : Repository<Patient>(dbContext), IPatientRepository
{
}
