using Microsoft.AspNetCore.Diagnostics;

namespace Clinic.Api.ErrorHandling;

/// <summary>
/// Maps the three generic exception types the Application and Domain layers use to HTTP status codes,
/// so management controllers do not repeat a try/catch in every action:
/// not found -> 404, invalid input -> 400, a rule or dependent data in the way -> 409.
/// The body is the plain message, like the older controllers return. Any other exception is not
/// handled here and becomes a 500.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? status = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => null
        };

        if (status is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = status.Value;
        await httpContext.Response.WriteAsync(exception.Message, cancellationToken);
        return true;
    }
}
