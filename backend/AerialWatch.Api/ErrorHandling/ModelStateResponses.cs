using Microsoft.AspNetCore.Mvc;

namespace AerialWatch.Api.ErrorHandling;

/// <summary>
/// Turns ASP.NET's raw JSON binding errors (e.g. "$.camera: could not be converted…") into friendly field errors
/// with the same shape our services return, e.g. { "camera": ["Invalid value."] }.
/// </summary>
public static class ModelStateResponses
{
    public static IActionResult Friendly(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0) continue;

            // Skip the generic "request field is required" that accompanies JSON conversion errors.
            if (!key.StartsWith("$", StringComparison.Ordinal) && context.ModelState.Keys.Any(k => k.StartsWith("$.", StringComparison.Ordinal)))
                continue;

            var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
            if (field is "$" or "") field = "body";
            field = char.ToLowerInvariant(field[0]) + field[1..];

            errors[field] = field == "body"
                ? new[] { "The request body is missing or not valid JSON." }
                : new[] { $"Invalid value for '{field}'." };
        }

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }
}
