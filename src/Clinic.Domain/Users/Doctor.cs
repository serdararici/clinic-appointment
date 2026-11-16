using Clinic.Domain.Clinics;

namespace Clinic.Domain.Users;

/// <summary>
/// A doctor or health staff member. Offers a specialty, a set of services, and
/// a set of availability windows that bound when appointments may be booked.
/// </summary>
public class Doctor : User
{
    public Guid ClinicFacilityId { get; private set; }
    public string Specialty { get; private set; }

    private readonly List<Service> _servicesProvided = [];
    public IReadOnlyCollection<Service> ServicesProvided => _servicesProvided.AsReadOnly();

    private readonly List<DoctorAvailability> _availabilitySlots = [];
    public IReadOnlyCollection<DoctorAvailability> AvailabilitySlots => _availabilitySlots.AsReadOnly();

    public Doctor(string fullName, string email, string phoneNumber, Guid clinicFacilityId, string specialty)
        : base(fullName, email, phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(specialty))
        {
            throw new ArgumentException("Specialty is required.", nameof(specialty));
        }

        ClinicFacilityId = clinicFacilityId;
        Specialty = specialty;
    }

    private Doctor()
    {
        Specialty = string.Empty;
    }

    public override string RoleName => "Doctor";

    public void AddService(Service service)
    {
        if (!_servicesProvided.Any(s => s.Id == service.Id))
        {
            _servicesProvided.Add(service);
        }
    }

    public DoctorAvailability AddAvailability(DateTime startUtc, DateTime endUtc)
    {
        var slot = new DoctorAvailability(Id, startUtc, endUtc);
        _availabilitySlots.Add(slot);
        return slot;
    }

    /// <summary>Whether this doctor has an availability window fully covering [start, end).</summary>
    public bool IsAvailableDuring(DateTime start, DateTime end) =>
        _availabilitySlots.Any(slot => slot.Contains(start, end));
}
