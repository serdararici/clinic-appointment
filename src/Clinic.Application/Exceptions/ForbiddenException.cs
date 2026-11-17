namespace Clinic.Application.Exceptions;

/// <summary>Thrown when the caller is logged in with an allowed role but the resource is not theirs (403).</summary>
public class ForbiddenException(string message) : Exception(message)
{
}
