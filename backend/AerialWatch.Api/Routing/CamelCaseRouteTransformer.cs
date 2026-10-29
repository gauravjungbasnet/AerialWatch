using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AerialWatch.Api.Routing;

public sealed class CamelCaseRouteTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text[1..];
    }
}
