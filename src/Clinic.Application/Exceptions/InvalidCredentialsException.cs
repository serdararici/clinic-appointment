namespace Clinic.Application.Exceptions;

/// <summary>
/// Thrown when a login fails. The message is the same for an unknown email and a wrong password,
/// so the response does not reveal which emails have accounts.
/// </summary>
public class InvalidCredentialsException() : Exception("Invalid email or password.")
{
}
