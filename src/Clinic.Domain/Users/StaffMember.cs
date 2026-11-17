using Clinic.Domain.Clinics;

namespace Clinic.Domain.Users;

/// <summary>
/// Abstract base for everyone who works in a branch department (doctors, technicians).
/// Holds the facility/department link, the services the person provides, availability windows
/// and the slot duration; role-specific data lives in the subclasses.
/// </summary>
public abstract class StaffMember : User
{
    public const int DefaultSlotDurationMinutes = 30;

    /// <summary>The only slot lengths a manager may choose, in minutes.</summary>
    public static readonly IReadOnlyList<int> AllowedSlotDurationMinutes = [10, 15, 20, 30, 60];

    public Guid ClinicFacilityId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public int SlotDurationMinutes { get; private set; }

    /// <summary>
    /// False once the person has been deactivated (e.g. left the clinic). Deactivated staff keep their
    /// history but can no longer be booked.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    private readonly List<Service> _servicesProvided = [];
    public IReadOnlyCollection<Service> ServicesProvided => _servicesProvided.AsReadOnly();

    private readonly List<DoctorAvailability> _availabilitySlots = [];
    public IReadOnlyCollection<DoctorAvailability> AvailabilitySlots => _availabilitySlots.AsReadOnly();

    /// <remarks>
    /// Takes the <see cref="Department"/> itself rather than two ids: the facility is read from the
    /// department, so a staff member can never be linked to a department of a different branch.
    /// </remarks>
    protected StaffMember(
        string fullName,
        string email,
        string phoneNumber,
        Department department,
        int slotDurationMinutes = DefaultSlotDurationMinutes)
        : base(fullName, email, phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(department);

        ClinicFacilityId = department.ClinicFacilityId;
        DepartmentId = department.Id;
        SlotDurationMinutes = ValidateSlotDuration(slotDurationMinutes);
    }

    protected StaffMember()
    {
    }

    public void SetSlotDuration(int minutes) => SlotDurationMinutes = ValidateSlotDuration(minutes);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>Registers a service this person provides; it must belong to their own department.</summary>
    public void AddService(Service service)
    {
        ArgumentNullException.ThrowIfNull(service);

        EnsureSameDepartment(service);

        if (!_servicesProvided.Any(s => s.Id == service.Id))
        {
            _servicesProvided.Add(service);
        }
    }

    /// <summary>
    /// Replaces the whole set of offered services. Every service must belong to the staff member's
    /// own department; if any does not, nothing changes.
    /// </summary>
    public void SetServices(IEnumerable<Service> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var wanted = services.DistinctBy(s => s.Id).ToList();
        foreach (var service in wanted)
        {
            EnsureSameDepartment(service);
        }

        _servicesProvided.Clear();
        _servicesProvided.AddRange(wanted);
    }

    private void EnsureSameDepartment(Service service)
    {
        if (service.DepartmentId != DepartmentId)
        {
            throw new ArgumentException(
                $"Service '{service.Name}' belongs to a different department; a staff member can only provide services of their own department.",
                nameof(service));
        }
    }

    /// <summary>Whether this person is registered as providing the given service.</summary>
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

    /// <summary>Whether an availability window fully covers [start, end).</summary>
    public bool IsAvailableDuring(DateTime start, DateTime end) =>
        _availabilitySlots.Any(slot => slot.Contains(start, end));

    private static int ValidateSlotDuration(int minutes)
    {
        if (!AllowedSlotDurationMinutes.Contains(minutes))
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutes),
                $"Slot duration must be one of: {string.Join(", ", AllowedSlotDurationMinutes)} minutes.");
        }

        return minutes;
    }
}
