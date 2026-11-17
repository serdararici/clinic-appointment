using Clinic.Domain.Common;

namespace Clinic.Domain.Clinics;

/// <summary>
/// A branch of a <see cref="HealthOrganization"/> (e.g. "Merkez" or "Adapazarı Şubesi").
/// An organization may run a single branch or several, each with its own working hours
/// and departments. <see cref="Name"/> is the branch name, not the organization's.
/// </summary>
public class ClinicFacility : Entity
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }

    private readonly List<Department> _departments = [];
    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    private readonly List<WorkingHour> _workingHours = [];
    public IReadOnlyCollection<WorkingHour> WorkingHours => _workingHours.AsReadOnly();

    public ClinicFacility(Guid organizationId, string name, string address)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("A branch must belong to an organization.", nameof(organizationId));
        }

        OrganizationId = organizationId;
        Name = ValidateName(name);
        Address = ValidateAddress(address);
    }

    protected ClinicFacility()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    /// <summary>Changes the branch name and address; the branch stays in its organization.</summary>
    public void UpdateDetails(string name, string address)
    {
        var validName = ValidateName(name);
        var validAddress = ValidateAddress(address);

        Name = validName;
        Address = validAddress;
    }

    public Department AddDepartment(string name)
    {
        var department = new Department(Id, name);
        _departments.Add(department);
        return department;
    }

    public WorkingHour AddWorkingHour(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        // Constructing first validates the range (closing after opening).
        var workingHour = new WorkingHour(Id, dayOfWeek, opensAt, closesAt);

        // Two opening ranges on the same day must not overlap (a lunch break between ranges is fine,
        // and so are back-to-back ranges): overlapping hours would make "open" ambiguous.
        if (_workingHours.Any(existing => existing.Overlaps(dayOfWeek, opensAt, closesAt)))
        {
            throw new InvalidOperationException($"The working hours overlap an existing range on {dayOfWeek}.");
        }

        _workingHours.Add(workingHour);
        return workingHour;
    }

    /// <summary>Removes a working-hours range by id; returns the removed range, or null if none matched.</summary>
    public WorkingHour? RemoveWorkingHour(Guid workingHourId)
    {
        var workingHour = _workingHours.FirstOrDefault(wh => wh.Id == workingHourId);
        if (workingHour is not null)
        {
            _workingHours.Remove(workingHour);
        }

        return workingHour;
    }

    /// <summary>Whether the facility is open at the given day/time, per its configured working hours.</summary>
    public bool IsOpenAt(DayOfWeek day, TimeOnly time) => _workingHours.Any(wh => wh.Contains(day, time));

    /// <summary>
    /// The facility's opening ranges on a clinic-local calendar date, converted to UTC and ordered by
    /// opening time. Empty when the branch is closed that weekday.
    /// </summary>
    public IReadOnlyList<(DateTime OpensUtc, DateTime ClosesUtc)> GetWorkingRangesUtc(DateOnly localDate) =>
        _workingHours
            .Where(wh => wh.DayOfWeek == localDate.DayOfWeek)
            .OrderBy(wh => wh.OpensAt)
            .Select(wh => (
                ClinicTimeZone.ToUtc(localDate.ToDateTime(wh.OpensAt)),
                ClinicTimeZone.ToUtc(localDate.ToDateTime(wh.ClosesAt))))
            .ToList();

    /// <summary>
    /// Whether the whole UTC range [startUtc, endUtc) falls inside the facility's working hours.
    /// Working hours are wall-clock times in the clinic's time zone, so the UTC range is converted
    /// to clinic local time first (e.g. 14:00 UTC is 17:00 in Turkey). A range that crosses
    /// local midnight is never inside working hours.
    /// </summary>
    public bool IsOpenDuring(DateTime startUtc, DateTime endUtc)
    {
        var start = ClinicTimeZone.ToLocal(startUtc);
        var end = ClinicTimeZone.ToLocal(endUtc);

        if (end <= start || end.Date != start.Date)
        {
            return false;
        }

        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);

        return _workingHours.Any(wh => wh.Covers(start.DayOfWeek, startTime, endTime));
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Branch address is required.", nameof(address));
        }

        return address.Trim();
    }
}
