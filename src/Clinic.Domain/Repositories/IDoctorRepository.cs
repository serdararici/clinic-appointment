using Clinic.Domain.Users;

namespace Clinic.Domain.Repositories;

public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>
    /// Marks a slot created via <see cref="Doctor.AddAvailability"/> as new. Needed because slots
    /// get their Guid id client-side, so EF cannot tell on its own that they are not yet in the database.
    /// </summary>
    void AddAvailability(DoctorAvailability slot);

    /// <summary>Marks a slot removed via <see cref="Doctor.RemoveAvailability"/> for deletion.</summary>
    void RemoveAvailability(DoctorAvailability slot);
}
