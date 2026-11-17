using Clinic.Domain.Repositories;
using Clinic.Domain.Users;
using Clinic.Infrastructure.Persistence;

namespace Clinic.Infrastructure.Repositories;

public class StaffMemberRepository(ClinicDbContext dbContext)
    : Repository<StaffMember>(dbContext), IStaffMemberRepository
{
}
