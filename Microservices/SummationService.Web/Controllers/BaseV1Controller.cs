using Microsoft.AspNetCore.Mvc;

namespace SummationService.Web.Controllers;

/// <summary>
/// Base API controller for the project.
/// </summary>
public class BaseV1Controller : ControllerBase
{
    /// <summary>
    /// Error thrown with description.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The error with the description.</returns>
    protected IActionResult NotFoundWithProblemDetails(string message)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = message
        };

        return this.NotFound(details);
    }
}
