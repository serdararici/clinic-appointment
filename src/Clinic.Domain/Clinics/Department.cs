using Clinic.Domain.Common;
using Clinic.Domain.Enums;

namespace Clinic.Domain.Clinics;

/// <summary>
/// An organizational unit within a clinic facility (e.g. Radiology, General Medicine).
/// </summary>
public class Department : Entity
{
    public Guid ClinicFacilityId { get; private set; }
    public string Name { get; private set; }

    private readonly List<Service> _services = [];
    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

    public Department(Guid clinicFacilityId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Department name is required.", nameof(name));
        }

        ClinicFacilityId = clinicFacilityId;
        Name = name;
    }

    protected Department()
    {
        Name = string.Empty;
    }

    public Service AddService(string name, ServiceType serviceType, TimeSpan duration, decimal price)
    {
        var service = new Service(Id, name, serviceType, duration, price);
        _services.Add(service);
        return service;
    }
}
