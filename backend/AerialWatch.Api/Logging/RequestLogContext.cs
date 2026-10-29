using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace AerialWatch.Api.Logging;

/// <summary>
/// Adds request details (module, feature, user, path, trace id…) to every log line written during a request,
/// so a row in core.ErrorLogs says where it happened and who was affected. Never logs bodies, cookies or headers
/// other than User-Agent.
/// </summary>
public sealed class RequestLogContext
{
    private readonly RequestDelegate _next;

    public RequestLogContext(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        using (Push(context, context.GetEndpoint()))
            await _next(context);
    }

    /// <summary>The id shown to the user as the error reference and stored in TraceId.</summary>
    public static string TraceId(HttpContext http) => Activity.Current?.TraceId.ToHexString() ?? http.TraceIdentifier;

    public static IDisposable Push(HttpContext http, Endpoint? endpoint, int? statusCode = null)
    {
        var properties = new List<ILogEventEnricher>();
        void Add(string name, object? value)
        {
            if (value is not null && value is not "") properties.Add(new PropertyEnricher(name, value));
        }

        // Controllers/Wfp/SurveyImagesController -> Module "Wfp", Feature "SurveyImages" (same naming as ModuleRouteConvention).
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is not null)
        {
            var ns = action.ControllerTypeInfo.Namespace ?? "";
            var marker = ns.LastIndexOf(".Controllers.", StringComparison.Ordinal);
            Add("Module", marker < 0 ? "Platform" : ns[(marker + ".Controllers.".Length)..]);
            Add("Feature", action.ControllerName);
            Add("Action", action.ActionName);
        }

        Add("TraceId", TraceId(http));
        Add("HttpMethod", http.Request.Method);
        Add("RequestPath", http.Request.Path.Value);
        Add("QueryString", http.Request.QueryString.Value);
        Add("StatusCode", statusCode);
        Add("UserName", http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : "anonymous");
        Add("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        Add("UserAgent", http.Request.Headers.UserAgent.ToString());

        return LogContext.Push(properties.ToArray());
    }
}
