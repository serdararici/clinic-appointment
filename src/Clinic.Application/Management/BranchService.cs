using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;

namespace Clinic.Application.Management;

public interface IBranchService
{
    /// <summary>Every branch with its organization's name, ordered by organization then branch name.</summary>
    Task<IReadOnlyList<BranchSummaryDto>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchDto>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<BranchDto> GetAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<BranchDto> CreateAsync(Guid organizationId, SaveBranchRequest request, CancellationToken cancellationToken = default);

    Task<BranchDto> UpdateAsync(Guid branchId, SaveBranchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the branch; refused while it still has departments or managers. Its working hours go with it.</summary>
    Task DeleteAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>Working hours ordered Monday to Sunday, optionally only for one weekday.</summary>
    Task<IReadOnlyList<WorkingHourDto>> GetWorkingHoursAsync(Guid branchId, DayOfWeek? dayOfWeek, CancellationToken cancellationToken = default);

    Task<WorkingHourDto> AddWorkingHourAsync(Guid branchId, AddWorkingHourRequest request, CancellationToken cancellationToken = default);

    Task RemoveWorkingHourAsync(Guid branchId, Guid workingHourId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages branches (<see cref="ClinicFacility"/>) under an organization and their weekly working hours.
/// The organization must exist before a branch can be created under it.
/// </summary>
public class BranchService(
    IHealthOrganizationRepository organizations,
    IClinicFacilityRepository clinics,
    IUnitOfWork unitOfWork) : IBranchService
{
    public async Task<IReadOnlyList<BranchSummaryDto>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var organizationNames = (await organizations.GetAllAsync(cancellationToken)).ToDictionary(o => o.Id, o => o.Name);

        return (await clinics.GetAllAsync(cancellationToken))
            .Select(b => new BranchSummaryDto(b.Id, b.OrganizationId, organizationNames.GetValueOrDefault(b.OrganizationId, string.Empty), b.Name, b.Address))
            .OrderBy(b => b.OrganizationName)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<BranchDto>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        _ = await organizations.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Organization {organizationId} was not found.");

        return (await clinics.GetByOrganizationIdAsync(organizationId, cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<BranchDto> GetAsync(Guid branchId, CancellationToken cancellationToken = default) =>
        ToDto(await LoadAsync(branchId, cancellationToken));

    public async Task<BranchDto> CreateAsync(Guid organizationId, SaveBranchRequest request, CancellationToken cancellationToken = default)
    {
        // A branch cannot exist without its organization, so the organization is checked first (404, not a DB error).
        _ = await organizations.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Organization {organizationId} was not found.");

        var branch = new ClinicFacility(organizationId, request.Name, request.Address);

        await clinics.AddAsync(branch, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(branch);
    }

    public async Task<BranchDto> UpdateAsync(Guid branchId, SaveBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await LoadAsync(branchId, cancellationToken);

        branch.UpdateDetails(request.Name, request.Address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(branch);
    }

    public async Task DeleteAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        // Loaded with its departments and working hours. Staff and services always sit under a
        // department, so "no departments" also means no staff and no services.
        var branch = await LoadAsync(branchId, cancellationToken);

        if (branch.Departments.Count > 0)
        {
            throw new InvalidOperationException("The branch still has departments; delete them first.");
        }

        if (await clinics.HasManagersAsync(branchId, cancellationToken))
        {
            throw new InvalidOperationException("The branch still has clinic managers assigned to it.");
        }

        clinics.Remove(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkingHourDto>> GetWorkingHoursAsync(Guid branchId, DayOfWeek? dayOfWeek, CancellationToken cancellationToken = default)
    {
        var branch = await LoadAsync(branchId, cancellationToken);

        return branch.WorkingHours
            .Where(wh => dayOfWeek is null || wh.DayOfWeek == dayOfWeek)
            .OrderBy(wh => ((int)wh.DayOfWeek + 6) % 7) // Monday first, Sunday last
            .ThenBy(wh => wh.OpensAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<WorkingHourDto> AddWorkingHourAsync(Guid branchId, AddWorkingHourRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await LoadAsync(branchId, cancellationToken);

        // Validates the range and rejects an overlap with an existing range on the same weekday.
        var workingHour = branch.AddWorkingHour(request.DayOfWeek, request.OpensAt, request.ClosesAt);

        clinics.AddWorkingHour(workingHour);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(workingHour);
    }

    public async Task RemoveWorkingHourAsync(Guid branchId, Guid workingHourId, CancellationToken cancellationToken = default)
    {
        var branch = await LoadAsync(branchId, cancellationToken);

        var workingHour = branch.RemoveWorkingHour(workingHourId)
            ?? throw new KeyNotFoundException($"Working hours {workingHourId} were not found for branch {branchId}.");

        clinics.RemoveWorkingHour(workingHour);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ClinicFacility> LoadAsync(Guid branchId, CancellationToken cancellationToken) =>
        await clinics.GetByIdAsync(branchId, cancellationToken)
        ?? throw new KeyNotFoundException($"Branch {branchId} was not found.");

    private static BranchDto ToDto(ClinicFacility branch) => new(branch.Id, branch.OrganizationId, branch.Name, branch.Address);

    private static WorkingHourDto ToDto(WorkingHour workingHour) =>
        new(workingHour.Id, workingHour.ClinicFacilityId, workingHour.DayOfWeek, workingHour.OpensAt, workingHour.ClosesAt);
}
