using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;

namespace Clinic.Application.Management;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<OrganizationDto> GetAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<OrganizationDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken = default);

    Task<OrganizationDto> UpdateAsync(Guid organizationId, SaveOrganizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the organization; refused (InvalidOperationException) while it still has branches.</summary>
    Task DeleteAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

/// <summary>Manages health organizations (the owners of branches). Validation lives in <see cref="HealthOrganization"/>.</summary>
public class OrganizationService(IHealthOrganizationRepository organizations, IUnitOfWork unitOfWork) : IOrganizationService
{
    public async Task<IReadOnlyList<OrganizationDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await organizations.GetAllAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<OrganizationDto> GetAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        ToDto(await LoadAsync(organizationId, cancellationToken));

    public async Task<OrganizationDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var organization = new HealthOrganization(request.Name);

        await organizations.AddAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(organization);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid organizationId, SaveOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var organization = await LoadAsync(organizationId, cancellationToken);

        organization.Rename(request.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(organization);
    }

    public async Task DeleteAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var organization = await LoadAsync(organizationId, cancellationToken);

        if (await organizations.HasBranchesAsync(organizationId, cancellationToken))
        {
            throw new InvalidOperationException("The organization still has branches; delete its branches first.");
        }

        organizations.Remove(organization);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<HealthOrganization> LoadAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await organizations.GetByIdAsync(organizationId, cancellationToken)
        ?? throw new KeyNotFoundException($"Organization {organizationId} was not found.");

    private static OrganizationDto ToDto(HealthOrganization organization) => new(organization.Id, organization.Name);
}
