using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application;
using AerialWatch.Infrastructure;
using AerialWatch.Persistence;

namespace AerialWatch.Startup;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every backend layer behind the interfaces in AerialWatch.Contracts:
    /// Application (services) · Persistence (SQL Server, repositories) · Infrastructure (clock; later files, AI client).
    /// </summary>
    public static IServiceCollection AddAerialWatch(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddApplication()
            .AddPersistence(configuration)
            .AddInfrastructure();
}
