using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AerialWatch.Api.Routing;

/// <summary>Controllers/Wfp/PlannerController.GetWindows becomes api/wfp/planner/getWindows.</summary>
public sealed class ModuleRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        const string marker = ".Controllers.";
        var ns = controller.ControllerType.Namespace ?? "";
        var index = ns.IndexOf(marker, StringComparison.Ordinal);
        foreach (var selector in controller.Selectors)
        {
            var route = selector.AttributeRouteModel;
            if (route?.Template?.Contains("[module]", StringComparison.Ordinal) != true) continue;
            var module = index < 0 ? "" : ns[(index + marker.Length)..];
            if (module.Length == 0 || module.Contains('.') || module == "Shared")
                throw new InvalidOperationException($"{controller.ControllerName} must be in Controllers/<Module> with a matching namespace.");
            route.Template = route.Template.Replace("[module]", module.ToLowerInvariant(), StringComparison.Ordinal);
        }
    }
}
