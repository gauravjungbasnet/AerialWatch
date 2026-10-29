using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AerialWatch.Application.Abstractions;
using AerialWatch.Persistence.Repositories;
using AerialWatch.Contracts.Health;

namespace AerialWatch.Persistence;

public static class DependencyInjection
{
    public const string ConnectionStringName = "AerialWatch";

    /// <summary>
    /// Registers the data layer: Microsoft SQL Server via EF Core, repositories and unit of work.
    /// Connection string: ConnectionStrings:AerialWatch (appsettings.json, user secrets or environment variable
    /// ConnectionStrings__AerialWatch). The API never creates or migrates the database; the developer runs
    /// `dotnet ef database update` (D-023).
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName} in appsettings.json.");

        services.AddDbContext<AerialWatchDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        // Repository layer (one DbContext per request; changes are committed by the unit of work)
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IModelCoefficientSetRepository, ModelCoefficientSetRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDatabaseStatus, SqlServerDatabaseStatus>();

        return services;
    }
}
