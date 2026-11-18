using Clinic.Domain.Clinics;
using Clinic.Domain.Users;

namespace Clinic.Tests;

/// <summary>Builders for valid domain objects, so tests only spell out what they are testing.</summary>
internal static class TestData
{
    public static Department NewDepartment(string name = "Dahiliye") => new(Guid.NewGuid(), name);

    public static Doctor NewDoctor(Department? department = null, int slotDurationMinutes = 30, string email = "doctor@example.com") =>
        new("Dr. Test", email, "+90-500-000-0000", department ?? NewDepartment(), slotDurationMinutes);
}
