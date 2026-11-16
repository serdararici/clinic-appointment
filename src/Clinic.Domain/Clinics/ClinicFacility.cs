using Clinic.Domain.Common;

namespace Clinic.Domain.Clinics;

/// <summary>
/// A hospital or clinic location. A healthcare provider may run a single facility
/// or several (multi-branch), each with its own working hours and departments.
/// </summary>
public class ClinicFacility : Entity
{
    public string Name { get; private set; }
    public string Address { get; private set; }

    private readonly List<Department> _departments = [];
    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    private readonly List<WorkingHour> _workingHours = [];
    public IReadOnlyCollection<WorkingHour> WorkingHours => _workingHours.AsReadOnly();

    public ClinicFacility(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Clinic name is required.", nameof(name));
        }

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
}
