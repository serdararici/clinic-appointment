using Clinic.Domain.Clinics;
using Clinic.Domain.Repositories;

namespace Clinic.Application.Management;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceDto>> ListByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<ServiceDto> GetAsync(Guid serviceId, CancellationToken cancellationToken = default);

    Task<ServiceDto> CreateAsync(Guid departmentId, SaveServiceRequest request, CancellationToken cancellationToken = default);

    Task<ServiceDto> UpdateAsync(Guid serviceId, SaveServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the service; refused while appointments refer to it or staff are registered as offering it.</summary>
    Task DeleteAsync(Guid serviceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages the services of a department: type (Examination / Diagnostic), duration and base price.
/// The values are validated by <see cref="Service"/>.
/// </summary>
public class ServiceCatalogService(
    IDepartmentRepository departments,
    IServiceRepository services,
    IAppointmentRepository appointments,
    IStaffMemberRepository staff,
    IUnitOfWork unitOfWork) : IServiceCatalogService
{
    public async Task<IReadOnlyList<ServiceDto>> ListByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        _ = await departments.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Department {departmentId} was not found.");

        return (await services.GetByDepartmentIdAsync(departmentId, cancellationToken)).Select(ToDto).ToList();
    }

    public async Task<ServiceDto> GetAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        ToDto(await LoadAsync(serviceId, cancellationToken));

    public async Task<ServiceDto> CreateAsync(Guid departmentId, SaveServiceRequest request, CancellationToken cancellationToken = default)
    {
        _ = await departments.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Department {departmentId} was not found.");

        var service = new Service(departmentId, request.Name, request.ServiceType, ToDuration(request.DurationMinutes), request.BasePrice);

        await services.AddAsync(service, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(service);
    }

    public async Task<ServiceDto> UpdateAsync(Guid serviceId, SaveServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await LoadAsync(serviceId, cancellationToken);

        // The type decides how appointments are booked and how long they last, so it is frozen once
        // appointments exist; everything else stays editable.
        if (request.ServiceType != service.ServiceType && await appointments.AnyForServiceAsync(serviceId, cancellationToken))
        {
            throw new InvalidOperationException("The service type cannot be changed because appointments already exist for this service.");
        }

        service.Update(request.Name, request.ServiceType, ToDuration(request.DurationMinutes), request.BasePrice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(service);
    }

    public async Task DeleteAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await LoadAsync(serviceId, cancellationToken);

        if (await appointments.AnyForServiceAsync(serviceId, cancellationToken))
        {
            throw new InvalidOperationException("Appointments refer to this service, so it cannot be deleted.");
        }

        if (await staff.AnyOfferingServiceAsync(serviceId, cancellationToken))
        {
            throw new InvalidOperationException("Staff are registered as offering this service; remove it from them first.");
        }

        services.Remove(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Service> LoadAsync(Guid serviceId, CancellationToken cancellationToken) =>
        await services.GetByIdAsync(serviceId, cancellationToken)
        ?? throw new KeyNotFoundException($"Service {serviceId} was not found.");

    // The API speaks whole minutes; zero, negative and over-long values are rejected by the Service entity.
    private static TimeSpan ToDuration(int minutes) => TimeSpan.FromMinutes(minutes);

    private static ServiceDto ToDto(Service service) => ServiceDto.From(service);
}
