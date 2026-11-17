using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

/// <summary>Repository over every kind of staff member (doctors and technicians).</summary>
public interface IStaffMemberRepository : IRepository<StaffMember>
{
    /// <summary>
    /// Staff of a branch and/or department (a null filter means "any"), with their offered services loaded,
    /// ordered by name.
    /// </summary>
    Task<IReadOnlyList<StaffMember>> ListAsync(Guid? clinicFacilityId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>Whether any staff member is registered as offering the service.</summary>
    Task<bool> AnyOfferingServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
