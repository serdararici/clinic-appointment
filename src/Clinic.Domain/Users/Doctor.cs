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

    /// <summary>Whether this doctor is registered as providing the given service.</summary>
    public bool OffersService(Guid serviceId) => _servicesProvided.Any(s => s.Id == serviceId);

    public DoctorAvailability AddAvailability(DateTime startUtc, DateTime endUtc)
    {
        // Constructing first validates the range (end after start).
        var slot = new DoctorAvailability(Id, startUtc, endUtc);

        // Overlapping windows would be ambiguous to remove later and would let the
        // same time be "added twice", so they are rejected.
        if (_availabilitySlots.Any(existing => existing.Overlaps(startUtc, endUtc)))
        {
            throw new InvalidOperationException("The availability range overlaps an existing availability range.");
        }

        _availabilitySlots.Add(slot);
        return slot;
    }

    /// <summary>Removes an availability window by id; returns the removed slot, or null if none matched.</summary>
    public DoctorAvailability? RemoveAvailability(Guid slotId)
    {
        var slot = _availabilitySlots.FirstOrDefault(s => s.Id == slotId);
        if (slot is not null)
        {
            _availabilitySlots.Remove(slot);
        }

        return slot;
    }

    /// <summary>Whether this doctor has an availability window fully covering [start, end).</summary>
    public bool IsAvailableDuring(DateTime start, DateTime end) =>
        _availabilitySlots.Any(slot => slot.Contains(start, end));
}
