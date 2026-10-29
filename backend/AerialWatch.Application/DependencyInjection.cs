using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Common;
using AerialWatch.Application.Wfp;

namespace AerialWatch.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the business logic of every module. Each module registers its own services in
    /// &lt;Module&gt;/&lt;Module&gt;Services.cs, so adding a module is one line here.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddCommonServices()    // shared by all modules (AI training dataset export, D-038)
            .AddWfpServices();      // Watch Feral Pigs
            // .AddWwlServices()    // Watch Wetlands (later)
            // .AddWgnServices()    // Watch Ghost Net (later)
}
