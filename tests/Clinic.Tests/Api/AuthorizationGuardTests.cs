using System.Reflection;
using Clinic.Api.Controllers;
using Clinic.Api.Security;
using Clinic.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Clinic.Tests.Api;

/// <summary>
/// Guards the API's access rules by reflection, so a new endpoint cannot be added without deciding who may call it.
/// </summary>
public class AuthorizationGuardTests
{
    private static readonly string[] KnownRoles = [AppRoles.Patient, AppRoles.Doctor, AppRoles.HealthTechnician, AppRoles.ClinicManager, AppRoles.SystemAdmin];

    /// <summary>Every public controller action: a method carrying an HTTP verb attribute.</summary>
    private static List<MethodInfo> Actions() => typeof(AuthController).Assembly.GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
        .ToList();

    private static string NameOf(MethodInfo action) => $"{action.DeclaringType!.Name}.{action.Name}";

    // The action's own attributes win; if it has none, the controller's apply.
    private static (bool Anonymous, List<AuthorizeAttribute> Authorize) Effective(MethodInfo action)
    {
        var actionAuthorize = action.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        var actionAnonymous = action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

        if (actionAnonymous || actionAuthorize.Count > 0)
        {
            return (actionAnonymous, actionAuthorize);
        }

        return (
            action.DeclaringType!.IsDefined(typeof(AllowAnonymousAttribute), inherit: true),
            action.DeclaringType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList());
    }

    /// <summary>The roles allowed to call the action, sorted; null when it is anonymous.</summary>
    private static string? AllowedRoles(Type controller, string actionName)
    {
        var action = Actions().Single(m => m.DeclaringType == controller && m.Name == actionName);
        var (anonymous, authorize) = Effective(action);

        return anonymous
            ? null
            : string.Join(",", authorize.SelectMany(a => (a.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Order());
    }

    [Fact]
    public void EveryAction_IsExplicitlyAnonymous_OrRestrictedToNamedRoles()
    {
        var actions = Actions();
        Assert.True(actions.Count >= 40, $"Only {actions.Count} actions were found; the scan is probably broken.");

        var problems = new List<string>();
        foreach (var action in actions)
        {
            var (anonymous, authorize) = Effective(action);

            if (anonymous && authorize.Count > 0)
            {
                problems.Add($"{NameOf(action)} is both [AllowAnonymous] and [Authorize]");
            }
            else if (!anonymous && authorize.Count == 0)
            {
                problems.Add($"{NameOf(action)} has neither [AllowAnonymous] nor [Authorize(Roles = ...)]");
            }
            else if (authorize.Any(a => string.IsNullOrWhiteSpace(a.Roles)))
            {
                problems.Add($"{NameOf(action)} uses [Authorize] without Roles (any logged-in user would pass)");
            }
            else if (authorize.SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries)).Any(r => !KnownRoles.Contains(r)))
            {
                problems.Add($"{NameOf(action)} names a role that does not exist");
            }
        }

        Assert.True(problems.Count == 0, "Unprotected or unclear endpoints:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void RoleConstants_EqualTheRoleNamesWrittenIntoTheToken()
    {
        var department = TestData.NewDepartment();

        Assert.Equal(AppRoles.Patient, new Patient("P", "p@example.com", "", new DateOnly(1990, 1, 1)).RoleName);
        Assert.Equal(AppRoles.Doctor, new Doctor("D", "d@example.com", "", department).RoleName);
        Assert.Equal(AppRoles.HealthTechnician, new HealthTechnician("T", "t@example.com", "", department, "X").RoleName);
        Assert.Equal(AppRoles.ClinicManager, new ClinicManager("M", "m@example.com", "", Guid.NewGuid()).RoleName);
        Assert.Equal(AppRoles.SystemAdmin, new SystemAdmin("A", "a@example.com", "").RoleName);
    }

    public static TheoryData<Type, string, string?> RoleMapping => new()
    {
        // Anonymous: login/registration and read-only browsing
        { typeof(AuthController), nameof(AuthController.Login), null },
        { typeof(AuthController), nameof(AuthController.Register), null },
        { typeof(OrganizationsController), nameof(OrganizationsController.List), null },
        { typeof(BranchesController), nameof(BranchesController.GetWorkingHours), null },
        { typeof(DepartmentsController), nameof(DepartmentsController.ListDoctors), null },
        { typeof(ServicesController), nameof(ServicesController.GetSlots), null },
        { typeof(DoctorsController), nameof(DoctorsController.GetSlots), null },

        // System administrator: clinic manager accounts
        { typeof(ManagersController), nameof(ManagersController.List), "SystemAdmin" },
        { typeof(ManagersController), nameof(ManagersController.Update), "SystemAdmin" },
        { typeof(ManagersController), nameof(ManagersController.Deactivate), "SystemAdmin" },
        { typeof(ManagersController), nameof(ManagersController.Activate), "SystemAdmin" },

        // Every logged-in role: the own account
        { typeof(AccountController), nameof(AccountController.Get), "ClinicManager,Doctor,HealthTechnician,Patient,SystemAdmin" },
        { typeof(AccountController), nameof(AccountController.Update), "ClinicManager,Doctor,HealthTechnician,Patient,SystemAdmin" },
        { typeof(AccountController), nameof(AccountController.ChangePassword), "ClinicManager,Doctor,HealthTechnician,Patient,SystemAdmin" },

        // Patient
        { typeof(AppointmentsController), nameof(AppointmentsController.Create), "Patient" },
        { typeof(ServicesController), nameof(ServicesController.PreviewPrice), "Patient" },
        { typeof(AppointmentsController), nameof(AppointmentsController.Cancel), "ClinicManager,Doctor,Patient,SystemAdmin" },
        { typeof(PatientsController), nameof(PatientsController.GetAppointments), "Patient" },

        // Doctor: own availability and appointment list
        { typeof(DoctorsController), nameof(DoctorsController.AddBulk), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.GetAll), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.Remove), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.GetAppointments), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.GetProfile), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.GetAutoSchedule), "Doctor" },
        { typeof(DoctorsController), nameof(DoctorsController.SetAutoSchedule), "Doctor" },

        // Doctor or clinic manager
        { typeof(AppointmentsController), nameof(AppointmentsController.Confirm), "ClinicManager,Doctor,SystemAdmin" },
        { typeof(AppointmentsController), nameof(AppointmentsController.Reject), "ClinicManager,Doctor,SystemAdmin" },
        { typeof(StaffController), nameof(StaffController.SetSlotDuration), "ClinicManager,Doctor,SystemAdmin" },

        // System administrator only: organizations are created and deleted by them, and so are manager accounts
        { typeof(OrganizationsController), nameof(OrganizationsController.Create), "SystemAdmin" },
        { typeof(OrganizationsController), nameof(OrganizationsController.Delete), "SystemAdmin" },
        { typeof(BranchesController), nameof(BranchesController.CreateManager), "SystemAdmin" },

        // Clinic manager (own organization) or system administrator: management
        { typeof(OrganizationsController), nameof(OrganizationsController.Update), "ClinicManager,SystemAdmin" },
        { typeof(BranchesController), nameof(BranchesController.AddWorkingHour), "ClinicManager,SystemAdmin" },
        { typeof(DepartmentsController), nameof(DepartmentsController.CreateDoctor), "ClinicManager,SystemAdmin" },
        { typeof(ServicesController), nameof(ServicesController.Delete), "ClinicManager,SystemAdmin" },
        { typeof(StaffController), nameof(StaffController.Deactivate), "ClinicManager,SystemAdmin" },
        { typeof(StaffController), nameof(StaffController.Activate), "ClinicManager,SystemAdmin" },
        { typeof(StaffController), nameof(StaffController.List), "ClinicManager,SystemAdmin" },
    };

    [Theory]
    [MemberData(nameof(RoleMapping))]
    public void Endpoint_AllowsExactlyTheIntendedRoles(Type controller, string action, string? expectedRoles)
    {
        Assert.Equal(expectedRoles, AllowedRoles(controller, action));
    }
}
