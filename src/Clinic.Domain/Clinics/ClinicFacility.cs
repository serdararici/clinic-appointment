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

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        OrganizationId = organizationId;
        Name = name;
        Address = address;
    }

    protected ClinicFacility()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Department AddDepartment(string name)
    {
        var department = new Department(Id, name);
        _departments.Add(department);
        return department;
    }

    public WorkingHour AddWorkingHour(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        var workingHour = new WorkingHour(Id, dayOfWeek, opensAt, closesAt);
        _workingHours.Add(workingHour);
        return workingHour;
    }

    /// <summary>Whether the facility is open at the given day/time, per its configured working hours.</summary>
    public bool IsOpenAt(DayOfWeek day, TimeOnly time) => _workingHours.Any(wh => wh.Contains(day, time));

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
}
