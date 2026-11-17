using Clinic.Domain.Common;

namespace Clinic.Domain.Clinics;

/// <summary>
/// The company/institution that owns one or more branches (e.g. a hospital group).
/// A branch (<see cref="ClinicFacility"/>) cannot exist without its organization, so the
/// organization is created first.
/// </summary>
public class HealthOrganization : Entity
{
    public string Name { get; private set; }

    public HealthOrganization(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Organization name is required.", nameof(name));
        }

        Name = name;
    }

    /// <summary>EF Core requires a parameterless constructor for materialization.</summary>
    protected HealthOrganization()
    {
        Name = string.Empty;
    }
}
