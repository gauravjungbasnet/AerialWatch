using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AerialWatch.Api.Logging;
using AerialWatch.Contracts.Common;

namespace AerialWatch.Api.ErrorHandling;

/// <summary>
/// Maps exceptions to RFC 7807 problem responses, so controllers stay thin, and logs each one to core.ErrorLogs
/// (D-036): expected errors (400/404) as Warning, anything else as Error with the full exception. Every response
/// carries <c>errorReference</c> (the TraceId), so a user can quote it and an admin can find the exact log row.
/// </summary>
public sealed class ApplicationExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApplicationExceptionHandler> _logger;

    public ApplicationExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApplicationExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var reference = RequestLogContext.TraceId(httpContext);
        ProblemDetails problem;
        switch (exception)
        {
            case RequestValidationException validation:
                problem = new ValidationProblemDetails(validation.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };
                break;
            case NotFoundException notFound:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not found",
                    Detail = notFound.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
                };
                break;
            case BadHttpRequestException bad:
                problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Bad request", Detail = bad.Message };
                break;
            default:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Something went wrong",
                    Detail = $"The error has been logged. Quote reference {reference} if you report it.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                };
                break;
        }

        var status = problem.Status!.Value;
        // The framework clears the endpoint before calling handlers; the feature keeps the original one.
        var endpoint = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
        using (RequestLogContext.Push(httpContext, endpoint, status))
        {
            if (status >= 500)
                _logger.LogError(exception, "Unhandled {ExceptionType}: {ExceptionMessage}",
                    exception.GetType().FullName, exception.Message);
            else
                _logger.LogWarning("Request refused ({ExceptionType}): {ExceptionMessage}",
                    exception.GetType().FullName, exception.Message);
        }

        problem.Extensions["errorReference"] = reference;
        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
