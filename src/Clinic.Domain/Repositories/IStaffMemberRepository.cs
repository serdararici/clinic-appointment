using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

/// <summary>Repository over every kind of staff member (doctors and technicians).</summary>
public interface IStaffMemberRepository : IRepository<StaffMember>
{
}
