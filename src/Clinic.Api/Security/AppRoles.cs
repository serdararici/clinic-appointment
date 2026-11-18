using Clinic.Application.Auth;

namespace Clinic.Api.Security;

/// <summary>
/// Role names used in <c>[Authorize(Roles = ...)]</c>. They are the same strings as <see cref="RoleNames"/>, which
/// equal <c>User.RoleName</c> and are written into the token's "role" claim (a test checks this).
/// </summary>
public static class AppRoles
{
    public const string Patient = RoleNames.Patient;
    public const string Doctor = RoleNames.Doctor;
    public const string HealthTechnician = RoleNames.HealthTechnician;
    public const string ClinicManager = RoleNames.ClinicManager;
    public const string SystemAdmin = RoleNames.SystemAdmin;

    // A comma-separated list means "any of these roles". A system administrator may use every management endpoint.
    public const string ClinicManagerOrAdmin = ClinicManager + "," + SystemAdmin;
    public const string DoctorOrManagerOrAdmin = Doctor + "," + ClinicManager + "," + SystemAdmin;
    public const string PatientOrManagerOrAdmin = Patient + "," + ClinicManager + "," + SystemAdmin;
    public const string PatientOrDoctorOrManagerOrAdmin = Patient + "," + Doctor + "," + ClinicManager + "," + SystemAdmin;
}
