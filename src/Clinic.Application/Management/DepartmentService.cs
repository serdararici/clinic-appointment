using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;

namespace Clinic.Application.Management;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<DepartmentDto> GetAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateAsync(Guid branchId, SaveDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<DepartmentDto> UpdateAsync(Guid departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the department; refused while it still has services or staff.</summary>
    Task DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default);
}

/// <summary>Manages departments under a branch. A department's branch never changes.</summary>
public class DepartmentService(
    IClinicFacilityRepository clinics,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentDto>> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        _ = await clinics.GetByIdAsync(branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Branch {branchId} was not found.");

        return (await departments.GetByClinicFacilityIdAsync(branchId, cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<DepartmentDto> GetAsync(Guid departmentId, CancellationToken cancellationToken = default) =>
        ToDto(await LoadAsync(departmentId, cancellationToken));

    public async Task<DepartmentDto> CreateAsync(Guid branchId, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        _ = await clinics.GetByIdAsync(branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Branch {branchId} was not found.");

        var department = new Department(branchId, request.Name);

        await departments.AddAsync(department, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(department);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await LoadAsync(departmentId, cancellationToken);

        department.Rename(request.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(department);
    }

    public async Task DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await LoadAsync(departmentId, cancellationToken);

        if (await departments.HasServicesAsync(departmentId, cancellationToken))
        {
            throw new InvalidOperationException("The department still has services; delete them first.");
        }

        if (await departments.HasStaffAsync(departmentId, cancellationToken))
        {
            throw new InvalidOperationException("The department still has staff; delete or move them first.");
        }

        departments.Remove(department);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Department> LoadAsync(Guid departmentId, CancellationToken cancellationToken) =>
        await departments.GetByIdAsync(departmentId, cancellationToken)
        ?? throw new KeyNotFoundException($"Department {departmentId} was not found.");

    private static DepartmentDto ToDto(Department department) => new(department.Id, department.ClinicFacilityId, department.Name);
}
