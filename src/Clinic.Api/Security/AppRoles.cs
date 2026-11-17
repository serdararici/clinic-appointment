namespace Clinic.Api.Security;

/// <summary>
/// Role names used in <c>[Authorize(Roles = ...)]</c>. They must equal <c>User.RoleName</c> of the matching
/// entity, because that is the value written into the token's "role" claim (a test checks this).
/// </summary>
public static class AppRoles
{
    public const string Patient = "Patient";
    public const string Doctor = "Doctor";
    public const string HealthTechnician = "HealthTechnician";
    public const string ClinicManager = "ClinicManager";

    /// <summary>A comma-separated list means "any of these roles".</summary>
    public const string DoctorOrClinicManager = Doctor + "," + ClinicManager;
}
