using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Common.Training;

namespace AerialWatch.Application.Common;

/// <summary>Services shared by every module (no module types inside).</summary>
public static class CommonServices
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped<TrainingDatasetExporter>(); // AI training dataset export (D-038)
        return services;
    }
}
